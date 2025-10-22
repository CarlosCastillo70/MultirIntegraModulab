using System;
using System.Threading.Tasks;
using MultirIntegraModulab.Application.DTOs;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Application.UseCases.ClassificarMostres;
using MultirIntegraModulab.Application.UseCases.DeterminarTipus;
using MultirIntegraModulab.Application.UseCases.ComprovadorMicroorganismes;
using MultirIntegraModulab.Application.UseCases.ComprovadorMecanismes;
using TipusIncorporacio = MultirIntegraModulab.Domain.Enums.TipusIncorporacio;
using TipusMostra = MultirIntegraModulab.Domain.Enums.TipusMostra;

namespace MultirIntegraModulab.Application.UseCases.ProcessarMostres
{
    /// <summary>
    /// Use Case per processar una col·lecció de mostres
    /// Segueix el patró Command/Query i coordina els altres Use Cases
    /// </summary>
    public class ProcessarMostresUseCase
    {
        private readonly IModulabRepository _modulabRepository;
        private readonly IMultiRRepository _multiRRepository;
        private readonly IPacientWebService _pacientWebService;
        private readonly ILoggerService _logger;
        
        // Use Cases de validació i classificació
        private readonly ValidarMostraUseCase _validarMostraUseCase;
        private readonly ClassificarMostraUseCase _classificarMostraUseCase;
        private readonly DeterminarTipusIncorporacioUseCase _determinarTipusUseCase;
        private readonly ComprovadorMicroorganismesUseCase _comprovadorMicroorganismesUseCase;
        private readonly ComprovadorMecanismesResistenciaUseCase _comprovadorMecanismesUseCase;
        
        // Use Cases de processament específic
        private readonly ProcessarMostraPositivaUseCase _processarPositivaUseCase;
        private readonly ProcessarMostraNegativaUseCase _processarNegativaUseCase;
        private readonly ProcessarMostresPositivesUseCase _processarPositivesUseCase;
        private readonly ProcessarMostresNegativesUseCase _processarNegativesUseCase;
        private readonly ProcessarMostraMixtaUseCase _processarMixtaUseCase;

        public ProcessarMostresUseCase(
            IModulabRepository modulabRepository,
            IMultiRRepository multiRRepository,
            IPacientWebService pacientWebService,
            ILoggerService logger,
            ValidarMostraUseCase validarMostraUseCase)
        {
            _modulabRepository = modulabRepository ?? throw new ArgumentNullException(nameof(modulabRepository));
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _pacientWebService = pacientWebService; // Pot ser null
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validarMostraUseCase = validarMostraUseCase ?? throw new ArgumentNullException(nameof(validarMostraUseCase));
            
            // Inicialitzar Use Cases de validació
            _classificarMostraUseCase = new ClassificarMostraUseCase(_multiRRepository, _logger);
            _determinarTipusUseCase = new DeterminarTipusIncorporacioUseCase(_multiRRepository, _logger);
            _comprovadorMicroorganismesUseCase = new ComprovadorMicroorganismesUseCase(_multiRRepository, _logger);
            _comprovadorMecanismesUseCase = new ComprovadorMecanismesResistenciaUseCase(_multiRRepository, _logger);
            
            // Inicialitzar Use Cases de processament
            _processarPositivaUseCase = new ProcessarMostraPositivaUseCase(_multiRRepository, _pacientWebService, _logger);
            _processarNegativaUseCase = new ProcessarMostraNegativaUseCase(_multiRRepository, _logger);
            _processarPositivesUseCase = new ProcessarMostresPositivesUseCase(_multiRRepository, _pacientWebService, _logger);
            _processarNegativesUseCase = new ProcessarMostresNegativesUseCase(_multiRRepository, _logger);
            _processarMixtaUseCase = new ProcessarMostraMixtaUseCase(_multiRRepository, _pacientWebService, _logger);
        }

