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

        /// <summary>
        /// Nombre real de positius incorporats (auditories OKP)
        /// </summary>
        public int PositiusIncorporats { get; set; }

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

        /// <summary>
        /// Nombre real de negatius contraresta positiu incorporats (auditories OKNCP)
        /// </summary>
        public int NegatiusContrarestaPositiuIncorporats { get; set; }

        public ResultatProcessamentPositiu()
        {
            Exitosa = true;
            PositiuAfegit = false;
            PositiusIncorporats = 0;
            NegatiusContrarestaPositiuIncorporats = 0;
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
        private readonly Infrastructure.ExternalServices.Email.EmailService _emailService;

        public ProcessarMostraPositivaUseCase(
            IMultiRRepository multiRRepository,
            IPacientWebService pacientWebService,
            ILoggerService logger,
            Infrastructure.ExternalServices.Email.EmailService emailService = null)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _pacientWebService = pacientWebService; // Pot ser null si no està configurat
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService; // Pot ser null si no està configurat
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
                // FASE 1: COMPROVAR / CREAR PACIENT
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

                // FASE 3: SINCRONITZAR microorganisme_mecanisme_captat per TOTES les mostres de la mateixa etiqueta
                // Quan s'han processat múltiples mecanismes per la mateixa etiqueta, 
                // cal que TOTES les mostres diagnòstiques tenguin el MATEIX valor amb TOTS els mecanismes
                SincronitzarMicroorganismeMecanismeCaptatPerEtiqueta(mostra, resultat);

                // Resultat final
                if (resultat.Exitosa)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}✅ Mostra positiva {mostra.EtiquetaId} processada correctament: " +
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
            _logger.Info($"🔎 Comprovant / creant pacient: {mostra.PacientSap}");

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
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✓ Pacient {mostra.PacientSap} JA existeix a MultiR");
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
                        }
                    }
                    else
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Pacient {mostra.PacientSap} no trobat al web service SAP. Es crearà pacient amb les dades de Modulab");

                        // Es crea pacient amb les dades de Modulab
                        bool pacientCreat = CrearPacientDesDeDadesModulab(mostra, "pacient no trobat al web service");

                        if (pacientCreat)
                        {
                            resultat.PacientCreat = true;
                            EnviarAlertaPacientNoTrobatWsSap(mostra);
                        }
                        else
                        {
                            resultat.Exitosa = false;
                            resultat.PacientCreat = false;
                            resultat.Missatge = "No s'ha pogut crear pacient amb dades de Modulab";
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Error consultant/inserint pacient via web service: {ex.Message}");
                    _logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Detall error: {ex}");

                    bool pacientCreat = CrearPacientDesDeDadesModulab(mostra, "error consultant el web service");

                    if (pacientCreat)
                    {
                        resultat.PacientCreat = true;
                    }
                    else
                    {
                        resultat.Exitosa = false;
                        resultat.PacientCreat = false;
                        resultat.Missatge = "No s'ha pogut crear pacient amb dades de Modulab";
                        return;
                    }
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

        private bool CrearPacientDesDeDadesModulab(Mostra mostra, string motiu)
        {
            string pacientSap = mostra?.PacientSap;
            var primerResultat = mostra?.Resultats?.FirstOrDefault();

            _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'han pogut obtenir dades del WebService de SAP del pacient {pacientSap} ({motiu}). Es crearà pacient amb dades de Modulab");

            var dadesPacient = new DadesPacient
            {
                PacientSap = pacientSap,
                Nom = primerResultat?.PacientNom,
                Cognom1 = primerResultat?.PacientCognom1,
                Cognom2 = primerResultat?.PacientCognom2,
                Sexe = TransformarSexeModulab(primerResultat?.PacientSexe),
                Cip = primerResultat?.PacientCip
            };

            bool inserit = _multiRRepository.InserirPacient(dadesPacient);

            if (!inserit && _multiRRepository.ExisteixPacient(pacientSap))
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ℹ️ Pacient {pacientSap} ja existeix després de l'intent de creació");
                return true;
            }

            if (inserit)
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Pacient {pacientSap} creat correctament amb dades de Modulab");
                return true;
            }

            _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠ No s'ha pogut crear pacient {pacientSap} amb dades de Modulab");
            return false;
        }

        private string TransformarSexeModulab(string sexeModulab)
        {
            if (string.IsNullOrWhiteSpace(sexeModulab))
            {
                return null;
            }

            string sexeNormalitzat = sexeModulab.Trim().ToUpperInvariant();

            if (sexeNormalitzat == "M")
            {
                return "H";
            }

            if (sexeNormalitzat == "F")
            {
                return "D";
            }

            return sexeNormalitzat;
        }

        private void EnviarAlertaPacientNoTrobatWsSap(Mostra mostra)
        {
            try
            {
                if (_emailService == null)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ EmailService no configurat. No s'envia alerta per pacient no trobat a SAP");
                    return;
                }

                var destinataris = _multiRRepository.ObtenirValorsPerClau("EMAIL_PACIENT_NO_TROBAT_WS_SAP");
                if (destinataris == null || !destinataris.Any())
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No hi ha destinataris a parametres_aplicacio per la clau EMAIL_PACIENT_NO_TROBAT_WS_SAP");
                    return;
                }

                string subject = $"⚠️ MultiR - Pacient no trobat a SAP - Mostra {mostra?.EtiquetaId}";
                string body =
                    "ATENCIÓ: S'ha incorporat a MultiR una mostra de Modulab d´un pacient del que no s´han trobat dades a SAP." + Environment.NewLine +
                    Environment.NewLine +
                    $"Etiqueta mostra: {mostra?.EtiquetaId}" + Environment.NewLine +
                    $"Pacient SAP: {mostra?.PacientSap} {mostra?.Resultats[0].PacientNom} {mostra?.Resultats[0].PacientCognom1} {mostra?.Resultats[0].PacientCognom2} " + Environment.NewLine +
                    $"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}" + Environment.NewLine +
                    Environment.NewLine +
                    "El pacient s'ha creat a MultiR amb dades provinents de Modulab. Cal revisar-ho" + Environment.NewLine + Environment.NewLine +
                    "Queden pendents per part vostra fer les accions habituals a SAP";

                if (_emailService == null)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ EmailService no configurat. No s'envia alerta per pacient no trobat a SAP");
                }
                else
                {
                    bool enviat = _emailService.EnviarEmailPersonalitzat(subject, body, destinataris, true);
                    if (enviat)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📧 Alerta enviada per pacient no trobat a SAP ({mostra?.PacientSap})");
                    }
                    else
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut enviar alerta per pacient no trobat a SAP ({mostra?.PacientSap})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Error enviant alerta de pacient no trobat a SAP: {ex.Message}");
            }
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

                        // Marcar que s'ha afegit un positiu nou per crear després el curs clinic
                        resultat.PositiuAfegit = true;

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

                            // Actualitzar quantitat de targetes en seguiments oberts (només per Multiresistent)
                            // Comprovar si el microorganisme és de tipus Multiresistent
                            var tipusMicroorganisme = _multiRRepository.ObtenirTipusMicroorganisme(resultatMostra.AillamentDescripcio);

                            if (tipusMicroorganisme == Domain.Enums.TipusMicroorganisme.Multiresistent)
                            {
                                _logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Actualitzant targetes de seguiment per mostra positiva MultiResistent...");

                                try
                                {
                                    bool targeteActualitzades = _multiRRepository.ActualitzarQuantitatTargetes(
                                        mostra.PacientSap,
                                        resultatMostra.MostraDescripcio);

                                    if (targeteActualitzades)
                                    {
                                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✅ Targetes de seguiment actualitzades correctament");
                                    }
                                }
                                catch (Exception exTargetes)
                                {
                                    // No deixem que un error en actualització de targetes bloquegi el processament
                                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Error actualitzant targetes: {exTargetes.Message}");
                                }

                                // Actualitzar data última mostra en seguiments oberts
                                try
                                {
                                    _multiRRepository.ActualitzarDataUltimaMostra(
                                        mostra.PacientSap,
                                        resultatMostra.MostraDescripcio);
                                }
                                catch (Exception exData)
                                {
                                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Error actualitzant data última mostra: {exData.Message}");
                                }
                            }
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

                        // NOTA: No actualitzem aquí. La sincronització es farà en FASE 3 (SincronitzarMicroorganismeMecanismeCaptatPerEtiqueta)
                        // per assegurar que TOTES les mostres de la mateixa etiqueta tinguin el MATEIX valor consolidat
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Mostra diagnòstic existent. Será actualitzada en la FASE 3 de sincronització (ID: {mostraDiagnosticId})");
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
                        resultat.PositiusIncorporats++;
                    }

                    // Incrementar comptador de mecanismes processats
                    resultat.MecanismesProcessats++;




                    // Comprovació i cancel·lació de mostres negatives anteriors
                    // ------------------------------------
                    
                    // Si existeix una mostra NEGATIVA anterior per al MATEIX diagnòstic amb mateixa etiqueta, cancel·lar-la

                    // Una mostra no pot tenir positiu i negatiu per al mateix diagnòstic i mateixa etiqueta simultàniament
                    // Aquesta comprovació ha de fer-se AQUÍ, just després de crear el positiu i ABANS de buscar altres diagnòstics
                    // perquè el positiu té prioritat i hauria de cancel·lar negatius anteriors


                    string microorganismeMecanismeABuscar = !string.IsNullOrWhiteSpace(mecanismeId)
                        ? $"{microorganismeCodi} - {mecanismeId}"
                        : microorganismeCodi;

                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔎 Comprovant si existeix mostra NEGATIVA anterior per al diagnòstic '{microorganismeMecanismeABuscar}' amb etiqueta '{mostra.EtiquetaId}'");

                    int mostraNegativaAnterior = _multiRRepository.ComprovarMostraNegativaPerDiagnostic(
                        mostra.PacientSap,
                        microorganismeMecanismeABuscar,
                        mostra.EtiquetaId);

                    if (mostraNegativaAnterior > 0)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Trobada mostra NEGATIVA anterior (ID: {mostraNegativaAnterior}) que cal cancel·lar");

                        bool negatiuCancelat = _multiRRepository.CancelarMostraDiagnostic(mostraNegativaAnterior);

                        if (negatiuCancelat)
                        {
                            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Mostra negativa anterior cancel·lada correctament al incorporar el positiu");
                        }
                        else
                        {
                            _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}❌ Error cancel·lant la mostra negativa anterior (ID: {mostraNegativaAnterior})");
                        }
                    }
                    else
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ No hi ha mostra negativa anterior a cancel·lar per al diagnòstic '{microorganismeMecanismeABuscar}' amb etiqueta '{mostra.EtiquetaId}'");
                    }



                    // Buscar altres diagnostics positius del mateix tipus de mostra, per crear mostres negatives que els contrarestin
                    // ------------------------------------

                    // Obtenir tots els diagnòstics positius del pacient per aquest tipus de mostra i equivalents (excloent l'actual)
                    // Afegim la cerca de tipus de mostra equivalent, per assegurar que capturem tots els positius que poden formar part de la mateixa mostra (etiqueta)
                    // encara que el tipus de mostra sigui diferent però equivalent

                    var altresDiagnosticsPositiusPacientPerTipusMostraIEquivalents = _multiRRepository.ObtenirDiagnosticsPositiusPacientPerTipusMostraIEquivalents(
                        mostra.PacientSap,
                        resultatMostra.MostraDescripcio,
                        mostra.EtiquetaId
                        );


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
                                //string microorganismeMecanismeCaptatActualitzar = !string.IsNullOrWhiteSpace(mecanismeId)
                                    //? $"{microorganismeCodi} - {mecanismeId}"
                                    //: microorganismeCodi;

                                // NOTA: No actualitzem aquí. La sincronització es farà en FASE 3 (SincronitzarMicroorganismeMecanismeCaptatPerEtiqueta)
                                // per assegurar que TOTES les mostres de la mateixa etiqueta tinguin el MATEIX valor consolidat
                                //_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Mostra diagnòstic existent (ID: {mostraDiagnosticIdExistent}). Será actualitzada en la FASE 3 de sincronització");
                            }
                            else
                            {
                                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut obtenir l'ID de la mostra diagnòstic per al diagnòstic {diagId}");
                            }
                        }
                    }


                    // En aquest punt tenim la llista de diagnòstics positius a contrarestar
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
                                        resultat.NegatiusContrarestaPositiuIncorporats++;
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

                // NOTA: No actualitzem aquí. La sincronització es farà en FASE 3 (SincronitzarMicroorganismeMecanismeCaptatPerEtiqueta)
                // per assegurar que TOTES les mostres de la mateixa etiqueta tinguin el MATEIX valor consolidat
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Mostra diagnòstic negativa existent (ID: {mostraDiagnosticId}). Será actualitzada en la FASE 3 de sincronització");
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


        /// <summary>
        /// Sincronitza el camp microorganisme_mecanisme_captat per TOTES les mostres diagnòstiques de la mateixa etiqueta.
        /// Això assegura que quan hi ha múltiples mecanismes per a un mateix resultat/microorganisme,
        /// totes les mostres creades tenguin el MATEIX valor amb la concatenació de TOTS els mecanismes.
        /// </summary>
        /// <param name="mostra">Mostra amb l'etiqueta</param>
        /// <param name="resultat">Resultat del processament</param>
        private void SincronitzarMicroorganismeMecanismeCaptatPerEtiqueta(Mostra mostra, ResultatProcessamentPositiu resultat)
        {
            if (mostra == null || string.IsNullOrWhiteSpace(mostra.EtiquetaId))
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ SincronitzarMicroorganismeMecanismeCaptatPerEtiqueta: mostra o etiqueta és null");
                return;
            }

            try
            {
                // Recopilar TOTS els mecanismes de TOTS els resultats de la mostra
                var mecanismesConsolidats = new Dictionary<string, HashSet<string>>(); // microorganisme -> set de mecanismes

                foreach (var resultatMostra in mostra.Resultats)
                {
                    if (string.IsNullOrWhiteSpace(resultatMostra.AillamentDescripcio))
                        continue;

                    var microorganismeEntitat = _multiRRepository.ObtenirMicroorganisme(resultatMostra.AillamentDescripcio);
                    string microorganismeCodi = microorganismeEntitat?.Codi ?? resultatMostra.AillamentDescripcio;

                    if (!mecanismesConsolidats.ContainsKey(microorganismeCodi))
                    {
                        mecanismesConsolidats[microorganismeCodi] = new HashSet<string>();
                    }

                    // Afegir tots els mecanismes no buits
                    if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia1Id))
                        mecanismesConsolidats[microorganismeCodi].Add($"{microorganismeCodi} - {resultatMostra.MecanismeResistencia1Id}");
                    if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia2Id))
                        mecanismesConsolidats[microorganismeCodi].Add($"{microorganismeCodi} - {resultatMostra.MecanismeResistencia2Id}");
                    if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia3Id))
                        mecanismesConsolidats[microorganismeCodi].Add($"{microorganismeCodi} - {resultatMostra.MecanismeResistencia3Id}");
                    if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia4Id))
                        mecanismesConsolidats[microorganismeCodi].Add($"{microorganismeCodi} - {resultatMostra.MecanismeResistencia4Id}");
                    if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia5Id))
                        mecanismesConsolidats[microorganismeCodi].Add($"{microorganismeCodi} - {resultatMostra.MecanismeResistencia5Id}");

                    // Si no hi ha mecanismes però el microorganisme és especial, afegir només el codi
                    var tesMecanismes = new[] {
                        resultatMostra.MecanismeResistencia1Id,
                        resultatMostra.MecanismeResistencia2Id,
                        resultatMostra.MecanismeResistencia3Id,
                        resultatMostra.MecanismeResistencia4Id,
                        resultatMostra.MecanismeResistencia5Id
                    }.Any(m => !string.IsNullOrWhiteSpace(m));

                    if (!tesMecanismes && (resultatMostra.EsMicroorganismeEspecial ?? false))
                    {
                        mecanismesConsolidats[microorganismeCodi].Add(microorganismeCodi);
                    }
                }

                // Construir el valor complet de microorganisme_mecanisme_captat
                var todosLosMecanismes = new List<string>();
                foreach (var mecanismesDelMicro in mecanismesConsolidats.Values)
                {
                    todosLosMecanismes.AddRange(mecanismesDelMicro);
                }

                if (todosLosMecanismes.Any())
                {
                    string microorganismeMecanismeCaptatComplet = string.Join(", ", todosLosMecanismes.OrderBy(x => x));

                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🔄 Sincronitzant microorganieme captat de les mostres amb etiqueta '{mostra.EtiquetaId}'");
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Microorganisme captat complet: '{microorganismeMecanismeCaptatComplet}'");

                    // Obtenir els IDs de les mostres diagnòstiques que seran actualitzades
                    var idsmostresDiagnostic = _multiRRepository.ObtenirIdsMostresDiagnosticPerEtiqueta(mostra.EtiquetaId);

                    if (idsmostresDiagnostic.Any())
                    {
                        string idsFormatats = string.Join(", ", idsmostresDiagnostic);
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}IDs de mostres a actualitzar: [{idsFormatats}]");
                    }

                    // Actualitzar TOTES les mostres de la mateixa etiqueta
                    int mostreActualitzades = _multiRRepository.ActualitzarMicroorganismeMecanismeCaptarPerEtiqueta(
                        mostra.EtiquetaId,
                        microorganismeMecanismeCaptatComplet);

                    if (mostreActualitzades > 0)
                    {
                        string idsActualitzats = string.Join(", ", idsmostresDiagnostic);
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Sincronitzat microorganisme captat per {mostreActualitzades} mostra(es): [{idsActualitzats}]");
                    }
                    else if (mostreActualitzades == 0)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ℹ️ No hi ha mostres amb etiqueta '{mostra.EtiquetaId}' per actualitzar");
                    }
                    else
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Error sincronitzant mostres amb etiqueta '{mostra.EtiquetaId}'");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Error sincronitzant microorganisme captat per etiqueta {mostra.EtiquetaId}", ex);
            }
        }

    }
}
