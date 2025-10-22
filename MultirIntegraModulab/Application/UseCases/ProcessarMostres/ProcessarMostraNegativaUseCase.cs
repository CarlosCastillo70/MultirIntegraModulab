using MultirIntegraModulab.Application.UseCases.ClassificarMostres;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MultirIntegraModulab.Application.UseCases.ProcessarMostres
{
    /// <summary>
    /// Resultat del processament d'una mostra negativa
    /// </summary>
    public class ResultatProcessamentNegatiu
    {
        public bool Exitosa { get; set; }
        public string Missatge { get; set; }
        public bool AuditoriaCreada { get; set; }
        
        // Comptadors detallats
        public int DiagnosticsCreats { get; set; }
        public int DiagnosticsExistents { get; set; }
        public int MostresDiagnosticCreades { get; set; }
        public int MostresDiagnosticExistents { get; set; }
        public int RelacionsCreades { get; set; }
        public int RelacionsDuplicades { get; set; }
        public int ResultatsProcessats { get; set; }
        public int IntegracionsCreades { get; set; }
        public int AuditoriasCreades { get; set; }
        public int ResultatsNoIncorporats { get; set; }
        
        // Comptadors per comprovacions
        public int IncorporatsPerComprovacio1 { get; set; }
        public int IncorporatsPerComprovacio2 { get; set; }

        public ResultatProcessamentNegatiu()
        {
            Exitosa = true;
        }
    }

    /// <summary>
    /// Tipus de comprovació que ha determinat que cal incorporar un negatiu
    /// </summary>
    public enum TipusComprovacioNegatiu
    {
        /// <summary>
        /// No cal incorporar el negatiu
        /// </summary>
        Cap = 0,
        
        /// <summary>
        /// Comprovació 1: Tipus de mostra amb comportament 1 i pacient amb positius
        /// </summary>
        Comprovacio1 = 1,
        
        /// <summary>
        /// Comprovació 2: Pacient amb positius vigents per aquest tipus de mostra o equivalents
        /// </summary>
        Comprovacio2 = 2
    }

    /// <summary>
    /// Use Case per processar una mostra amb un sol resultat negatiu
    /// Les mostres negatives en principi no ens interessa incorporar-los, a excepció d´alguns casos 
    /// Per veure si ens interessa o no incorporar el negatiu, s'han de fer dos comprovacions, que es detallen a continuació
    /// Si es compleix alguna de les dues comprovacions, llavors voldrà dir que si que ens interessa incorporar el negatiu
    /// Comprovació 1 Tipus de mostra a incorporar sempre que hi hagi qualsevol positiu 
    /// Tipus de mostra a incorporar si el pacient ha tingut algun positiu per aquest tipus de mostra, i el positiu és vigent
    /// </summary>
    public class ProcessarMostraNegativaUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly ILoggerService _logger;

        public ProcessarMostraNegativaUseCase(
            IMultiRRepository multiRRepository,
            ILoggerService logger)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executa el processament d'una mostra amb un sol rsultat negatiu
        /// </summary>
        /// <param name="mostra">Mostra a processar</param>
        /// <param name="classificacio">Classificació de la mostra</param>
        /// <returns>Resultat del processament</returns>
        public async Task<ResultatProcessamentNegatiu> ExecutarAsync(
            Mostra mostra,
            ResultatClassificacio classificacio)
        {
            if (mostra == null)
            {
                _logger.Warning("Intentant processar mostra negativa amb mostra null");
                throw new ArgumentNullException(nameof(mostra));
            }

            if (classificacio == null)
            {
                _logger.Warning("Intentant processar mostra negativa amb classificació null");
                throw new ArgumentNullException(nameof(classificacio));
            }

            // Comprovar si hi ha un o més resultats negatius
            if (classificacio.ResultatsNegatius == 0)
            {
                _logger.Warning($"No hi ha resultats negatius a la mostra {mostra.EtiquetaId}");
                return new ResultatProcessamentNegatiu
                {
                    Exitosa = false,
                    Missatge = "No hi ha resultats negatius per processar"
                };
            }

            _logger.Info($"🔄 Processant mostra amb {classificacio.ResultatsNegatius} negatiu/s: {mostra.EtiquetaId}");

            var resultat = new ResultatProcessamentNegatiu();

            try
            {

                // FASE 1: PROCESSAR CADA RESULTAT NEGATIU
                // Nota: En una mostra amb un sol resultat negatiu, processem tots els resultats
                foreach (var resultatMostra in mostra.Resultats)
                {
                    ProcessarResultatNegatiu(mostra, resultatMostra, resultat);
                }

                if (resultat.Exitosa)
                {
                    _logger.Info($"Mostra negativa {mostra.EtiquetaId} processada correctament: " +
                        $"{resultat.DiagnosticsCreats} diagnòstics creats, {resultat.DiagnosticsExistents} diagnòstics existents, " +
                        $"{resultat.MostresDiagnosticCreades} mostres creades, {resultat.MostresDiagnosticExistents} mostres existents, " +
                        $"{resultat.RelacionsCreades} relacions creades, {resultat.RelacionsDuplicades} duplicades, " +
                        $"{resultat.ResultatsProcessats} resultats processats, {resultat.ResultatsNoIncorporats} no incorporats, " +
                        $"{resultat.IncorporatsPerComprovacio1} incorporats per comprovació 1 (comportament 1), " +
                        $"{resultat.IncorporatsPerComprovacio2} incorporats per comprovació 2 (comportament 0), " +
                        $"{resultat.AuditoriasCreades} auditories");

                    resultat.Missatge = "Mostra negativa processada correctament";
                }


                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processant mostra negativa {mostra.EtiquetaId}", ex);
                resultat.Exitosa = false;
                resultat.Missatge = $"Error: {ex.Message}";
                return resultat;
            }
        }

        /// <summary>
        /// Processa un resultat negatiu individual (ResultatMostra)
        /// </summary>
        private void ProcessarResultatNegatiu(
            Mostra mostra,
            ResultatMostra resultatMostra,
            ResultatProcessamentNegatiu resultat)
        {
            string microorganisme = resultatMostra.AillamentDescripcio ?? "sense microorganisme";
            
            _logger.Info($"  Processant resultat negatiu: {microorganisme}");

            
            // FASE 1: COMPROVACIONS PER DETERMINAR SI CAL INCORPORAR EL NEGATIU
            // ------------------------------------
            
            _logger.Info($"  🔍 Comprovant si cal incorporar el negatiu per tipus mostra: {resultatMostra.MostraDescripcio}");
            
            bool calIncorporarNegatiu = false;
            TipusComprovacioNegatiu tipusComprovacio = TipusComprovacioNegatiu.Cap;


            // Comprovació 0: Comprovar si tenim el pacient a la taula de pacients
            // ------------------------------------
            
            _logger.Info($"   Aplicant Comprovació 0: Verificant existència del pacient {mostra.PacientSap}");
            
            bool pacientExisteix = _multiRRepository.ExisteixPacient(mostra.PacientSap);
            
            if (!pacientExisteix)
            {
                _logger.Info($"   ⚠️ Pacient {mostra.PacientSap} no existeix a la taula de pacients");
                
                // Inserir auditoria amb codi NMRCMC (No supera la comprovació de mostra amb motiu client)
                bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "NMRCMC", null, resultatMostra);
                
                if (auditoriaCreada)
                {
                    resultat.AuditoriasCreades++;
                }
                
                resultat.ResultatsNoIncorporats++;
                return;
            }
            
            _logger.Info($"   ✔️ Pacient {mostra.PacientSap} existeix a la taula de pacients");


            // Comprovació 1: Tipus de mostra a incorporar sempre que el pacient tingui algun positiu
            // ------------------------------------

            _logger.Info($"   Aplicant Comprovació 1: Positius vigents per qualsevol tipus de mostra");

            // Obtenir el comportament del tipus de mostra
            int? comportament = _multiRRepository.ObtenirComportamentTipusMostra(resultatMostra.MostraDescripcio);
            
            if (comportament.HasValue && comportament.Value == 1)
            {
                // Comprovar si el pacient té algun positiu previ (per qualsevol tipus de mostra)
                bool pacientTePositius = _multiRRepository.PacientTePositiusAlgunTipusMostra(mostra.PacientSap);
                
                if (pacientTePositius)
                {
                    calIncorporarNegatiu = true;
                    tipusComprovacio = TipusComprovacioNegatiu.Comprovacio1;
                }
            }
            else
            {
                if (comportament.HasValue)
                {
                    _logger.Info($"   Tipus de mostra amb comportament {comportament.Value} (no aplica comprovació 1)");
                }
                else
                {
                    _logger.Info($"   ⚠️ Tipus de mostra no trobat o sense comportament definit");
                }
            }
			
			
            // Comprovació 2: Tipus de mostra a incorporar si el pacient té positius vigents per aquest tipus de mostra
            // ------------------------------------
            
            if (!calIncorporarNegatiu)
            {
                
                // Comprovar si el pacient té positius vigents per aquest tipus de mostra o equivalents
                bool pacientTePositiusVigents = _multiRRepository.PacientTePositiusVigentsTipusMostraIEquivalents(
                    mostra.PacientSap, 
                    resultatMostra.MostraDescripcio);
                
                if (pacientTePositiusVigents)
                {
                    calIncorporarNegatiu = true;
                    tipusComprovacio = TipusComprovacioNegatiu.Comprovacio2;
                }

            }
			
			
            // Resultat de les comprovacions sobre si cal o no incorporar el negatiu
            // ------------------------------------
            
            if (!calIncorporarNegatiu)
            {
                _logger.Info($"   Resultat negatiu NO cal incorporar segons comprovacions 1 i 2");

                // Inserir auditoria amb codi NMRCM (No supera la comprovació de mostra)
                bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "NMRCM", null, resultatMostra);
                
                if (auditoriaCreada)
                {
                    resultat.AuditoriasCreades++;
                }
                
                resultat.ResultatsNoIncorporats++;
                return;
            }
			
			
            // FASE 2: RECUPERAR DIAGNÒSTICS POSITIUS A NEUTRALITZAR
            // ------------------------------------
            
            _logger.Info($"   ✔️ Resultat negatiu CAL incorporar (via {tipusComprovacio}), recuperant diagnòstics positius...");
            

            // Incrementar comptador segons tipus de comprovació
            if (tipusComprovacio == TipusComprovacioNegatiu.Comprovacio1)
            {
                resultat.IncorporatsPerComprovacio1++;
            }
            else if (tipusComprovacio == TipusComprovacioNegatiu.Comprovacio2)
            {
                resultat.IncorporatsPerComprovacio2++;
            }


            // Recuperar els diagnòstics positius a neutralitzar segons el tipus de comprovació
            List<int> diagnosticsPositiusANeutralitzar = new List<int>();

            if (tipusComprovacio == TipusComprovacioNegatiu.Comprovacio1)
            {
                // Comprovació 1: Recuperar tots els diagnòstics positius del pacient (qualsevol tipus mostra)
                diagnosticsPositiusANeutralitzar = _multiRRepository.ObtenirDiagnosticsPositiusPacientAlgunTipusMostra(mostra.PacientSap);
            }
            else if (tipusComprovacio == TipusComprovacioNegatiu.Comprovacio2)
            {
                // Comprovació 2: Recuperar diagnòstics positius vigents per aquest tipus de mostra o equivalents
                diagnosticsPositiusANeutralitzar = _multiRRepository.ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents(
                    mostra.PacientSap, 
                    resultatMostra.MostraDescripcio);
            }


            if (diagnosticsPositiusANeutralitzar == null || diagnosticsPositiusANeutralitzar.Count == 0)
            {
                _logger.Info($"   No s'han trobat diagnòstics positius a neutralitzar");
            }
            else
            {
                _logger.Info($"   Trobats {diagnosticsPositiusANeutralitzar.Count} diagnòstics positius a neutralitzar");

                // S´han trobat diagnòstics positius a neutralitzar, procedim a crear la mostra diagnòstic per al negatiu
                // Procedim a crear la mostra diagnòstic per al negatiu (serà la mateixa per a tots els diagnòstics positius)


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
                        "", // sense mecanisme
                        resultatMostra.EsMicroorganismeEspecial);

                    if (nouMostraDiagnosticId > 0)
                    {
                        mostraDiagnosticIdFinal = nouMostraDiagnosticId;
                        resultat.MostresDiagnosticCreades++;
                    }
                }


                // FASE 3: PER CADA DIAGNÒSTIC POSITIU INCORPORAR EL RESULTAT NEGATIU 
                // ------------------------------------

                foreach (var diagnosticId in diagnosticsPositiusANeutralitzar)
                {
                    var diagnosticInfo = _multiRRepository.ObtenirInformDiagnostic(diagnosticId);
                    if (diagnosticInfo != null)
                    {

                        _logger.Info($"  🔄 Incorporant el resultat negatiu al diagnòstic {diagnosticId}: {diagnosticInfo.MicroorganismeCodi} + {diagnosticInfo.MecanismeId ?? "(sense mecanisme)"}");


                        // Mostra_Microorganisme
                        // ------------------------------------

                        // Comprovar si ja existeix el registre mostra_microorganisme
                        bool existeixMostraMicroorganisme = _multiRRepository.ComprovarMostraMicroorganismeExisteix(
                            diagnosticId,
                            mostraDiagnosticIdFinal);

                        if (!existeixMostraMicroorganisme)
                        {
                            // Si no existeix, crear-lo
                            bool mostraMicroorganismeCreat = _multiRRepository.CrearMostraMicroorganisme(
                                diagnosticId,
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
                            diagnosticInfo.MicroorganismeCodi,
                            diagnosticInfo.MecanismeId,
                            diagnosticInfo.MecanismeDescrip);


                        // Actualitzar la data_diagnostic (de pacients_diagnostics_mostra) amb la data de mostra més antiga
                        // ------------------------------------

                        bool dataMostraActualitzada = _multiRRepository.ActualitzarDataDiagnosticPacientsDiagnosticsMostra(
                            mostra.PacientSap,
                            diagnosticInfo.MicroorganismeCodi,
                            diagnosticInfo.MecanismeId,
                            diagnosticInfo.MecanismeDescrip);

                    }


                }

                // Final OK (de negatiu)
                // ------------------------------------

                // Si arribem aquí indica que s´ha fet tota la gestió. Deixem registre auditoria (OK Negatiu)
                bool auditoriaCreadaOk = _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "OKN", null, resultatMostra);

                if (auditoriaCreadaOk)
                {
                    resultat.AuditoriasCreades++;
                }

                // Incrementar comptador de resultats processats
                resultat.ResultatsProcessats++;

            }

        }
    }
}