        /// <summary>
        /// Executa el processament de totes les mostres
        /// </summary>
        public async Task<ResumProcessamentDto> ExecutarAsync(ColeccioMostres mostres)
        {
            var resum = new ResumProcessamentDto
            {
                DataIniciProcessament = DateTime.Now
            };

            if (mostres == null || mostres.NombreTotalMostres == 0)
            {
                _logger.Warning("❌ No hi ha mostres per processar");
                resum.DataFiProcessament = DateTime.Now;
                return resum;
            }

            foreach (var mostra in mostres.ObtenirTotesLesMostres())
            {
                try
                {
                    _logger.Info($"------------------------------------");
                    _logger.Info($">>> Processant mostra del pacient {mostra.PacientSap} , amb etiqueta : {mostra.EtiquetaId}");
                    _logger.Info($"------------------------------------");

                    // FASE 1: Validar mostra (existència dades bàsiques)
                    if (!_validarMostraUseCase.Executar(mostra))
                    {
                        _logger.Warning($"Mostra {mostra.EtiquetaId} no vàlida - s'omet");
                        resum.MostresAmbError++;
                        continue;
                    }

                    
                    // FASE 2: Determinar tipus d'incorporació (nova, validada, re validada, ...)
                    var tipusIncorporacio = _determinarTipusUseCase.Executar(mostra);

                    // Actualitzar resum final segons tipus d'incorporació
                    ActualitzarResumPerTipus(resum, tipusIncorporacio);

                    

                    // FASE 3: Classificar mostra (un sol positiu, múltiples negatius, mixta, ...)
                    var classificacio = _classificarMostraUseCase.Executar(mostra);



                    // FASE 4: Comprovar microorganismes
                    var resultatMicroorganismes = _comprovadorMicroorganismesUseCase.Executar(mostra);
                    if (!resultatMicroorganismes.Exitosa)
                    {
                        _logger.Warning($" ❌ Error comprovant microorganismes: {resultatMicroorganismes.Missatge}");
                    }


                    // FASE 5: Comprovar mecanismes de resistència
                    var resultatMecanismes = _comprovadorMecanismesUseCase.Executar(mostra);
                    if (!resultatMecanismes.ContinuarProcessament)
                    {
                        _logger.Warning($" ⚠️ Mostra {mostra.EtiquetaId} descartada: {resultatMecanismes.Missatge}");
                        resum.MostresAmbError++;
                        continue;
                    }

                    // FASE 6: Processar segons el tipus de mostra
                    await ProcessarPerTipusMostraAsync(mostra, classificacio, resum);



                    resum.TotalProcessats++;
                    _logger.Info($"✅ Mostra {mostra.EtiquetaId} processada correctament");
                }
                catch (Exception ex)
                {
                    _logger.Error($"❌ Error processant mostra {mostra?.EtiquetaId}: {ex.Message}", ex);
                    resum.MostresAmbError++;
                }
            }

            resum.DataFiProcessament = DateTime.Now;
            
            _logger.Info($"");
            _logger.Info($"========================================");
            _logger.Info($"Processament finalitzat: {resum}");
            _logger.Info($"========================================");

            return resum;
        }

        /// <summary>
        /// Processa la mostra segons el seu tipus: un positiu, múltiples positius, un negatiu, múltiples negatius, mixta
        /// </summary>
        private async Task ProcessarPerTipusMostraAsync(
            Mostra mostra,
            ResultatClassificacio classificacio,
            ResumProcessamentDto resum)
        {
            switch (classificacio.TipusMostra)
            {
                case TipusMostra.UnSolResultatPositiu:
                    var resultatPositiu = await _processarPositivaUseCase.ExecutarAsync(mostra, classificacio);
                    if (resultatPositiu.Exitosa)
                    {
                        resum.MostresPositives++;
                    }
                    break;

                case TipusMostra.UnSolResultatNegatiu:
                    var resultatNegatiu = await _processarNegativaUseCase.ExecutarAsync(mostra, classificacio);
                    if (resultatNegatiu.Exitosa)
                    {
                        resum.MostresNegatives++;
                    }
                    break;

                case TipusMostra.MultiplesResultatsTotsPositius:
                    var resultatPositius = await _processarPositivesUseCase.ExecutarAsync(mostra, classificacio);
                    if (resultatPositius.Exitosa)
                    {
                        resum.MostresPositives++;
                    }
                    break;

                case TipusMostra.MultiplesResultatsTotsNegatius:
                    var resultatNegatius = await _processarNegativesUseCase.ExecutarAsync(mostra, classificacio);
                    if (resultatNegatius.Exitosa)
                    {
                        resum.MostresNegatives++;
                    }
                    break;

                case TipusMostra.MultiplesResultatsPositiusINegatius:
                    var resultatMixta = await _processarMixtaUseCase.ExecutarAsync(mostra, classificacio);
                    if (resultatMixta.Exitosa)
                    {
                        resum.MostresPositives++; // Es compta com positiva
                    }
                    break;

                default:
                    _logger.Warning($"❌ Tipus de mostra desconegut: {classificacio.TipusMostra}");
                    break;
            }
        }

        /// <summary>
        /// Actualitza el resum segons el tipus d'incorporació
        /// </summary>
        private void ActualitzarResumPerTipus(ResumProcessamentDto resum, TipusIncorporacio tipus)
        {
            switch (tipus)
            {
                case TipusIncorporacio.Nova:
                    resum.NovesIncorporacions++;
                    break;
                case TipusIncorporacio.Antiga:
                    resum.MostresAntigues++;
                    break;
                case TipusIncorporacio.Repetida:
                    resum.MostresRepetides++;
                    break;
                case TipusIncorporacio.Desvalidada:
                    resum.MostresDesvalidades++;
                    break;
                case TipusIncorporacio.Validada:
                    resum.MostresValidades++;
                    break;
                case TipusIncorporacio.Revalidada:
                    resum.MostresRevalidades++;
                    break;
                default:
                    // Altres tipus
                    _logger.Warning($"❌ Tipus d'incorporació desconegut (no es gestiona): {tipus.ToString()}");
                    break;
            }
        }
    }
}
