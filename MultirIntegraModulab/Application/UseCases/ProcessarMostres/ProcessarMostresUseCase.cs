using System;
using System.Collections.Generic;
using System.Linq;
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


                    // FASE 3: Tractament specifíc segons tipus d´incorporació
                    if (!TractarTipusIncorporacio(mostra, tipusIncorporacio, resum))
                    {
                        // Si retorna false, cal ometre el processament posterior (continue)
                        continue;
                    }



                    // FASE 4: Comprovar microorganismes
                    var resultatMicroorganismes = _comprovadorMicroorganismesUseCase.Executar(mostra);
                    if (!resultatMicroorganismes.Exitosa)
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}❌ Error comprovant microorganismes: {resultatMicroorganismes.Missatge}");
                    }


                    // FASE 5: Comprovar mecanismes de resistència
                    var resultatMecanismes = _comprovadorMecanismesUseCase.Executar(mostra);
                    if (!resultatMecanismes.ContinuarProcessament)
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ Mostra {mostra.EtiquetaId} descartada: {resultatMecanismes.Missatge}");
                        resum.MostresAmbError++;
                        continue;
                    }

                    
                    // FASE 6: Classificar mostra (un sol positiu, múltiples negatius, mixta, ...)
                    var classificacio = _classificarMostraUseCase.Executar(mostra);


                    // FASE 7: Processar segons el tipus de mostra
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
        /// Actualitza els comptadors del resum segons el tipus d'incorporació
        /// </summary>
        private void ActualitzarResumPerTipus(ResumProcessamentDto resum, TipusIncorporacio tipusIncorporacio)
        {
            switch (tipusIncorporacio)
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
            }
        }

        /// <summary>
        /// Processa una mostra segons el seu tipus de classificació
        /// </summary>
        private async Task ProcessarPerTipusMostraAsync(Mostra mostra, ResultatClassificacio classificacio, ResumProcessamentDto resum)
        {
            switch (classificacio.TipusMostra)
            {
                case TipusMostra.UnSolResultatPositiu:
                    var resultatPositiu = await _processarPositivaUseCase.ExecutarAsync(mostra, classificacio);
                    if (resultatPositiu.Exitosa)
                    {
                        resum.MostresPositives++;
                    }
                    else
                    {
                        resum.MostresAmbError++;
                    }
                    break;

                case TipusMostra.MultiplesResultatsTotsPositius:
                    var resultatPositives = await _processarPositivesUseCase.ExecutarAsync(mostra, classificacio);
                    if (resultatPositives.Exitosa)
                    {
                        resum.MostresPositives++;
                    }
                    else
                    {
                        resum.MostresAmbError++;
                    }
                    break;

                case TipusMostra.UnSolResultatNegatiu:
                    var resultatNegatiu = await _processarNegativaUseCase.ExecutarAsync(mostra, classificacio);
                    if (resultatNegatiu.Exitosa)
                    {
                        resum.MostresNegatives++;
                    }
                    else
                    {
                        resum.MostresAmbError++;
                    }
                    break;

                case TipusMostra.MultiplesResultatsTotsNegatius:
                    var resultatNegatives = await _processarNegativesUseCase.ExecutarAsync(mostra, classificacio);
                    if (resultatNegatives.Exitosa)
                    {
                        resum.MostresNegatives++;
                    }
                    else
                    {
                        resum.MostresAmbError++;
                    }
                    break;

                case TipusMostra.MultiplesResultatsPositiusINegatius:
                    var resultatMixta = await _processarMixtaUseCase.ExecutarAsync(mostra, classificacio);
                    if (resultatMixta.Exitosa)
                    {
                        resum.MostresPositives++;
                        resum.MostresNegatives++;
                    }
                    else
                    {
                        resum.MostresAmbError++;
                    }
                    break;

                default:
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ Tipus de mostra desconegut: {classificacio.TipusMostra}");
                    resum.MostresAmbError++;
                    break;
            }
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
        /// Tracta una mostra antiga: actualitza les dates
        /// </summary>
        private bool TractarMostraAntigua(Mostra mostra, ResumProcessamentDto resum)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ Mostra antiga (sense dates) - actualitzant dates...");

            try
            {
                // Obtenir les dates del primer resultat (són iguals per tots els resultats de la mateixa etiqueta)
                var primerResultat = mostra.Resultats[0];
                var dataResultat = primerResultat.DataResultat;
                var dataValidacio = primerResultat.DataValidacio;

                // Actualitzar les dates a la base de dades
                bool actualitzat = _multiRRepository.ActualitzarResultatAntic(
                    mostra.EtiquetaId,
                    dataResultat,
                    dataValidacio);

                if (actualitzat)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✅ Dates actualitzades correctament");
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}   - Data resultat: {dataResultat:dd/MM/yyyy HH:mm}");
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}   - Data validació: {(dataValidacio.HasValue ? dataValidacio.Value.ToString("dd/MM/yyyy HH:mm") : "NULL")}");
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'han pogut actualitzar les dates");
                }

                // Inserir auditoria amb codi EMCA (Estat Mostra Cas Antic)
                bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
                    mostra,
                    "EMCA",
                    primerResultat,
                    null);

                if (auditoriaCreada)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✅ Auditoria EMCA (Estat Mostra Cas Antic) creada correctament");
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha pogut crear l'auditoria EMCA");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error tractant mostra antiga: {ex.Message}", ex);
                resum.MostresAmbError++;
            }

            return false; // No continuar processament, passar a la següent mostra
        }


        /// <summary>
        /// Tracta una mostra desvalidada: compara amb mostra existent i decideix acció
        /// Si són idèntiques: actualitza data_validacio a NULL i estat a 'P', insereix auditoria EMCD
        /// Si són diferents: guarda historial, esborra dades i continua processament
        /// </summary>
        private bool TractarMostraDesvalidada(Mostra mostra, ResumProcessamentDto resum)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🗑️ Mostra desvalidada - comprovant canvis...");

            try
            {
                // 1. Obtenir la mostra existent de la base de dades
                var mostraExistent = _multiRRepository.ObtenirMostraDiagnostic(mostra.EtiquetaId);
                
                if (mostraExistent == null)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha trobat mostra existent per comparar");
                    resum.MostresAmbError++;
                    return false;
                }

                // 2. Comparar mostres per detectar canvis
                var resultatComparacio = _multiRRepository.CompararMostres(mostraExistent, mostra);

                if (!resultatComparacio.HiHaCanvis)
                {
                    // CAS 1: No hi ha canvis - només actualitzar data_validacio a NULL
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✅ Mostres idèntiques - actualitzant data_validacio a NULL...");

                    bool actualitzat = _multiRRepository.ActualitzarDataValidacio(mostra.EtiquetaId, null);

                    if (actualitzat)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Data validació actualitzada a NULL i estat_integracio_m a 'P'");
                    }
                    else
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut actualitzar la data de validació");
                    }

                    // Inserir auditoria EMCD (Estat Mostra Cas Desvalidat sense canvis)
                    var primerResultat = mostra.Resultats[0];
                    bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
                        mostra,
                        "EMCD",
                        primerResultat,
                        null);

                    if (auditoriaCreada)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Auditoria EMCD (Estat Mostra Cas Desvalidat sense canvis) creada correctament");
                    }
                    else
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut crear l'auditoria EMCD");
                    }

                    return false; // No continuar processament
                }
                else
                {
                    // CAS 2: Hi ha canvis - guardar historial, esborrar i continuar
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔄 Mostres diferents - guardant historial i esborrant dades...");
                    
                    // Mostrar canvis detectats
                    foreach (var canvi in resultatComparacio.CanvisDetectats)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}   📝 {canvi}");
                    }

                    // Preparar dades per a l'historial
                    var tipusCanvi = "DESVALIDADA_AMB_CANVIS";
                    
                    // Obtenir combinacions anteriors i noves en format text
                    var combinacionsAnteriors = ObtenirCombinacionsTextMostraExistent(mostraExistent);
                    var combinacionsNoves = ObtenirCombinacionsTextMostraEntrant(mostra);
                    
                    // Guardar historial abans d'esborrar
                    bool historialGuardat = _multiRRepository.GuardarHistorialMostra(
                        mostra.EtiquetaId,
                        tipusCanvi,
                        combinacionsAnteriors,
                        mostraExistent.DataResultat,
                        mostraExistent.DataValidacio,
                        combinacionsNoves,
                        mostra.DataUltimResultat,
                        mostra.Resultats.FirstOrDefault()?.DataValidacio);

                    if (historialGuardat)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Historial guardat correctament");
                    }
                    else
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut guardar l'historial");
                    }

                    // Esborrar dades de la mostra
                    bool esborrat = _multiRRepository.EsborrarDadesMostra(mostra.EtiquetaId);
                    
                    if (!esborrat)
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}❌ Error esborrant mostra desvalidada");
                        resum.MostresAmbError++;
                        return false;
                    }
                    else
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Dades esborrades correctament");
                    }

                    // Continuar processament per re-processar la mostra amb les noves dades
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}➡️ Continuant processament amb noves dades...");
                    return true; // Continuar processament
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error tractant mostra desvalidada: {ex.Message}", ex);
                resum.MostresAmbError++;
                return false;
            }
        }

        /// <summary>
        /// Obté les combinacions microorganisme+mecanisme d'una mostra existent en format text
        /// </summary>
        private string ObtenirCombinacionsTextMostraExistent(MostraDiagnosticExistent mostraExistent)
        {
            if (mostraExistent == null)
                return null;

            // Aquí hauries d'obtenir les combinacions de la base de dades
            // Per ara retornem un text simple amb la informació disponible
            return $"Tipus mostra: {mostraExistent.TipusMostra}, " +
                   $"Data resultat: {mostraExistent.DataResultat:dd/MM/yyyy HH:mm}, " +
                   $"Data validació: {mostraExistent.DataValidacio?.ToString("dd/MM/yyyy HH:mm") ?? "NULL"}";
        }

        /// <summary>
        /// Obté les combinacions microorganisme+mecanisme d'una mostra entrant en format text
        /// </summary>
        private string ObtenirCombinacionsTextMostraEntrant(Mostra mostra)
        {
            if (mostra == null || !mostra.Resultats.Any())
                return null;

            var combinacions = new List<string>();
            
            foreach (var resultat in mostra.Resultats)
            {
                var microorganisme = resultat.AillamentDescripcio ?? "Sense microorganisme";
                var mecanismes = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia1Id))
                    mecanismes.Add(resultat.MecanismeResistencia1Id);
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia2Id))
                    mecanismes.Add(resultat.MecanismeResistencia2Id);
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia3Id))
                    mecanismes.Add(resultat.MecanismeResistencia3Id);
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia4Id))
                    mecanismes.Add(resultat.MecanismeResistencia4Id);
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia5Id))
                    mecanismes.Add(resultat.MecanismeResistencia5Id);
                
                if (mecanismes.Any())
                {
                    combinacions.Add($"{microorganisme}+[{string.Join(",", mecanismes)}]");
                }
                else
                {
                    combinacions.Add(microorganisme);
                }
            }
            
            return string.Join("; ", combinacions);
        }

        /// <summary>
        /// Tracta una mostra validada: guarda historial i continua processament
        /// </summary>
        private bool TractarMostraValidada(Mostra mostra, TipusIncorporacio tipusIncorporacio)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}📝 Mostra validada - guardant historial i actualitzant...");

            try
            {
                // Obtenir mostra existent per les dades anteriors
                var mostraExistent = _multiRRepository.ObtenirMostraDiagnostic(mostra.EtiquetaId);
                
                if (mostraExistent != null)
                {
                    // Preparar dades per a l'historial
                    var tipusCanvi = "VALIDADA_AMB_CANVIS";
                    var combinacionsAnteriors = ObtenirCombinacionsTextMostraExistent(mostraExistent);
                    var combinacionsNoves = ObtenirCombinacionsTextMostraEntrant(mostra);
                    
                    // Guardar historial
                    bool historialGuardat = _multiRRepository.GuardarHistorialMostra(
                        mostra.EtiquetaId,
                        tipusCanvi,
                        combinacionsAnteriors,
                        mostraExistent.DataResultat,
                        mostraExistent.DataValidacio,
                        combinacionsNoves,
                        mostra.DataUltimResultat,
                        mostra.Resultats.FirstOrDefault()?.DataValidacio);

                    if (historialGuardat)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}📝 Historial guardat correctament");
                    }
                    else
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha pogut guardar l'historial");
                    }
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha trobat mostra existent per guardar historial");
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
                // Obtenir mostra existent per les dades anteriors
                var mostraExistent = _multiRRepository.ObtenirMostraDiagnostic(mostra.EtiquetaId);
                
                if (mostraExistent != null)
                {
                    // Preparar dades per a l'historial
                    var tipusCanvi = "REVALIDADA_AMB_CANVIS";
                    var combinacionsAnteriors = ObtenirCombinacionsTextMostraExistent(mostraExistent);
                    var combinacionsNoves = ObtenirCombinacionsTextMostraEntrant(mostra);
                    
                    // Guardar historial
                    bool historialGuardat = _multiRRepository.GuardarHistorialMostra(
                        mostra.EtiquetaId,
                        tipusCanvi,
                        combinacionsAnteriors,
                        mostraExistent.DataResultat,
                        mostraExistent.DataValidacio,
                        combinacionsNoves,
                        mostra.DataUltimResultat,
                        mostra.Resultats.FirstOrDefault()?.DataValidacio);

                    if (historialGuardat)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}📝 Historial guardat correctament");
                    }
                    else
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha pogut guardar l'historial");
                    }
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha trobat mostra existent per guardar historial");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error guardant historial de mostra revalidada: {ex.Message}", ex);
            }

            return true; // Continuar processament per actualitzar dates i relacions
        }
    }
}
