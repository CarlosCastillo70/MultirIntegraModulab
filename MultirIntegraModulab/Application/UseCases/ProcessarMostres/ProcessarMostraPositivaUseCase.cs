using MultirIntegraModulab.Application.Helpers;
using MultirIntegraModulab.Application.UseCases.ClassificarMostres;
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
        
        /// <summary>
        /// Indica si s'ha afegit almenys un positiu nou durant aquest processament
        /// </summary>
        public bool PositiuAfegit { get; set; }

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
            PositiuAfegit = false;
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
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Mostra positiva {mostra.EtiquetaId} processada correctament: " +
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
            _logger.Info($"🔎 Comprovant/creant pacient: {mostra.PacientSap}");

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
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Pacient {mostra.PacientSap} JA existeix a MultiR");
                resultat.PacientCreat = false;
                return;
            }

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Pacient {mostra.PacientSap} NO existeix a MultiR. Consultant web service SAP");

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
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}💥 Inserint auditoria amb codi NPWS i aturant processament");

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

            // Construir text de la llista de mecanismes 
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
                mecanismes.Add($"{resultatMostra.MecanismeResistencia5Id }: {resultatMostra.MecanismeResistenciaDescrip5}");

            string microorganisme = resultatMostra.AillamentDescripcio ?? "sense microorganisme";
            string textMecanismes = mecanismes.Any() ? $" [{string.Join(", ", mecanismes)}]" : " [sense mecanismes]";

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Processant resultat positiu: '{microorganisme}{textMecanismes}'");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");


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

                // Si el microorganisme és especial i no té cap mecanisme,
                // afegir un mecanisme "buit" per garantir que es processa
                var mecanismesAProcessar = mecanismesResultat
                    .Where(m => !string.IsNullOrWhiteSpace(m.Item1))
                    .ToList();

                // Si no hi ha cap mecanisme però és microorganisme especial, afegir entrada buida
                bool esMicroorganismeEspecial = resultatMostra.EsMicroorganismeEspecial ?? false;
                if (!mecanismesAProcessar.Any() && esMicroorganismeEspecial)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚡ Microorganisme especial sense mecanismes de resistència");
                    mecanismesAProcessar.Add((null, null));
                }

                // Per cada mecanisme del resultat (o l'entrada buida si és especial sense mecanismes)...
                foreach (var (mecanismeId, mecanismeDescrip) in mecanismesAProcessar)
                {
                    string infoMecanisme = string.IsNullOrWhiteSpace(mecanismeId)
                        ? "sense mecanisme"
                        : $"{mecanismeId} - {mecanismeDescrip}";

                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}---------------------------");
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Processant microorganisme [mecanisme de resistència]: '{microorganismeCodi} [{infoMecanisme}]'");
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}---------------------------");


                    // Pacients_diagnostics
                    // ------------------------------------

                    // Comprovar si ja existeix el diagnòstic
                    int diagnosticId = _multiRRepository.ComprovarDiagnosticExisteix(
                        mostra.PacientSap,
                        microorganismeCodi,
                        mecanismeId ?? "",
                        mecanismeDescrip ?? "");

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
                            
                            // Marcar que s'ha afegit un positiu NOU
                            resultat.PositiuAfegit = true;
                        }
                    }
                    else
                    {
                        resultat.DiagnosticsExistents++;
                    }


                    // Pacients_diagnostics_mostres
                    // ------------------------------------

                    // Comprovar si ja existeix la mostra positiva
                    int mostraDiagnosticId = _multiRRepository.ComprovarMostraDiagnosticExisteix(
                        mostra.PacientSap,
                        resultatMostra.DataPeticioTrunc,
                        resultatMostra.MostraDescripcio, "2", mostra.EtiquetaId);

                    int mostraDiagnosticIdFinal = mostraDiagnosticId;

                    if (mostraDiagnosticId == 0)
                    {
                        // No existeix la mostra diagnòstic positiva. Es crea

                        // Construir la combinació microorganisme + mecanisme per a captat
                        string microorganismeMecanismeCaptat = !string.IsNullOrWhiteSpace(mecanismeId)
                            ? $"{microorganismeCodi} - {mecanismeId}"
                            : microorganismeCodi;

                        // Crear mostra positiva amb el microorganisme i el mecanisme
                        int nouMostraDiagnosticId = _multiRRepository.CrearMostraDiagnostic(
                            mostra.PacientSap,
                            resultatMostra.DataPeticioTrunc,
                            resultatMostra.MostraDescripcio,
                            resultatMostra.ProvaDescripcio,
                            mostra.EtiquetaId,
                            mostra.DataUltimResultat, // agafar data resultat de la mostra (no del resultat, ja que per una mostra poden haver diferents valors)
                            resultatMostra.DataValidacio,
                            mecanismeId,
                            resultatMostra.EsMicroorganismeEspecial,
                            microorganismeMecanismeCaptat); // 260113. En Martí confirma que per als positius s´ha de desar microorganisme + mecanisme captat

                        if (nouMostraDiagnosticId > 0)
                        {
                            mostraDiagnosticIdFinal = nouMostraDiagnosticId;
                            resultat.MostresDiagnosticCreades++;
                        }
                    }
                    else
                    {
                        resultat.MostresDiagnosticExistents++;
                        
                        // La mostra diagnòstic ja existeix
                        // Unicament s'actualitza el camp microorganisme_mecanisme_captat concatenant el nou valor
                        
                        // Construir la combinació microorganisme + mecanisme per a captat
                        string nouMicroorganismeMecanismeCaptat = !string.IsNullOrWhiteSpace(mecanismeId)
                            ? $"{microorganismeCodi} - {mecanismeId}"
                            : microorganismeCodi;
                        
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Mostra diagnòstic existent. Actualitzant microorganisme_mecanisme_captat amb '{nouMicroorganismeMecanismeCaptat}'");
                        
                        bool actualitzat = _multiRRepository.ActualitzarMicroorganismeMecanismeCaptat(
                            mostraDiagnosticId,
                            nouMicroorganismeMecanismeCaptat);
                        
                        if (actualitzat)
                        {
                            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Camp microorganisme_mecanisme_captat actualitzat correctament");
                        }
                        else
                        {
                            _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ No s'ha pogut actualitzar el camp microorganisme_mecanisme_captat");
                        }
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



                    // Final OK del positiu
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






                    // Buscar altres diagnostics positius del mateix tipus de mostra, per crear mostres negatives que els contrarestin
                    // ------------------------------------

                    // Obtenir tots els diagnòstics positius del pacient per aquest tipus de mostra (excloent l'actual)
                    var altresDiagnosticsPositius = _multiRRepository.ObtenirDiagnosticsPositiusPacientPerTipusMostra(
                        mostra.PacientSap,
                        resultatMostra.MostraDescripcio,
                        mostra.EtiquetaId
                        );


                    // Obtenir tots els diagnòstics positius del pacient per aquest tipus de mostra i equivalents (excloent l'actual)
                    // 20260213. Afegim també la cerca de tipus de mostra equivalent, per assegurar que capturem tots els positius que poden formar part de la mateixa mostra (etiqueta) encara que el tipus de mostra sigui diferent però equivalent
                    var altresDiagnosticsPositiusPacientPerTipusMostraIEquivalents = _multiRRepository.ObtenirDiagnosticsPositiusPacientPerTipusMostraIEquivalents(
                        mostra.PacientSap,
                        resultatMostra.MostraDescripcio,
                        mostra.EtiquetaId
                        );


                    if (altresDiagnosticsPositius.Count() != altresDiagnosticsPositiusPacientPerTipusMostraIEquivalents.Count())
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}El nombre de positius incloent o no els equivalents, ÉS DIFERENT {altresDiagnosticsPositius.Count()} vs {altresDiagnosticsPositiusPacientPerTipusMostraIEquivalents.Count()}");
                    }

                    // Filtrar els diagnòstics que tenen alguna mostra amb la mateixa etiqueta que la mostra actual
                    // Això evita crear negatius per diagnòstics que formen part de la mateixa mostra que estem processant
                    var diagnosticsPositius = new List<int>();
                    
                    foreach (var diagId in altresDiagnosticsPositiusPacientPerTipusMostraIEquivalents)
                    {
                        // Comprovar si aquest diagnòstic té alguna mostra amb l'etiqueta actual i el mateix tipus de mostra
                        bool teMostraAmbMateixaEtiqueta = _multiRRepository.DiagnosticTeMostraAmbEtiqueta(
                            diagId,
                            mostra.EtiquetaId,
                            resultatMostra.MostraDescripcio);

                        // Obtenir informació del diagnòstic per al log
                        var infoDiagnostic = _multiRRepository.ObtenirInformDiagnostic(diagId);
                        string infoMicro = infoDiagnostic != null
                            ? $"{infoDiagnostic.MicroorganismeCodi} + {infoDiagnostic.MecanismeId}"
                            : diagId.ToString();

                        // Si NO té cap mostra amb la mateixa etiqueta, comprovar si forma part dels positius pendents de processar de la mostra actual
                        if (!teMostraAmbMateixaEtiqueta)
                        {
                            // Verificar que el diagnòstic positiu trobat NO correspon a un dels positius de la mostra que s´esta incorporant 

                            // Exemple: Si estem processant una mostra amb positius A i B, i ara processem A,
                            // no hem de crear negatiu per B perquè encara el processarem després
                            // En cas contrari s´afegiria un negatiu per B abans d´haver processat el positiu B

                            bool esPositiuPendentDeProcessar = false;
                            
                            if (infoDiagnostic != null)
                            {
                                // Recórrer tots els mecanismes de la mostra que s´està incorporant
                                foreach (var (mecPendent, mecDescripPendent) in mecanismesAProcessar)
                                {
                                    // Comprovar si el diagnòstic trobat coincideix amb algun mecanisme pendent
                                    bool microorganismeCoincideix = string.Equals(
                                        infoDiagnostic.MicroorganismeCodi, 
                                        microorganismeCodi, 
                                        StringComparison.OrdinalIgnoreCase);
                                    
                                    bool mecanismeCoincideix = string.Equals(
                                        infoDiagnostic.MecanismeId ?? "", 
                                        mecPendent ?? "", 
                                        StringComparison.OrdinalIgnoreCase);
                                    
                                    if (microorganismeCoincideix && mecanismeCoincideix)
                                    {
                                        esPositiuPendentDeProcessar = true;
                                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Diagnòstic {diagId} ({infoMicro}) DESCARTAT dels positius a neutralitzar perquè és un positiu pendent de processar en aquesta mateixa mostra");
                                        break;
                                    }
                                }
                            }
                            
                            // Només afegir a la llista si NO és un positiu pendent de processar
                            if (!esPositiuPendentDeProcessar)
                            {
                                diagnosticsPositius.Add(diagId);
                                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Diagnòstic {diagId} ({infoMicro}) AFEGIT a llista de positius a neutralitzar perquè NO forma part de la mateixa mostra (etiqueta {mostra.EtiquetaId} i tipus '{resultatMostra.MostraDescripcio}')");
                            }
                        }
                        else
                        {
                            // El diagnòstic JA té una mostra amb la mateixa etiqueta
                            // Actualitzar el camp microorganisme_mecanisme_captat de la mostra existent
                            
                            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Diagnòstic {diagId} ({infoMicro}) DESCARTAT dels positius a neutralitzar, perquè és un positiu anterior al que acabem d´incorporar (forma part de la mateixa mostra (etiqueta {mostra.EtiquetaId} i tipus '{resultatMostra.MostraDescripcio}'))");
                            
                            // Obtenir l'ID de la mostra diagnòstic associada
                            int mostraDiagnosticIdExistent = _multiRRepository.ObtenirIdMostraDiagnosticPerDiagnosticIEtiqueta(
                                diagId,
                                mostra.EtiquetaId,
                                resultatMostra.MostraDescripcio);
                            
                            if (mostraDiagnosticIdExistent > 0)
                            {
                                // Construir la combinació microorganisme + mecanisme per a captat
                                string microorganismeMecanismeCaptatActualitzar = !string.IsNullOrWhiteSpace(mecanismeId)
                                    ? $"{microorganismeCodi} - {mecanismeId}"
                                    : microorganismeCodi;
                                
                                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Actualitzant microorganisme_mecanisme_captat (si cal) de la mostra existent (ID: {mostraDiagnosticIdExistent}) amb '{microorganismeMecanismeCaptatActualitzar}'");
                                
                                bool actualitzat = _multiRRepository.ActualitzarMicroorganismeMecanismeCaptat(
                                    mostraDiagnosticIdExistent,
                                    microorganismeMecanismeCaptatActualitzar);
                                
                                if (actualitzat)
                                {
                                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Camp microorganisme_mecanisme_captat actualitzat correctament per mostra ID {mostraDiagnosticIdExistent}");
                                }
                                else
                                {
                                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ No s'ha pogut actualitzar el camp microorganisme_mecanisme_captat per mostra ID {mostraDiagnosticIdExistent}");
                                }
                            }
                            else
                            {
                                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut obtenir l'ID de la mostra diagnòstic per al diagnòstic {diagId}");
                            }
                        }
                    }


                    // En aquest punt tenim la llista de positius a contrarestar
                    if (diagnosticsPositius == null || diagnosticsPositius.Count == 0)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ NO hi ha altres diagnòstics positius a contrarestar amb negatiu, per aquest pacient i tipus de mostra");
                    }
                    else
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Trobats {diagnosticsPositius.Count} diagnòstic(s) positiu(s) a neutralitzar amb un negatiu, per aquest pacient i tipus de mostra");

                        // Mostrar detall dels diagnòstics trobats
                        foreach (var diagId in diagnosticsPositius)
                        {
                            var infoDiag = _multiRRepository.ObtenirInformDiagnostic(diagId);
                            if (infoDiag != null)
                            {
                                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}  - Diagnòstic {diagId}: {infoDiag.MicroorganismeCodi} + {infoDiag.MecanismeId}");
                            }
                        }

                        var diagnosticsPositiusANeutralitzar = diagnosticsPositius.ToList();

                        if (diagnosticsPositiusANeutralitzar.Count != 0)
                        {
                            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📋 Creant mostra NEGATIVA per {diagnosticsPositiusANeutralitzar.Count} diagnòstic(s)");

                            // Per cada altre diagnòstic positiu, crear una mostra negativa per neutralitzar-lo
                            foreach (int altDiagnosticId in diagnosticsPositiusANeutralitzar)
                            {

                                // Comprovar si el pacient té algun negatiu, per al tipus de mostra, amb la mateixa etiqueta
                                // Mostres amb més d´un positiu, si no es fa aquesta comprovació, afegirà tants negatius com positius entrin
                                // Sol ha d´entrar el primer negatiu que contraresti el positiu.

                                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Processant negatiu per al diagnòstic '{altDiagnosticId}'");
                                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Comprovant si ja existeix un negatiu incorporat amb la mateixa etiqueta...");

                                // Comprovar si ja existeix una mostra negativa (valoració '1') amb aquesta etiqueta específica
                                int mostraNegativaExistent = _multiRRepository.ComprovarMostraDiagnosticPerEtiqueta(
                                    mostra.PacientSap,
                                    resultatMostra.MostraDescripcio,
                                    "1", // Valoració '1' = negatiu
                                    mostra.EtiquetaId); // Etiqueta específica de la mostra actual


                                if (mostraNegativaExistent > 0)
                                {
                                    // Ja existeix una mostra negativa

                                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ JA existeix un negatiu per aquesta mostra (ID: {mostraNegativaExistent})");
                                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}No cal incorporar més negatius de la mateixa etiqueta");

                                    // Construir la combinació microorganisme + mecanisme per a captat
                                    string microorganismeMecanismeCaptat = !string.IsNullOrWhiteSpace(mecanismeId)
                                        ? $"{microorganismeCodi} - {mecanismeId}"
                                        : microorganismeCodi;


                                    // Crea la mostra negativa, i la mostra_diagnòstic
                                    // En aquest cas no cal crear la mostra, però si el mostra_diagnostic
                                    // Atenció: CrearMostraNegativaPerDiagnostic s´encarrega de crear els registres que calgui
                                    bool mostraNegativaCreada = CrearMostraNegativaPerDiagnostic(
                                            mostra,
                                            resultatMostra,
                                            altDiagnosticId,
                                            microorganismeMecanismeCaptat);


                                    // No deixem registre auditoria, ja que el negatiu ja existia

                                    // Actualitzant (si cal) microorganisme_mecanisme_captat
                                    //_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Mostra diagnòstic negativa ja existent. Actualitzant (si cal) microorganisme_mecanisme_captat amb '{microorganismeMecanismeCaptat}'");

                                    //bool actualitzat = _multiRRepository.ActualitzarMicroorganismeMecanismeCaptat(
                                    //    mostraNegativaExistent,
                                    //    microorganismeMecanismeCaptat);

                                    //if (actualitzat)
                                    //{
                                    //    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Camp microorganisme_mecanisme_captat actualitzat correctament");
                                    //}
                                    //else
                                    //{
                                    //    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ No s'ha pogut actualitzar el camp microorganisme_mecanisme_captat");
                                    //}


                                }
                                else
                                {
                                    // No existeix una mostra negativa. Es procedeix a crear-lo
                                    
                                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}NO existeix un negatiu per aquest diagnòstic (ID: {altDiagnosticId}) i etiqueta {mostra.EtiquetaId}");
                                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Procedint a crear negatiu");

                                    // Crea la mostra negativa, i la mostra_diagnòstic

                                    // Construir la combinació microorganisme + mecanisme per a captat
                                    string microorganismeMecanismeCaptat = !string.IsNullOrWhiteSpace(mecanismeId)
                                        ? $"{microorganismeCodi} - {mecanismeId}"
                                        : microorganismeCodi;


                                    bool mostraNegativaCreada = CrearMostraNegativaPerDiagnostic(
                                            mostra,
                                            resultatMostra,
                                            altDiagnosticId,
                                            microorganismeMecanismeCaptat);

                                    if (mostraNegativaCreada)
                                    {
                                        resultat.MostresNegativesCreades++;
                                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Mostra negativa creada per contrarestar el positiu del diagnòstic {altDiagnosticId}");
                                    }
                                    else
                                    {
                                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}❌ No s'ha pogut crear mostra negativa per contrarestar al diagnòstic {altDiagnosticId}");
                                    }

                                    // Deixem registre auditoria del negatiu creat segons un positiu (OK Negatiu contraresta Positiu)
                                    bool auditoriaNegatiuCreadaOk = _multiRRepository.InserirAuditoriaIntegracioModulab(
                                        mostra,
                                        "OKNCP",
                                        resultatMostra,
                                        new MecanismeResistenciaInfo { Id = mecanismeId });

                                    if (auditoriaNegatiuCreadaOk)
                                    {
                                        resultat.AuditoriasCreades++;
                                    }

                                }

                                // Actualitzar la data_diagnostic (de pacients_diagnostics) amb la data de mostra més antiga
                                bool dataActualitzadaDiagnosticNegatiu = _multiRRepository.ActualitzarDataDiagnosticPacientsDiagnostics(
                                    mostra.PacientSap,
                                    microorganismeCodi,
                                    mecanismeId,
                                    mecanismeDescrip ?? mecanismeId);

                                // Actualitzar la data_diagnostic (de pacients_diagnostics_mostra) amb la data de mostra més antiga
                                bool dataActualitzadaMostraNegatiu = _multiRRepository.ActualitzarDataDiagnosticPacientsDiagnosticsMostra(
                                    mostra.PacientSap,
                                    microorganismeCodi,
                                    mecanismeId,
                                    mecanismeDescrip ?? mecanismeId);

                            }

                        }

                    }

                }
            }
        }


        /// <summary>
        /// Crea una mostra negativa (si cal) per un diagnòstic positiu específic
        /// Una mostra negativa és una mostra del mateix tipus però que NO conté 
        /// el microorganisme/mecanisme del diagnòstic positiu
        /// </summary>
        private bool CrearMostraNegativaPerDiagnostic(
            Mostra mostra,
            ResultatMostra resultatMostra,
            int diagnosticPositiuId,
            string microorganismeMecanismeCaptat)
        {
            // 1. Crear la mostra diagnòstic (sense mecanisme, és negativa)

            // Comprovar si ja existeix la mostra diagnòstic negativa
            int mostraDiagnosticId = _multiRRepository.ComprovarMostraDiagnosticExisteix(
                mostra.PacientSap,
                resultatMostra.DataPeticioTrunc,
                resultatMostra.MostraDescripcio,
                "1");

            int mostraDiagnosticIdFinal = mostraDiagnosticId;

            if (mostraDiagnosticId == 0)
            {
                // Obtenir el microorganisme del resultat per incloure'l al camp microorganismeMecanismeCaptat
                //var microorganismeEntitat = _multiRRepository.ObtenirMicroorganisme(resultatMostra.AillamentDescripcio);
                //string microorganismeCodi = microorganismeEntitat?.Codi ?? resultatMostra.AillamentDescripcio ?? "";


                // Crear mostra negativa només amb el microorganisme (sense mecanisme)
                mostraDiagnosticIdFinal = _multiRRepository.CrearMostraDiagnostic(
                    mostra.PacientSap,
                    resultatMostra.DataPeticioTrunc,
                    resultatMostra.MostraDescripcio,
                    resultatMostra.ProvaDescripcio,
                    mostra.EtiquetaId,
                    mostra.DataUltimResultat, // agafar data resultat de la mostra (no del resultat, ja que per una mostra poden haver diferents valors)
                    resultatMostra.DataValidacio,
                    null, // Sense mecanisme (negativa)
                    false, // No és microorganisme especial
                    microorganismeMecanismeCaptat); // Per a negatius, només el microorganisme (sense mecanisme)
                                                    // valor anterior microorganismeCodi

                if (mostraDiagnosticIdFinal == 0)
                {
                    return false;
                }
            }
            else
            {
                // Ja existeix la mostra negativa
                // Actualitzo el camp microorganisme_mecanisme_captat concatenant el nou valor 

                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Mostra diagnòstic existent. Actualitzant (si cal) microorganisme_mecanisme_captat amb '{microorganismeMecanismeCaptat}'");

                bool actualitzat = _multiRRepository.ActualitzarMicroorganismeMecanismeCaptat(
                    mostraDiagnosticId,
                    microorganismeMecanismeCaptat);

                if (actualitzat)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Camp microorganisme_mecanisme_captat actualitzat correctament");
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ No s'ha pogut actualitzar el camp microorganisme_mecanisme_captat");
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
