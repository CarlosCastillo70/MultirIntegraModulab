using System;
using System.Linq;
using System.Threading.Tasks;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Application.UseCases.ClassificarMostres;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab.Application.UseCases.ProcessarMostres
{
    /// <summary>
    /// Use Case per processar mostres amb múltiples resultats tots positius
    /// Delega el processament a ProcessarMostraPositivaUseCase per cada registre
    /// </summary>
    public class ProcessarMostresPositivesUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly IPacientWebService _pacientWebService;
        private readonly ILoggerService _logger;
        private readonly ProcessarMostraPositivaUseCase _processarPositivaUseCase;

        public ProcessarMostresPositivesUseCase(
            IMultiRRepository multiRRepository,
            IPacientWebService pacientWebService,
            ILoggerService logger)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _pacientWebService = pacientWebService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Reutilitzar el Use Case de mostra positiva individual
            _processarPositivaUseCase = new ProcessarMostraPositivaUseCase(
                _multiRRepository,
                _pacientWebService,
                _logger);
        }

        /// <summary>
        /// Executa el processament de mostres amb múltiples resultats positius
        /// </summary>
        public async Task<ResultatProcessamentPositiu> ExecutarAsync(
            Mostra mostra,
            ResultatClassificacio classificacio)
        {
            if (mostra == null)
            {
                _logger.Warning("Intentant processar mostres positives amb mostra null");
                throw new ArgumentNullException(nameof(mostra));
            }

            _logger.Info($"🔄 Processant mostra amb múltiples resultats positius: {mostra.EtiquetaId}");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Total registres positius: {classificacio.ResultatsPositius}");

            try
            {
                // Delegar al Use Case de mostra positiva individual
                // Aquest ja gestiona múltiples registres correctament
                var resultat = await _processarPositivaUseCase.ExecutarAsync(mostra, classificacio);

                if (resultat.Exitosa)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Mostra amb múltiples positius {mostra.EtiquetaId} processada correctament");
                }

                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processant mostra amb múltiples positius {mostra.EtiquetaId}", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Use Case per processar mostres amb múltiples resultats tots negatius
    /// Delega el processament a ProcessarMostraNegativaUseCase
    /// </summary>
    public class ProcessarMostresNegativesUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly ILoggerService _logger;
        private readonly ProcessarMostraNegativaUseCase _processarNegativaUseCase;

        public ProcessarMostresNegativesUseCase(
            IMultiRRepository multiRRepository,
            ILoggerService logger)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Reutilitzar el Use Case de mostra negativa individual
            _processarNegativaUseCase = new ProcessarMostraNegativaUseCase(
                _multiRRepository,
                _logger);
        }

        /// <summary>
        /// Executa el processament de mostres amb múltiples resultats negatius
        /// </summary>
        public async Task<ResultatProcessamentNegatiu> ExecutarAsync(
            Mostra mostra,
            ResultatClassificacio classificacio)
        {
            if (mostra == null)
            {
                _logger.Warning("Intentant processar mostres negatives amb mostra null");
                throw new ArgumentNullException(nameof(mostra));
            }

            _logger.Info($"🔄 Processant mostra amb múltiples ({classificacio.ResultatsNegatius}) resultats negatius: {mostra.EtiquetaId}");

            try
            {
                // Delegar al Use Case de mostra negativa
                var resultat = await _processarNegativaUseCase.ExecutarAsync(mostra, classificacio);

                if (resultat.Exitosa)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Mostra amb múltiples negatius {mostra.EtiquetaId} processada");
                }

                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processant mostra amb múltiples negatius {mostra.EtiquetaId}", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Resultat del processament d'una mostra mixta
    /// </summary>
    public class ResultatProcessamentMixte
    {
        public bool Exitosa { get; set; }
        public string Missatge { get; set; }
        
        /// <summary>
        /// Indica si s'ha afegit almenys un positiu nou durant aquest processament
        /// </summary>
        public bool PositiuAfegit { get; set; }
        
        // Comptadors positius
        public bool PacientCreat { get; set; }
        public int DiagnosticsCreats { get; set; }
        public int DiagnosticsExistents { get; set; }
        public int MostresDiagnosticCreades { get; set; }
        public int MostresDiagnosticExistents { get; set; }
        public int RelacionsCreades { get; set; }
        public int RelacionsDuplicades { get; set; }
        public int MecanismesProcessats { get; set; }
        public int IntegracionsCreades { get; set; }
        public int MostresNegativesCreades { get; set; }
        
        // Comptadors negatius
        public int MostresDiagnosticCreadesNegatives { get; set; }
        public int MostresDiagnosticExistentsNegatives { get; set; }
        public int RelacionsCreadesNegatives { get; set; }
        public int RelacionsDuplicadesNegatives { get; set; }
        public int ResultatsProcessatsNegatius { get; set; }
        public int ResultatsNoIncorporats { get; set; }
        public int IncorporatsPerComprovacio1 { get; set; }
        public int IncorporatsPerComprovacio2 { get; set; }
        
        // Totals auditories
        public int AuditoriasCreades { get; set; }

        public ResultatProcessamentMixte()
        {
            Exitosa = true;
            PositiuAfegit = false;
        }
    }

    /// <summary>
    /// Use Case per processar mostres mixtes (amb resultats positius i negatius)
    /// Processa els positius i després els negatius
    /// </summary>
    public class ProcessarMostraMixtaUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly IPacientWebService _pacientWebService;
        private readonly ILoggerService _logger;

        public ProcessarMostraMixtaUseCase(
            IMultiRRepository multiRRepository,
            IPacientWebService pacientWebService,
            ILoggerService logger)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _pacientWebService = pacientWebService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executa el processament d'una mostra mixta
        /// </summary>
        public async Task<ResultatProcessamentMixte> ExecutarAsync(
            Mostra mostra,
            ResultatClassificacio classificacio)
        {
            if (mostra == null)
            {
                _logger.Warning("Intentant processar mostra mixta amb mostra null");
                throw new ArgumentNullException(nameof(mostra));
            }

            _logger.Info($"🔄 Processant mostra mixta amb {classificacio.ResultatsPositius} resultat(s) positiu(s) i {classificacio.ResultatsNegatius} resultat(s) negatiu(s)");

            var resultat = new ResultatProcessamentMixte();

            try
            {
                // FASE 1: PROCESSAR RESULTATS POSITIUS
                // ------------------------------------
                
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}📋 Processant {classificacio.ResultatsPositius} resultat(s) positiu(s)");

                // Crear una mostra temporal només amb els resultats positius
                var mostraPositius = CrearMostraAmbResultatsPositius(mostra, classificacio);

                // Processar com una mostra positiva
                var processarPositivaUseCase = new ProcessarMostraPositivaUseCase(
                    _multiRRepository,
                    _pacientWebService,
                    _logger);

                var resultatPositius = await processarPositivaUseCase.ExecutarAsync(mostraPositius, classificacio);

                if (!resultatPositius.Exitosa)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ Error processant positius de mostra mixta");
                    resultat.Exitosa = false;
                    resultat.Missatge = resultatPositius.Missatge;
                    return resultat;
                }

                // Copiar resultats dels positius
                resultat.PacientCreat = resultatPositius.PacientCreat;
                resultat.DiagnosticsCreats = resultatPositius.DiagnosticsCreats;
                resultat.DiagnosticsExistents = resultatPositius.DiagnosticsExistents;
                resultat.MostresDiagnosticCreades = resultatPositius.MostresDiagnosticCreades;
                resultat.MostresDiagnosticExistents = resultatPositius.MostresDiagnosticExistents;
                resultat.RelacionsCreades = resultatPositius.RelacionsCreades;
                resultat.RelacionsDuplicades = resultatPositius.RelacionsDuplicades;
                resultat.MecanismesProcessats = resultatPositius.MecanismesProcessats;
                resultat.IntegracionsCreades = resultatPositius.IntegracionsCreades;
                resultat.MostresNegativesCreades = resultatPositius.MostresNegativesCreades;
                resultat.AuditoriasCreades = resultatPositius.AuditoriasCreades;
                resultat.PositiuAfegit = resultatPositius.PositiuAfegit;  // Copiar també si s'ha afegit positiu

                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}✔️ Processats {resultatPositius.MecanismesProcessats} resultat(s) positiu(s)");


                // FASE 2: PROCESSAR RESULTATS NEGATIUS
                // ------------------------------------
                
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}📋 Processant {classificacio.ResultatsNegatius} resultat(s) negatiu(s)");

                // Crear una mostra temporal només amb els resultats negatius
                var mostraNegatius = CrearMostraAmbResultatsNegatius(mostra, classificacio);

                // Processar com una mostra negativa
                var processarNegativaUseCase = new ProcessarMostraNegativaUseCase(
                    _multiRRepository,
                    _logger);

                var resultatNegatius = await processarNegativaUseCase.ExecutarAsync(mostraNegatius, classificacio);

                if (!resultatNegatius.Exitosa)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ Error processant negatius de mostra mixta");
                    // No fallem el processament complet, ja que els positius s'han processat correctament
                }
                else
                {
                    // Copiar resultats dels negatius
                    resultat.MostresDiagnosticCreadesNegatives = resultatNegatius.MostresDiagnosticCreades;
                    resultat.MostresDiagnosticExistentsNegatives = resultatNegatius.MostresDiagnosticExistents;
                    resultat.RelacionsCreadesNegatives = resultatNegatius.RelacionsCreades;
                    resultat.RelacionsDuplicadesNegatives = resultatNegatius.RelacionsDuplicades;
                    resultat.ResultatsProcessatsNegatius = resultatNegatius.ResultatsProcessats;
                    resultat.ResultatsNoIncorporats = resultatNegatius.ResultatsNoIncorporats;
                    resultat.IncorporatsPerComprovacio1 = resultatNegatius.IncorporatsPerComprovacio1;
                    resultat.IncorporatsPerComprovacio2 = resultatNegatius.IncorporatsPerComprovacio2;
                    resultat.AuditoriasCreades += resultatNegatius.AuditoriasCreades;

                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}✔️ Processats {resultatNegatius.ResultatsProcessats} resultat(s) negatiu(s)");
                }


                // RESULTAT FINAL
                // ------------------------------------
                
                if (resultat.Exitosa)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Mostra mixta {mostra.EtiquetaId} processada correctament:");
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Resultats positius : {resultat.MecanismesProcessats} processats, " +
                        $"{resultat.DiagnosticsCreats} diagnòstics creats, {resultat.MostresDiagnosticCreades} mostres creades");
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Resultats negatius: {resultat.ResultatsProcessatsNegatius} processats, " +
                        $"{resultat.ResultatsNoIncorporats} no incorporats");
                    
                    resultat.Missatge = $"Mostra mixta processada: {resultat.MecanismesProcessats} positius, " +
                        $"{resultat.ResultatsProcessatsNegatius} negatius incorporats, {resultat.ResultatsNoIncorporats} negatius no incorporats";
                }

                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processant mostra mixta {mostra.EtiquetaId}", ex);
                resultat.Exitosa = false;
                resultat.Missatge = $"Error: {ex.Message}";
                return resultat;
            }
        }

        /// <summary>
        /// Crea una mostra temporal només amb els resultats que tenen mecanismes (positius)
        /// </summary>
        private Mostra CrearMostraAmbResultatsPositius(
            Mostra mostraOriginal,
            ResultatClassificacio classificacio)
        {
            var mostraPositius = new Mostra(mostraOriginal.EtiquetaId, mostraOriginal.PacientSap);
            
            foreach (var resultat in mostraOriginal.Resultats)
            {
                // Un resultat és positiu si té almenys un mecanisme o és microorganisme especial
                bool teAlgunMecanisme = !string.IsNullOrWhiteSpace(resultat.MecanismeResistencia1Id) ||
                                       !string.IsNullOrWhiteSpace(resultat.MecanismeResistencia2Id) ||
                                       !string.IsNullOrWhiteSpace(resultat.MecanismeResistencia3Id) ||
                                       !string.IsNullOrWhiteSpace(resultat.MecanismeResistencia4Id) ||
                                       !string.IsNullOrWhiteSpace(resultat.MecanismeResistencia5Id);
                
                bool esMicroorganismeEspecial = resultat.EsMicroorganismeEspecial.HasValue && 
                                               resultat.EsMicroorganismeEspecial.Value;
                
                if (teAlgunMecanisme || esMicroorganismeEspecial)
                {
                    mostraPositius.AfegirResultat(resultat);
                }
            }
            
            return mostraPositius;
        }

        /// <summary>
        /// Crea una mostra temporal només amb els resultats que NO tenen mecanismes (negatius)
        /// </summary>
        private Mostra CrearMostraAmbResultatsNegatius(
            Mostra mostraOriginal,
            ResultatClassificacio classificacio)
        {
            var mostraNegatius = new Mostra(mostraOriginal.EtiquetaId, mostraOriginal.PacientSap);
            
            foreach (var resultat in mostraOriginal.Resultats)
            {
                // Un resultat és negatiu si NO té cap mecanisme i NO és microorganisme especial
                bool teAlgunMecanisme = !string.IsNullOrWhiteSpace(resultat.MecanismeResistencia1Id) ||
                                       !string.IsNullOrWhiteSpace(resultat.MecanismeResistencia2Id) ||
                                       !string.IsNullOrWhiteSpace(resultat.MecanismeResistencia3Id) ||
                                       !string.IsNullOrWhiteSpace(resultat.MecanismeResistencia4Id) ||
                                       !string.IsNullOrWhiteSpace(resultat.MecanismeResistencia5Id);
                
                bool esMicroorganismeEspecial = resultat.EsMicroorganismeEspecial.HasValue && 
                                               resultat.EsMicroorganismeEspecial.Value;
                
                if (!teAlgunMecanisme && !esMicroorganismeEspecial)
                {
                    mostraNegatius.AfegirResultat(resultat);
                }
            }
            
            return mostraNegatius;
        }
    }
}
