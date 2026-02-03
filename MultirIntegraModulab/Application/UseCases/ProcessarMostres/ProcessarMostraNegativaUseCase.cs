using MultirIntegraModulab.Application.Helpers;
using MultirIntegraModulab.Application.UseCases.ClassificarMostres;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Enums;
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
    /// Tipus de comprobació que ha determinat que cal incorporar un negatiu
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
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Mostra negativa {mostra.EtiquetaId} processada correctament: " +
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

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Processant resultat negatiu: '{microorganisme}'");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");

            // FASE 1: COMPROVACIONS PER DETERMINAR SI CAL INCORPORAR EL NEGATIU
            // ------------------------------------

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔍 Comprovant si cal incorporar el negatiu per tipus mostra: '{resultatMostra.MostraDescripcio}'");
            
            bool calIncorporarNegatiu = false;
            TipusComprovacioNegatiu tipusComprovacio = TipusComprovacioNegatiu.Cap;


            // Comprovació 0: Comprovar si tenim el pacient a la taula de pacients
            // ------------------------------------
            
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Aplicant Comprovació 0: Verificant existència del pacient {mostra.PacientSap}");
            
            bool pacientExisteix = _multiRRepository.ExisteixPacient(mostra.PacientSap);
            
            if (!pacientExisteix)
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Pacient {mostra.PacientSap} NO existeix a la taula de pacients");
                
                // Inserir auditoria amb codi NMRCMP (No supera la comprovació de mostra amb motiu pacient)
                bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "NMRCMP", resultatMostra);
                
                if (auditoriaCreada)
                {
                    resultat.AuditoriasCreades++;
                }
                
                resultat.ResultatsNoIncorporats++;
                return;
            }
            
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Pacient {mostra.PacientSap} SI existeix a la taula de pacients");


            // Comprovació 1: Tipus de mostra a incorporar sempre que el pacient tingui algun positiu
            // ------------------------------------

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Aplicant Comprovació 1: Positius vigents per qualsevol tipus de mostra");

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
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Tipus de mostra amb comportament {comportament.Value}. Per tant NO aplica comprovació 1 i es continúa amb comprovació 2");
                }
                else
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Tipus de mostra no trobat o sense comportament definit");
                }
            }
			
			
            // Comprovació 2: Tipus de mostra a incorporar si el pacient té positius vigents per aquest tipus de mostra
            // ------------------------------------
            
            if (!calIncorporarNegatiu)
            {

                // Comprovar si el pacient té positius vigents per aquest tipus de mostra o equivalents, amb diferent etiquetaid
                bool pacientTePositiusVigents = _multiRRepository.PacientTePositiusVigentsTipusMostraIEquivalents(
                    mostra.PacientSap,
                    resultatMostra.MostraDescripcio, 
                    mostra.EtiquetaId);

                // NotaCC : segons Marti, pot ser que per una mateixa data puguin haver diferents resultats per una mateixa data
                // Comprovar si el pacient té positius vigents per aquest tipus de mostra o equivalents, amb diferent etiquetaid i diferent datapeticio
                //bool pacientTePositiusVigents = _multiRRepository.PacientTePositiusVigentsTipusMostraIEquivalents(
                //    mostra.PacientSap, 
                //    resultatMostra.MostraDescripcio, 
                //    mostra.EtiquetaId, 
                //    mostra.DataPeticio);

                //if (pacientTePositiusVigentsSenseFiltreData != pacientTePositiusVigents)
                //{
                //    var aaa = "TODOCC";
                //}

                
                if (pacientTePositiusVigents)
                {

                    // Comprovar si el pacient té algun negatiu, per al tipus de mostra, amb la mateixa etiqueta
                    // Mostres amb més d´un negatiu, si no es fa aquesta comprovació, afegirà tants negatius com negatius entrin
                    // Sol ha d´entrar el primer negatiu que contraresti el positiu.

                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Comprovant si ja existeix una mostra negativa, ja incorporada amb la mateixa etiqueta");

                    // Comprovar si ja existeix una mostra negativa (valoració '1') amb aquesta etiqueta específica
                    int mostraNegativaExistent = _multiRRepository.ComprovarMostraDiagnosticPerEtiqueta(
                        mostra.PacientSap,
                        resultatMostra.MostraDescripcio,
                        "1", // Valoració '1' = negatiu
                        mostra.EtiquetaId); // Etiqueta específica de la mostra actual

                    if (mostraNegativaExistent > 0)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ JA existeix un negatiu per aquesta mostra (ID: {mostraNegativaExistent})");
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}No cal donar d´alta més negatius de la mateixa etiqueta");

                        //// Inserir auditoria amb codi NMRCM (ja s'ha incorporat un negatiu per aquesta mostra)
                        //bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "NMRCM", resultatMostra);

                        //if (auditoriaCreada)
                        //{
                        //    resultat.AuditoriasCreades++;
                        //}

                        //resultat.ResultatsNoIncorporats++;
                        //return;
                    }
                    else
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ No existeix cap negatiu previ per aquesta mostra → Continuar amb la incorporació del negatiu");
                    }
    

                    calIncorporarNegatiu = true;
                    tipusComprovacio = TipusComprovacioNegatiu.Comprovacio2;
                }

            }
			
			
            // Resultat de les comprovacions sobre si cal o no incorporar el negatiu
            // ------------------------------------
            
            if (!calIncorporarNegatiu)
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Resultat negatiu NO cal incorporar segons comprovacions 0, 1 i 2");

                // Inserir auditoria amb codi NMRCM (No supera la comprovació de mostra)
                bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "NMRCM", resultatMostra);
                
                if (auditoriaCreada)
                {
                    resultat.AuditoriasCreades++;
                }
                
                resultat.ResultatsNoIncorporats++;
                return;
            }
			
			
            // FASE 2: RECUPERAR DIAGNÒSTICS POSITIUS A NEUTRALITZAR
            // ------------------------------------
            
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Resultat negatiu CAL incorporar (via {tipusComprovacio}), recuperant diagnòstics positius...");
            

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
                diagnosticsPositiusANeutralitzar = _multiRRepository.ObtenirDiagnosticsPositiusPacientAlgunTipusMostra(mostra.PacientSap, mostra.EtiquetaId);
            }
            else if (tipusComprovacio == TipusComprovacioNegatiu.Comprovacio2)
            {
                // Comprovació 2: Recuperar diagnòstics positius vigents per aquest tipus de mostra o equivalents (amb diferent etiqueta que la que s´esta processant)
                diagnosticsPositiusANeutralitzar = _multiRRepository.ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents(
                    mostra.PacientSap, 
                    resultatMostra.MostraDescripcio, 
                    mostra.EtiquetaId);
            }


            if (diagnosticsPositiusANeutralitzar == null || diagnosticsPositiusANeutralitzar.Count == 0)
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}No s'han trobat diagnòstics positius a neutralitzar");
            }
            else
            {
                
                // Mostrar els IDs dels diagnòstics positius trobats, que s'han de neutralitzar
                string idsDiagnostics = string.Join(", ", diagnosticsPositiusANeutralitzar);
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}IDs dels diagnòstics positius a neutralitzar: {idsDiagnostics}");

                
                // S´han trobat diagnòstics positius a neutralitzar, procedim a crear la mostra diagnòstic per al negatiu que neutralitzarà els positius
                // Procedim a crear la mostra diagnòstic per al negatiu que neutralitzarà els positius (serà la mateixa per a tots els diagnòstics positius)

                // Pacients_diagnostics_mostres
                // ------------------------------------

                // Comprovar si ja existeix la mostra diagnòstic negativa que neutralitzarà els positius
                int mostraDiagnosticId = _multiRRepository.ComprovarMostraDiagnosticExisteix(
                    mostra.PacientSap,
                    resultatMostra.DataPeticioTrunc,
                    resultatMostra.MostraDescripcio,
                    "1");

                int mostraDiagnosticIdFinal = mostraDiagnosticId;

                if (mostraDiagnosticId == 0)
                {
                    // Obtenir el microorganisme del resultat per incloure'l al camp microorganismeMecanismeCaptat
                    var microorganismeEntitat = _multiRRepository.ObtenirMicroorganisme(resultatMostra.AillamentDescripcio);
                    string microorganismeCodi = microorganismeEntitat?.Codi ?? resultatMostra.AillamentDescripcio ?? "";
                    

                    // Crear mostra negativa (amb microorganisme) que neutralitza una positiva
                    int nouMostraDiagnosticId = _multiRRepository.CrearMostraDiagnostic(
                        mostra.PacientSap,
                        resultatMostra.DataPeticioTrunc,
                        resultatMostra.MostraDescripcio,
                        resultatMostra.ProvaDescripcio,
                        mostra.EtiquetaId,
                        mostra.DataUltimResultat, // agafar data resultat de la mostra (no del resultat, ja que per una mostra poden haver diferents valors)
                        resultatMostra.DataValidacio,
                        "", // sense mecanisme
                        resultatMostra.EsMicroorganismeEspecial,
                        microorganismeCodi + "-"); // Per a negatius, només tenim el microorganisme (sense mecanisme)

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

                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}🔄 Incorporant el resultat negatiu al diagnòstic {diagnosticId}: {diagnosticInfo.MicroorganismeCodi} + {diagnosticInfo.MecanismeId ?? "(sense mecanisme)"}");


                        // Mostra_Microorganisme
                        // ------------------------------------

                        // Comprovar si ja existeix el registre mostra_microorganisme
                        bool existeixMostraMicroorganisme = _multiRRepository.ComprovarMostraMicroorganismeExisteix(
                            diagnosticId,
                            mostraDiagnosticIdFinal);

                        if (existeixMostraMicroorganisme)
                        {
                            // Si existeix, és un duplicat. Ho deixem auditat i no fem res més per aquest mecanisme
                            bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
                                mostra,
                                "DMM",
                                resultatMostra);

                            if (auditoriaCreada)
                            {
                                resultat.AuditoriasCreades++;
                            }

                            resultat.RelacionsDuplicades++;

                            // Continuar amb el següent diagnostic a neutralitzar
                            //continue;
                        }
                        else
                        {
                            // Si no existeix, crear-lo
                            bool mostraMicroorganismeCreat = _multiRRepository.CrearMostraMicroorganisme(
                                diagnosticId,
                                mostraDiagnosticIdFinal);

                            if (mostraMicroorganismeCreat)
                            {
                                resultat.RelacionsCreades++;

                                // Ho deixem auditat i continuem endavant
                                bool auditoriaCreadaOk = _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "OKN", resultatMostra);

                                if (auditoriaCreadaOk)
                                {
                                    resultat.AuditoriasCreades++;
                                }

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


                // Incrementar comptador de resultats processats
                resultat.ResultatsProcessats++;

            }

        }
    }
}
