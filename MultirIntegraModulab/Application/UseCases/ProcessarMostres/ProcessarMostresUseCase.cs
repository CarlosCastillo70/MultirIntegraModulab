using System;
using System.Threading.Tasks;
using MultirIntegraModulab.Application.DTOs;
using MultirIntegraModulab.Application.Helpers;
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
                    _logger.Info($"▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄");
                    _logger.Info($" Processant mostra del pacient {mostra.PacientSap} , amb etiqueta : {mostra.EtiquetaId}");
                    _logger.Info($"▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀");

                    if (mostra.EtiquetaId == "402876565" || mostra.EtiquetaId == "402877669") 
                    {
                        var revisioDeCasos = 1;
                    }



                    // FASE 1: Validar mostra (existència dades bàsiques)
                    if (!_validarMostraUseCase.Executar(mostra))
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Mostra {mostra.EtiquetaId} no vàlida - s'omet");
                        resum.MostresAmbError++;
                        continue;
                    }

                    
                    // FASE 2: Determinar tipus d'incorporació (nova, validada, re validada, ...)
                    var tipusIncorporacio = _determinarTipusUseCase.Executar(mostra);

                    // Actualitzar resum final segons tipus d'incorporació
                    ActualitzarResumPerTipus(resum, tipusIncorporacio);

                    // TRACTAMENT ESPECÍFIC SEGONS TIPUS D'INCORPORACIÓ
                    if (!TractarTipusIncorporacio(mostra, tipusIncorporacio, resum))
                    {
                        // Si retorna false, cal ometre el processament posterior (continue)
                        continue;
                    }



                    // FASE 3: Comprovar microorganismes
                    var resultatMicroorganismes = _comprovadorMicroorganismesUseCase.Executar(mostra);
                    if (!resultatMicroorganismes.Exitosa)
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}❌ Error comprovant microorganismes: {resultatMicroorganismes.Missatge}");
                    }


                    // FASE 4: Comprovar mecanismes de resistència
                    var resultatMecanismes = _comprovadorMecanismesUseCase.Executar(mostra);
                    if (!resultatMecanismes.ContinuarProcessament)
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ Mostra {mostra.EtiquetaId} descartada: {resultatMecanismes.Missatge}");
                        resum.MostresAmbError++;
                        continue;
                    }

                    
                    // FASE 5: Classificar mostra (un sol positiu, múltiples negatius, mixta, ...)
                    var classificacio = _classificarMostraUseCase.Executar(mostra);


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
        /// Tracta una mostra segons el seu tipus d'incorporació
        /// </summary>
        /// <param name="mostra">Mostra a tractar</param>
        /// <param name="tipusIncorporacio">Tipus d'incorporació determinat</param>
        /// <param name="resum">Resum del processament</param>
        /// <returns>True si cal continuar processant, False si cal ometre la mostra</returns>
        private bool TractarTipusIncorporacio(Mostra mostra, TipusIncorporacio tipusIncorporacio, ResumProcessamentDto resum)
        {
            switch (tipusIncorporacio)
            {
                case TipusIncorporacio.Nova:
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}✨ Mostra nova - continuar endavant...");
                    return true; // Continuar processament normal

                case TipusIncorporacio.Repetida:
                    return TractarMostraRepetida(mostra, resum);

                case TipusIncorporacio.Desvalidada:
                    return TractarMostraDesvalidada(mostra, resum);

                case TipusIncorporacio.Antiga:
                    return TractarMostraAntigua(mostra, resum);

                case TipusIncorporacio.Validada:
                    return TractarMostraValidada(mostra, tipusIncorporacio);

                case TipusIncorporacio.Revalidada:
                    return TractarMostraRevalidada(mostra, tipusIncorporacio);

                default:
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}❓ Tipus d'incorporació desconegut: {tipusIncorporacio}");
                    return true; // Continuar per seguretat
            }
        }

        /// <summary>
        /// Tracta una mostra desvalidada: guarda historial i esborra dades
        /// </summary>
        private bool TractarMostraDesvalidada(Mostra mostra, ResumProcessamentDto resum)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🗑️ Mostra desvalidada - guardant historial i esborrant dades...");

            try
            {
                // Guardar historial abans d'esborrar
                var tipusCanvi = "DESVALIDADA";
                var observacions = "Mostra desvalidada - Oracle no té data de validació";
                
                bool historialGuardat = _multiRRepository.GuardarHistorialMostra(
                    mostra.EtiquetaId,
                    tipusCanvi,
                    observacions);

                if (historialGuardat)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}📝 Historial guardat correctament");
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha pogut guardar l'historial");
                }

                // Esborrar dades de la mostra
                bool esborrat = _multiRRepository.EsborrarDadesMostra(mostra.EtiquetaId);
                
                if (!esborrat)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error esborrant mostra desvalidada");
                    resum.MostresAmbError++;
                }
                else
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✅ Dades esborrades correctament");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error tractant mostra desvalidada: {ex.Message}", ex);
                resum.MostresAmbError++;
            }

            return false; // No processar més, passar a la següent mostra
        }

        /// <summary>
        /// Tracta una mostra antiga: actualitza les dates
        /// </summary>
        private bool TractarMostraAntigua(Mostra mostra, ResumProcessamentDto resum)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ Mostra antiga (sense dates) - actualitzant dates...");

            // TODO: Implementar actualització de dates per mostres antigues
            // Per ara, deixem que continuï el processament normal
            _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ Tractament de mostres antigues pendent d'implementació");

            return true; // Continuar processant
        }



        /// <summary>
        /// Tracta una mostra repetida: inserir auditoria i no continuar
        /// </summary>
        private bool TractarMostraRepetida(Mostra mostra, ResumProcessamentDto resum)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⏭️ Mostra repetida (dates idèntiques) - inserint auditoria...");

            try
            {
                // Inserir auditoria amb codi EMCR (Estat Mostra Cas Repetit)
                // Utilitzem el primer resultat per l'auditoria
                var primerResultat = mostra.Resultats[0];
                
                bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
                    mostra, 
                    "EMCR", 
                    primerResultat, 
                    null);

                if (auditoriaCreada)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✅ Auditoria EMCR (Estat Mostra Cas Repetit) creada correctament");
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha pogut crear l'auditoria EMCR");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error creant auditoria per mostra repetida: {ex.Message}", ex);
            }

            return false; // No processar més, passar a la següent mostra
        }


        /// <summary>
        /// Tracta una mostra validada: guarda historial i continua processament
        /// </summary>
        private bool TractarMostraValidada(Mostra mostra, TipusIncorporacio tipusIncorporacio)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}📝 Mostra validada - guardant historial i actualitzant...");

            try
            {
                // Guardar historial
                var tipusCanvi = "VALIDADA";
                var observacions = "Mostra validada - Oracle té nova data de validació";

                bool historialGuardat = _multiRRepository.GuardarHistorialMostra(
                    mostra.EtiquetaId,
                    tipusCanvi,
                    observacions);

                if (historialGuardat)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}📝 Historial guardat correctament");
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha pogut guardar l'historial");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error guardant historial de mostra validada: {ex.Message}", ex);
            }

            return true; // Continuar processament per actualitzar dates i relacions
        }

        /// <summary>
        /// Tracta una mostra revalidada: guarda historial i continua processament
        /// </summary>
        private bool TractarMostraRevalidada(Mostra mostra, TipusIncorporacio tipusIncorporacio)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🔄 Mostra revalidada - guardant historial i actualitzant...");

            try
            {
                // Guardar historial
                var tipusCanvi = "REVALIDADA";
                var observacions = "Mostra revalidada - Data de validació diferent a Oracle";

                bool historialGuardat = _multiRRepository.GuardarHistorialMostra(
                    mostra.EtiquetaId,
                    tipusCanvi,
                    observacions);

                if (historialGuardat)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}📝 Historial guardat correctament");
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha pogut guardar l'historial");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error guardant historial de mostra revalidada: {ex.Message}", ex);
            }

            return true; // Continuar processament per actualitzar dates i relacions
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
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}❌ Tipus d'incorporació desconegut (no es gestiona): {tipus.ToString()}");
                    break;
            }
        }
    }
}
