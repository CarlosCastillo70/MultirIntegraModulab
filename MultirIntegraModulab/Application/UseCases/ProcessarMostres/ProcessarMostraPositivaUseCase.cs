using MultirIntegraModulab.Application.UseCases.ClassificarMostres;
using MultirIntegraModulab.Application.Helpers;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Enums;
using MultirIntegraModulab.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultirIntegraModulab.Application.UseCases.ProcessarMostres
{
    /// <summary>
    /// Resultat del processament d'una mostra positiva
    /// </summary>
    public class ResultatProcessamentPositiu
    {
        public bool Exitosa { get; set; }
        public string Missatge { get; set; }
        public bool PacientCreat { get; set; }
        
        // Comptadors detallats
        public int DiagnosticsCreats { get; set; }
        public int DiagnosticsExistents { get; set; }
        public int MostresDiagnosticCreades { get; set; }
        public int MostresDiagnosticExistents { get; set; }
        public int RelacionsCreades { get; set; }
        public int RelacionsDuplicades { get; set; }
        public int MecanismesProcessats { get; set; }
        public int IntegracionsCreades { get; set; }
        public int AuditoriasCreades { get; set; }
        public int MostresNegativesCreades { get; set; }

        public ResultatProcessamentPositiu()
        {
            Exitosa = true;
        }
    }

    /// <summary>
    /// Use Case per processar una mostra amb un sol resultat positiu: microorganisme especial i/o mecanismes de resistència
    /// Les mostres positives sempre s'incorporen
    /// Gestiona la inserció del pacient, diagnostic, mostra, ...
    /// </summary>
    public class ProcessarMostraPositivaUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly IPacientWebService _pacientWebService;
        private readonly ILoggerService _logger;

        public ProcessarMostraPositivaUseCase(
            IMultiRRepository multiRRepository,
            IPacientWebService pacientWebService,
            ILoggerService logger)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _pacientWebService = pacientWebService; // Pot ser null si no està configurat
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executa el processament d'una mostra amb un sol resultat positiu
        /// </summary>
        /// <param name="mostra">Mostra a processar (contenidor amb ETIQUETA_ID)</param>
        /// <param name="classificacio">Classificació de la mostra</param>
        /// <returns>Resultat del processament</returns>
        public async Task<ResultatProcessamentPositiu> ExecutarAsync(
            Mostra mostra, 
            ResultatClassificacio classificacio)
        {
            if (mostra == null)
            {
                _logger.Warning("Intentant processar resultat positiu amb mostra null");
                throw new ArgumentNullException(nameof(mostra));
            }

            if (classificacio == null)
            {
                _logger.Warning("Intentant processar mostra positiva amb classificació null");
                throw new ArgumentNullException(nameof(classificacio));
            }

            _logger.Info($"🔄 Processant resultat/s positiu/s de la mostra : {mostra.EtiquetaId}");

            var resultat = new ResultatProcessamentPositiu();

            try
            {
                // FASE 1: COMPROVAR/CREAR PACIENT
                await ProcessarPacientAsync(mostra, resultat);

                if (!resultat.Exitosa)
                {
                    return resultat;
                }

                // FASE 2: PROCESSAR CADA RESULTAT
                foreach (var resultatMostra in mostra.Resultats)
                {
                    ProcessarResultatPositiu(mostra, resultatMostra, resultat);
                }

                // Resultat final
                if (resultat.Exitosa)
                {
                    _logger.Info($"Mostra positiva {mostra.EtiquetaId} processada correctament: " +
                        $"{resultat.DiagnosticsCreats} diagnòstics creats, {resultat.DiagnosticsExistents} diagnòstics existents, " +
                        $"{resultat.MostresDiagnosticCreades} mostres creades, {resultat.MostresDiagnosticExistents} mostres existents, " +
                        $"{resultat.RelacionsCreades} relacions creades, {resultat.RelacionsDuplicades} duplicades, " +
                        $"{resultat.MecanismesProcessats} mecanismes processats, " +
                        $"{resultat.AuditoriasCreades} auditories");
                    
                    resultat.Missatge = "Mostra positiva processada correctament";
                }

                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processant mostra positiva {mostra.EtiquetaId}", ex);
                resultat.Exitosa = false;
                resultat.Missatge = $"Error: {ex.Message}";
                return resultat;
            }
        }

        /// <summary>
        /// Processa el pacient de la mostra
        /// </summary>
        private async Task ProcessarPacientAsync(Mostra mostra, ResultatProcessamentPositiu resultat)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🔎 Comprovant/creant pacient: {mostra.PacientSap}");

            // Validació bàsica: comprovar que existeix identificador de pacient
            if (string.IsNullOrWhiteSpace(mostra.PacientSap))
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ Mostra {mostra.EtiquetaId} sense identificador de pacient");
                resultat.Exitosa = false;
                resultat.Missatge = "La mostra no té identificador de pacient";
                return;
            }

            // 1. Comprovar si el pacient ja existeix a la base de dades MultiR
            bool pacientExisteixMultiR = _multiRRepository.ExisteixPacient(mostra.PacientSap);

            if (pacientExisteixMultiR)
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✓ Pacient {mostra.PacientSap} ja existeix a MultiR");
                resultat.PacientCreat = false;
                return;
            }

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Pacient {mostra.PacientSap} no existeix a MultiR, consultant web service SAP ...");

            // 2. Si el pacient no existeix, intentar recuperar les dades del web service
            if (_pacientWebService != null)
            {
                try
                {
                    var dadesPacient = _pacientWebService.ObtenirDadesPacient(mostra.PacientSap);
                    
                    if (dadesPacient != null)
                    {
                        // 3. Inserir el pacient a la base de dades MultiR
                        bool pacientInserit = _multiRRepository.InserirPacient(dadesPacient);
                        
                        if (pacientInserit)
                        {
                            resultat.PacientCreat = true;
                        }
                        else
                        {
                            _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠ No s'ha pogut inserir el pacient {mostra.PacientSap} a MultiR");
                            resultat.PacientCreat = false;
                            // No fallem el processament, només registrem l'advertència
                        }
                    }
                    else
                    {
                        // Si la consulta al webservice no retorna dades per al pacient
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Pacient {mostra.PacientSap} no trobat al web service");
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Inserint auditoria amb codi NPWS i aturant processament");
                        
                        // Inserir a taula log amb codi NPWS (No trobat al Web Service de Pacients)
                        bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "NPWS");
                        
                        if (auditoriaCreada)
                        {
                            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✓ Auditoria NPWS creada per mostra {mostra.EtiquetaId}");
                            resultat.AuditoriasCreades++;
                        }
                        else
                        {
                            _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠ No s'ha pogut crear l'auditoria NPWS");
                        }
                        
                        // Marcar com a no exitós per no continuar endavant
                        resultat.Exitosa = false;
                        resultat.PacientCreat = false;
                        resultat.Missatge = "Pacient no trobat al web service (NPWS)";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Error consultant/inserint pacient via web service: {ex.Message}");
                    _logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Detall error: {ex}");
                    
                    // Continuar igualment per no bloquejar el processament
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Continuant processament malgrat error en gestió del pacient");
                    resultat.PacientCreat = false;
                }
            }
            else
            {
                // Web service no configurat
                _logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Web service de pacients no configurat");
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}ℹ S'assumeix pacient {mostra.PacientSap} vàlid (sense validació ni inserció)");
                resultat.PacientCreat = false;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Processa un resultat positiu individual (ResultatMostra)
        /// </summary>
        private void ProcessarResultatPositiu(
            Mostra mostra,
            ResultatMostra resultatMostra,
            ResultatProcessamentPositiu resultat)
        {
            // Construir llista de mecanismes 
            var mecanismes = new List<string>();
            if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia1Id))
                mecanismes.Add($"{resultatMostra.MecanismeResistencia1Id}: {resultatMostra.MecanismeResistenciaDescrip}");
            if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia2Id))
                mecanismes.Add($"{resultatMostra.MecanismeResistencia2Id}: {resultatMostra.MecanismeResistenciaDescrip2}");
            if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia3Id))
                mecanismes.Add($"{resultatMostra.MecanismeResistencia3Id}: {resultatMostra.MecanismeResistenciaDescrip3}");
            if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia4Id))
                mecanismes.Add($"{resultatMostra.MecanismeResistencia4Id}: {resultatMostra.MecanismeResistenciaDescrip4}");
            if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia5Id))
                mecanismes.Add($"{resultatMostra.MecanismeResistencia5Id}: {resultatMostra.MecanismeResistenciaDescrip5}");

            string microorganisme = resultatMostra.AillamentDescripcio ?? "sense microorganisme";
            string textMecanismes = mecanismes.Any() ? $" [{string.Join(", ", mecanismes)}]" : " [sense mecanismes]";
            
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Processant resultat: {microorganisme}{textMecanismes}");

            
            if (!string.IsNullOrWhiteSpace(resultatMostra.AillamentDescripcio))
            {
                // Obtenir el codi del microorganisme de la taula microorganismes
                var microorganismeEntitat = _multiRRepository.ObtenirMicroorganisme(resultatMostra.AillamentDescripcio);
                string microorganismeCodi = microorganismeEntitat?.Codi ?? resultatMostra.AillamentDescripcio;

                // Obtenir tots els mecanismes del resultat
                var mecanismesResultat = new[]
                {
                    (resultatMostra.MecanismeResistencia1Id, resultatMostra.MecanismeResistenciaDescrip),
                    (resultatMostra.MecanismeResistencia2Id, resultatMostra.MecanismeResistenciaDescrip2),
                    (resultatMostra.MecanismeResistencia3Id, resultatMostra.MecanismeResistenciaDescrip3),
                    (resultatMostra.MecanismeResistencia4Id, resultatMostra.MecanismeResistenciaDescrip4),
                    (resultatMostra.MecanismeResistencia5Id, resultatMostra.MecanismeResistenciaDescrip5)
                };

                // Per cada mecanisme del resultat...
                foreach (var (mecanismeId, mecanismeDescrip) in mecanismesResultat)
                {
                    if (!string.IsNullOrWhiteSpace(mecanismeId))
                    {
                        
                        // Pacients_diagnostics
                        // ------------------------------------

                        // Comprovar si ja existeix el diagnòstic
                        int diagnosticId = _multiRRepository.ComprovarDiagnosticExisteix(
                            mostra.PacientSap,
                            microorganismeCodi,
                            mecanismeId,
                            mecanismeDescrip ?? mecanismeId);

                        int diagnosticIdFinal = diagnosticId;

                        if (diagnosticId == 0)
                        {
                            // Diagnòstic no existeix. Es procedeix a crear-lo
                            int nouDiagnosticId = _multiRRepository.CrearDiagnosticPacient(
                                mostra.PacientSap,
                                microorganismeCodi,
                                mecanismeId,
                                mecanismeDescrip ?? mecanismeId);

                            if (nouDiagnosticId > 0)
                            {
                                diagnosticIdFinal = nouDiagnosticId;
                                resultat.DiagnosticsCreats++;
                            }
                        }
                        else
                        {
                            resultat.DiagnosticsExistents++;
                        }


                        // Pacients_diagnostics_mostres
                        // ------------------------------------

                        // Comprovar si ja existeix la mostra diagnòstic
                        int mostraDiagnosticId = _multiRRepository.ComprovarMostraDiagnosticExisteix(
                            mostra.PacientSap,
                            resultatMostra.DataPeticioTrunc,
                            resultatMostra.MostraDescripcio);

                        int mostraDiagnosticIdFinal = mostraDiagnosticId;

                        if (mostraDiagnosticId == 0)
                        {
                            // Mostra diagnòstic no existeix. Es procedeix a crear-la
                            int nouMostraDiagnosticId = _multiRRepository.CrearMostraDiagnostic(
                                mostra.PacientSap,
                                resultatMostra.DataPeticioTrunc,
                                resultatMostra.MostraDescripcio,
                                resultatMostra.ProvaDescripcio,
                                mostra.EtiquetaId,
                                resultatMostra.DataResultat,
                                resultatMostra.DataValidacio,
                                mecanismeId,
                                resultatMostra.EsMicroorganismeEspecial);

                            if (nouMostraDiagnosticId > 0)
                            {
                                mostraDiagnosticIdFinal = nouMostraDiagnosticId;
                                resultat.MostresDiagnosticCreades++;
                            }
                        }
                        else
                        {
                            resultat.MostresDiagnosticExistents++;
                        }


                        // Mostra_Microorganisme
                        // ------------------------------------

                        // Comprovar si ja existeix el registre mostra_microorganisme
                        bool existeixMostraMicroorganisme = _multiRRepository.ComprovarMostraMicroorganismeExisteix(
                            diagnosticIdFinal,
                            mostraDiagnosticIdFinal);

                        if (existeixMostraMicroorganisme)
                        {
                            // Si existeix, és un duplicat. Ho deixem auditat i no fem res més per aquest mecanisme
                            bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
                                mostra,
                                "DMM",
                                resultatMostra,
                                new MecanismeResistenciaInfo { Id = mecanismeId });

                            if (auditoriaCreada)
                            {
                                resultat.AuditoriasCreades++;
                            }

                            resultat.RelacionsDuplicades++;
                            
                            // Continuar amb el següent mecanisme
                            continue;
                        }
                        else
                        {
                            // Si no existeix, crear-lo
                            bool mostraMicroorganismeCreat = _multiRRepository.CrearMostraMicroorganisme(
                                diagnosticIdFinal,
                                mostraDiagnosticIdFinal);

                            if (mostraMicroorganismeCreat)
                            {
                                resultat.RelacionsCreades++;
                            }
                        }


                        // Actualitzar la data_diagnostic (de pacients_diagnostics) amb la data de mostra més antiga
                        // ------------------------------------
                        
                        bool dataActualitzada = _multiRRepository.ActualitzarDataDiagnosticPacientsDiagnostics(
                            mostra.PacientSap,
                            microorganismeCodi,
                            mecanismeId,
                            mecanismeDescrip ?? mecanismeId);


                        // Actualitzar la data_diagnostic (de pacients_diagnostics_mostra) amb la data de mostra més antiga
                        // ------------------------------------
                        
                        bool dataMostraActualitzada = _multiRRepository.ActualitzarDataDiagnosticPacientsDiagnosticsMostra(
                            mostra.PacientSap,
                            microorganismeCodi,
                            mecanismeId,
                            mecanismeDescrip ?? mecanismeId);



                        // Comprovar / Crear tipus de mostra a tipusmostra_m
                        // ------------------------------------

                        // Comprovar si existeix el tipus de mostra
                        bool existeixTipusMostra = _multiRRepository.ExisteixTipusMostraMactiu(resultatMostra.MostraDescripcio);

                        if (!existeixTipusMostra)
                        {
                            // Crear el tipus de mostra
                            bool tipusMostraCreat = _multiRRepository.CrearTipusMostraM(resultatMostra.MostraDescripcio);
                        }


                        // Comprovar / Crear tipus de prova a tipusprova
                        // ------------------------------------

                        // Comprovar si existeix el tipus de prova
                        bool existeixTipusProva = _multiRRepository.ExisteixTipusProvaActiu(resultatMostra.ProvaDescripcio);

                        if (!existeixTipusProva)
                        {
                            // Crear el tipus de prova
                            bool tipusProvaCreat = _multiRRepository.CrearTipusProva(resultatMostra.ProvaDescripcio);
                        }


                        // Buscar altres diagnostics positius del mateix tipus de mostra, per crear mostres negatives
                        // ------------------------------------

                        // Obtenir tots els diagnòstics positius del pacient per aquest tipus de mostra (excloent l'actual)
                        var diagnosticsPositius = _multiRRepository.ObtenirDiagnosticsPositiusPacientPerTipusMostra(
                            mostra.PacientSap,
                            resultatMostra.MostraDescripcio,
                            mostra.EtiquetaId);

                        if (diagnosticsPositius == null || diagnosticsPositius.Count == 0)
                        {
                            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✔️ No hi ha altres diagnòstics positius per aquest pacient i tipus de mostra");
                        }
                        else
                        {
                            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ Trobats {diagnosticsPositius.Count} diagnòstics positius (excloent l'actual)");

                            var altresDiagnosticsPositius = diagnosticsPositius.ToList();

                            if (altresDiagnosticsPositius.Count != 0)
                            {
                                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📋 Creant mostres NEGATIVES per {altresDiagnosticsPositius.Count} diagnòstic(s) diferent(s)...");

                                // Per cada altre diagnòstic positiu, crear una mostra negativa
                                foreach (int altDiagnosticId in altresDiagnosticsPositius)
                                {
                                    bool mostraNegativaCreada = CrearMostraNegativaPerDiagnostic(
                                        mostra,
                                        resultatMostra,
                                        altDiagnosticId);

                                    if (mostraNegativaCreada)
                                    {
                                        resultat.MostresNegativesCreades++;
                                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Mostra negativa creada per al diagnòstic #{altDiagnosticId}");
                                    }
                                    else
                                    {
                                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}❌ No s'ha pogut crear mostra negativa per diagnòstic #{altDiagnosticId}");
                                    }
                                }
                            }
                        }



                        // Final OK
                        // ------------------------------------

                        // Si arribem aquí indica que s´ha fet tota la gestió. Deixem registre auditoria (OK Positiva)
                        bool auditoriaCreadaOk = _multiRRepository.InserirAuditoriaIntegracioModulab(
                            mostra,
                            "OKP",
                            resultatMostra,
                            new MecanismeResistenciaInfo { Id = mecanismeId });

                        if (auditoriaCreadaOk)
                        {
                            resultat.AuditoriasCreades++;
                        }

                        // Incrementar comptador de mecanismes processats
                        resultat.MecanismesProcessats++;
                    }
                }
            }
        }


        /// <summary>
        /// Crea una mostra negativa per un diagnòstic positiu específic
        /// Una mostra negativa és una mostra del mateix tipus però que NO conté 
        /// el microorganisme/mecanisme del diagnòstic positiu
        /// </summary>
        private bool CrearMostraNegativaPerDiagnostic(
            Mostra mostra,
            ResultatMostra resultatMostra,
            int diagnosticPositiuId)
        {
            // 1. Crear la mostra diagnòstic (sense mecanisme, és negativa)
            int mostraDiagnosticId = _multiRRepository.ComprovarMostraDiagnosticExisteix(
                mostra.PacientSap,
                resultatMostra.DataPeticioTrunc,
                resultatMostra.MostraDescripcio);

            int mostraDiagnosticIdFinal = mostraDiagnosticId;

            if (mostraDiagnosticId == 0)
            {
                // Crear mostra negativa: sense mecanisme, no és especial
                mostraDiagnosticIdFinal = _multiRRepository.CrearMostraDiagnostic(
                    mostra.PacientSap,
                    resultatMostra.DataPeticioTrunc,
                    resultatMostra.MostraDescripcio,
                    resultatMostra.ProvaDescripcio,
                    mostra.EtiquetaId,
                    resultatMostra.DataResultat,
                    resultatMostra.DataValidacio,
                    null, // Sense mecanisme (negativa)
                    false); // No és microorganisme especial

                if (mostraDiagnosticIdFinal == 0)
                {
                    return false;
                }
            }

            // 2. Relacionar la mostra negativa amb el diagnòstic positiu
            bool existeixRelacio = _multiRRepository.ComprovarMostraMicroorganismeExisteix(
                diagnosticPositiuId,
                mostraDiagnosticIdFinal);

            if (existeixRelacio)
            {
                // Ja existeix la relació, no cal crear-la
                return false;
            }

            // Crear la relació
            bool relacionCreada = _multiRRepository.CrearMostraMicroorganisme(
                diagnosticPositiuId,
                mostraDiagnosticIdFinal);

            return relacionCreada;
        }


    }
}
