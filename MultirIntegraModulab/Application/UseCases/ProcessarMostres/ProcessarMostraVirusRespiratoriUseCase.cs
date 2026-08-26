using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab.Application.UseCases.ProcessarMostres
{
    /// <summary>
    /// Resultat del processament d'una mostra de Virus Respiratori
    /// </summary>
    public class ResultatProcessamentVirusRespiratori
    {
        public bool Exitosa { get; set; }
        public string Missatge { get; set; }
        
        // Comptadors
        public int ResultatsProcessats { get; set; }
        public int DiagnosticsCreats { get; set; }
        public int DiagnosticsExistents { get; set; }
        public int MostresCreades { get; set; }
        public int MostresExistents { get; set; }
        public int RelacionsCreades { get; set; }
        public int AuditoriasCreades { get; set; }

        /// <summary>
        /// Nombre real d'OKVR inserits
        /// </summary>
        public int PositiusVirusRespiratorisIncorporats { get; set; }

        public ResultatProcessamentVirusRespiratori()
        {
            Exitosa = true;
            PositiusVirusRespiratorisIncorporats = 0;
        }

        public override string ToString()
        {
            return $"VR: {ResultatsProcessats} processats, " +
                   $"{DiagnosticsCreats} diagnòstics creats, " +
                   $"{MostresCreades} mostres creades, " +
                   $"{RelacionsCreades} relacions, " +
                   $"{AuditoriasCreades} auditories";
        }
    }

    /// <summary>
    /// Use Case per processar mostres de Virus Respiratoris (VR)
    /// 
    /// CARACTERÍSTIQUES ESPECÍFIQUES DELS VR:
    /// - SEMPRE són positius (no hi ha VR negatius)
    /// - NO tenen mecanismes de resistència (sempre null)
    /// - SEMPRE s'incorporen (sense comprovacions de comportament)
    /// - Processament simplificat vs MMR
    /// 
    /// FLUX:
    /// 1. Processar pacient (si cal)
    /// 2. Per cada resultat VR:
    ///    - Crear/Obtenir diagnòstic (microorganisme + mecanisme=null)
    ///    - Crear/Obtenir mostra diagnòstic
    ///    - Crear relació mostra_microorganisme
    ///    - Actualitzar dates
    /// 3. Generar nota curs clínic (si cal)
    /// </summary>
    public class ProcessarMostraVirusRespiratoriUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly IPacientWebService _pacientWebService;
        private readonly ILoggerService _logger;
        private readonly Infrastructure.ExternalServices.Email.EmailService _emailService;

        public ProcessarMostraVirusRespiratoriUseCase(
            IMultiRRepository multiRRepository,
            IPacientWebService pacientWebService,
            ILoggerService logger,
            Infrastructure.ExternalServices.Email.EmailService emailService = null)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _pacientWebService = pacientWebService; // Pot ser null
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService; // Pot ser null
        }

        /// <summary>
        /// Executa el processament d'una mostra de Virus Respiratori
        /// </summary>
        /// <param name="mostra">Mostra amb virus respiratoris a processar</param>
        /// <returns>Resultat del processament</returns>
        public async Task<ResultatProcessamentVirusRespiratori> ExecutarAsync(Mostra mostra)
        {
            if (mostra == null)
            {
                _logger.Warning("Intentant processar mostra VR amb mostra null");
                throw new ArgumentNullException(nameof(mostra));
            }

            var resultat = new ResultatProcessamentVirusRespiratori();

            _logger.Info($"🔄 Processant mostra amb virus respiratori : {mostra.EtiquetaId}");

            try
            {

                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Processant mostra VR amb {mostra.Resultats.Count} resultat(s) VR");
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");

                // Comprovar si la mostra té resultats
                if (mostra.Resultats == null || mostra.Resultats.Count == 0)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ Mostra sense resultats");
                    resultat.Exitosa = false;
                    resultat.Missatge = "Mostra sense resultats";
                    return resultat;
                }

                // FASE 0: COMPROVAR TIPUS DE PROVA (NOMÉS PER VR)
                // Obtenir el tipus de prova del primer resultat (tots els resultats d'una mostra tenen el mateix tipus de prova)

                string tipusProva = mostra.Resultats[0].ProvaDescripcio;
                
                _logger.Info($"🔎 Comprovant tipus de prova: '{tipusProva}'");
                
                bool permitIncorporar = _multiRRepository.TipusProvaPermitIncorporarVirusRespiratori(tipusProva);
                
                if (!permitIncorporar)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ El tipus de prova '{tipusProva}' NO existeix o bé NO permet incorporar virus respiratoris");
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ La mostra NO es processarà");
                    
                    // Inserir auditoria per cada resultat VR
                    foreach (var resultatMostra in mostra.Resultats)
                    {
                        _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "TPNIVR", resultatMostra);
                    }
                    
                    resultat.Exitosa = false;
                    resultat.Missatge = $"Tipus de prova '{tipusProva}' no permet incorporar VR";
                    return resultat;
                }
                
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}✅ Tipus de prova '{tipusProva}' permet incorporar virus respiratoris");

                // FASE 0b: COMPROVAR CENTRE (NOMÉS PER VR)
                // Obtenir el centre del primer resultat (tots els resultats d'una mostra tenen el mateix centre)
                string centreDescripcio = mostra.Resultats[0].CentreDescripcio;
                
                _logger.Info($"🔎 Comprovant si centre: '{centreDescripcio}' és un del centres configurats a Parametres / VR_CENTRES");
                
                bool centrePermitVR = _multiRRepository.ExisteixParametre("VR_CENTRES", centreDescripcio);
                
                if (!centrePermitVR)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ El centre '{centreDescripcio}' NO està configurat a Parametres / VR_CENTRES per incorporar virus respiratoris");
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}💥 La mostra NO es processarà");
                    
                    // Inserir auditoria per cada resultat VR
                    foreach (var resultatMostra in mostra.Resultats)
                    {
                        _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "CNIVR", resultatMostra);
                    }
                    
                    resultat.Exitosa = false;
                    resultat.Missatge = $"Centre '{centreDescripcio}' no permet incorporar VR";
                    return resultat;
                }
                
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}✅ Centre '{centreDescripcio}' permet incorporar virus respiratoris");

                // FASE 1: PROCESSAR PACIENT
                _logger.Info($"🔎 Comprovant / creant pacient: {mostra.PacientSap}");
                
                bool pacientProcessat = await ProcessarPacientAsync(mostra);
                
                if (!pacientProcessat)
                {
                    // _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha pogut processar el pacient");
                    resultat.Exitosa = false;
                    resultat.Missatge = "Error processant pacient";
                    return resultat;
                }

                // FASE 2: PROCESSAR CADA RESULTAT VR (TOTS SÓN POSITIUS)
                foreach (var resultatMostra in mostra.Resultats)
                {
                    // Saltar resultats sense microorganisme
                    if (string.IsNullOrWhiteSpace(resultatMostra.AillamentDescripcio))
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ Resultat sense microorganisme - s'omet");
                        continue;
                    }

                    await ProcessarResultatVirusRespiratoriAsync(mostra, resultatMostra, resultat);
                    resultat.ResultatsProcessats++;
                }

                // FASE 3: GENERAR NOTA CURS CLÍNIC (si s'han creat o ja existien diagnòstics)
                if (resultat.DiagnosticsCreats > 0 || resultat.DiagnosticsExistents > 0)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}📝 Procedint a crear nota VR del curs clínic...");
                    
                    bool notaCreada = _multiRRepository.AfegirNotaCursClinicVirusRespiratoriSiCal(mostra.PacientSap, true);
                    
                    if (notaCreada)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✔️ Nota curs clínic VR creada");
                    }
                    else
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}ℹ️ No s'ha creat nota curs clínic VR");
                    }
                }

                resultat.Missatge = $"VR processada correctament: {resultat.ResultatsProcessats} resultats";
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}✔️ Mostra VR {mostra.EtiquetaId} processada: {resultat}");

                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Error processant mostra VR {mostra.EtiquetaId}", ex);
                resultat.Exitosa = false;
                resultat.Missatge = $"Error: {ex.Message}";
                return resultat;
            }
        }

        /// <summary>
        /// Processa el pacient: comprova existència i crea si cal
        /// Reutilitza lògica similar a MMR
        /// </summary>
        private async Task<bool> ProcessarPacientAsync(Mostra mostra)
        {
            try
            {
                // Comprovar si el pacient ja existeix a MultiR
                bool existeix = _multiRRepository.ExisteixPacient(mostra.PacientSap);

                if (existeix)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✓ Pacient {mostra.PacientSap} ja existeix a MultiR");
                    return true;
                }

                // Si no existeix, intentar recuperar-lo del WebService
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ Pacient {mostra.PacientSap} no existeix - consultant WebService...");

                if (_pacientWebService == null)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ WebService no disponible");
                    return false;
                }

                var dadesPacient = _pacientWebService.ObtenirDadesPacient(mostra.PacientSap);

                if (dadesPacient == null)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'han pogut obtenir dades del pacient {mostra.PacientSap} des del web service. Es crearà pacient amb dades de Modulab");
                    bool pacientCreat = CrearPacientDesDeDadesModulab(mostra, "pacient no trobat al web service");
                    if (pacientCreat)
                    {
                        EnviarAlertaPacientNoTrobatWsSap(mostra);
                    }

                    return pacientCreat;
                }

                // Inserir pacient
                bool inserit = _multiRRepository.InserirPacient(dadesPacient);

                if (inserit)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Pacient {mostra.PacientSap} creat a MultiR");
                    return true;
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Error creant pacient");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error processant pacient {mostra.PacientSap}", ex);
                return CrearPacientDesDeDadesModulab(mostra, "error consultant el web service");
            }
        }

        private bool CrearPacientDesDeDadesModulab(Mostra mostra, string motiu)
        {
            string pacientSap = mostra?.PacientSap;
            var primerResultat = mostra?.Resultats?[0];

            _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'han pogut obtenir dades completes del pacient {pacientSap} ({motiu}). Es crearà pacient amb dades de Modulab");

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

                List<string> destinataris = _multiRRepository.ObtenirValorsPerClau("EMAIL_PACIENT_NO_TROBAT_WS_SAP");
                if (destinataris == null || destinataris.Count == 0)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No hi ha destinataris a parametres_aplicacio per la clau EMAIL_PACIENT_NO_TROBAT_WS_SAP");
                    return;
                }

                string subject = $"⚠️ MultiR - Pacient no trobat a SAP - Mostra {mostra?.EtiquetaId}";
                string body =
                    "S'ha incorporat una mostra amb pacient no trobat al web service SAP." + Environment.NewLine +
                    Environment.NewLine +
                    $"Etiqueta mostra: {mostra?.EtiquetaId}" + Environment.NewLine +
                    $"Pacient SAP: {mostra?.PacientSap}" + Environment.NewLine +
                    $"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}" + Environment.NewLine +
                    Environment.NewLine +
                    "El pacient s'ha creat a MultiR amb dades provinents de la consulta Modulab.";

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
            catch (Exception ex)
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Error enviant alerta de pacient no trobat a SAP: {ex.Message}");
            }
        }

        /// <summary>
        /// Processa un resultat individual de Virus Respiratori
        /// IMPORTANT: Els VR NO tenen mecanismes de resistència (sempre null)
        /// </summary>
        private async Task ProcessarResultatVirusRespiratoriAsync(
            Mostra mostra,
            ResultatMostra resultatMostra,
            ResultatProcessamentVirusRespiratori resultat)
        {
            string microorganisme = resultatMostra.AillamentDescripcio;

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Processant Virus Respiratori : '{microorganisme}'");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");

            try
            {
                // 1. COMPROVAR/CREAR MICROORGANISME
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔎 Comprovant / creant microorganisme VR");
                
                bool microorganismeExisteix = _multiRRepository.ComprovarICrearMicroorganisme(microorganisme);
                
                if (!microorganismeExisteix)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha pogut crear microorganisme VR");
                    return;
                }

                // 2. COMPROVAR/CREAR DIAGNÒSTIC (mecanisme = null per VR)
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔎 Comprovant / creant diagnòstic VR");
                
                int diagnosticId = _multiRRepository.ComprovarDiagnosticExisteix(
                    mostra.PacientSap,
                    microorganisme,
                    mecanisme: null,  // ⚠️ VR NO tenen mecanisme
                    tipusMecanisme: null);

                bool diagnosticNou = false;

                if (diagnosticId == 0)
                {
                    // Crear nou diagnòstic
                    diagnosticId = _multiRRepository.CrearDiagnosticPacient(
                        mostra.PacientSap,
                        microorganisme,
                        mecanisme: null,  // ⚠️ VR NO tenen mecanisme
                        tipusMecanisme: null);

                    if (diagnosticId > 0)
                    {
                        diagnosticNou = true;
                        resultat.DiagnosticsCreats++;
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Diagnòstic VR creat (ID: {diagnosticId})");
                    }
                    else
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Error creant diagnòstic VR");
                        return;
                    }
                }
                else
                {
                    resultat.DiagnosticsExistents++;
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}ℹ️ Diagnòstic VR ja existeix (ID: {diagnosticId})");
                }

                // 3. COMPROVAR/CREAR MOSTRA DIAGNÒSTIC
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔎 Comprovant / creant mostra diagnòstic VR");
                
                int mostraDiagnosticId = _multiRRepository.ComprovarMostraDiagnosticExisteix(
                    mostra.PacientSap,
                    resultatMostra.DataPeticioTrunc,
                    resultatMostra.MostraDescripcio,
                    valoracio: "2",  // VR sempre positiu
                    etiqueta: mostra.EtiquetaId);

                bool mostraDiagnosticNova = false;

                if (mostraDiagnosticId == 0)
                {
                    // Crear nova mostra diagnòstic
                    mostraDiagnosticId = _multiRRepository.CrearMostraDiagnostic(
                        mostra.PacientSap,
                        resultatMostra.DataPeticioTrunc,
                        resultatMostra.MostraDescripcio,
                        resultatMostra.ProvaDescripcio,
                        mostra.EtiquetaId,
                        mostra.DataUltimResultat,
                        resultatMostra.DataValidacio,
                        mecanismeId: null,  // ⚠️ VR NO tenen mecanisme
                        esMicroorganismeEspecial: false,  // No aplica per VR
                        microorganismeMecanismeCaptat: microorganisme);  // Només microorganisme

                    if (mostraDiagnosticId > 0)
                    {
                        mostraDiagnosticNova = true;
                        resultat.MostresCreades++;
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Mostra diagnòstic VR creada (ID: {mostraDiagnosticId})");
                    }
                    else
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Error creant mostra diagnòstic VR");
                        return;
                    }
                }
                else
                {
                    resultat.MostresExistents++;
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}ℹ️ Mostra diagnòstic VR ja existeix (ID: {mostraDiagnosticId})");
                }

                // 4. CREAR RELACIÓ MOSTRA_MICROORGANISME
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔎 Comprovant / creant relació mostra-microorganisme VR");
                
                bool relacioCreada = _multiRRepository.CrearMostraMicroorganisme(diagnosticId, mostraDiagnosticId);

                if (relacioCreada)
                {
                    resultat.RelacionsCreades++;
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Relació creada");
                }
                else
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}ℹ️ Relació ja existia");
                }

                // 5. ACTUALITZAR DATES DIAGNÒSTIQUES
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔄 Actualitzant data_diagnostic (pacients_diagnostics) per al pacient");
                
                _multiRRepository.ActualitzarDataDiagnosticPacientsDiagnostics(
                    mostra.PacientSap,
                    microorganisme,
                    mecanismeId: null,  // ⚠️ VR NO tenen mecanisme
                    tipusMecanisme: null);

                _multiRRepository.ActualitzarDataDiagnosticPacientsDiagnosticsMostra(
                    mostra.PacientSap,
                    microorganisme,
                    mecanismeId: null,  // ⚠️ VR NO tenen mecanisme
                    tipusMecanisme: null);


                // 6. INSERIR AUDITORIA
                bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
                    mostra,
                    "OKVR",  // Codi específic per VR
                    resultatMostra);

                if (auditoriaCreada)
                {
                    resultat.AuditoriasCreades++;
                    resultat.PositiusVirusRespiratorisIncorporats++;
                }

                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✅ VR '{microorganisme}' processat correctament");
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error processant VR '{microorganisme}'", ex);
            }
        }
    }
}
