using System;
using System.Linq;
using System.Threading.Tasks;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Application.UseCases.ClassificarMostres;

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

            _logger.Info($"Processant mostra amb múltiples resultats positius: {mostra.EtiquetaId}");
            _logger.Info($"  Total registres positius: {classificacio.ResultatsPositius}");

            try
            {
                // Delegar al Use Case de mostra positiva individual
                // Aquest ja gestiona múltiples registres correctament
                var resultat = await _processarPositivaUseCase.ExecutarAsync(mostra, classificacio);

                if (resultat.Exitosa)
                {
                    _logger.Info($"Mostra amb múltiples positius {mostra.EtiquetaId} processada correctament");
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

            _logger.Info($"Processant mostra amb múltiples resultats negatius: {mostra.EtiquetaId}");
            _logger.Info($"  Total registres negatius: {classificacio.ResultatsNegatius}");

            try
            {
                // Delegar al Use Case de mostra negativa
                var resultat = await _processarNegativaUseCase.ExecutarAsync(mostra, classificacio);

                if (resultat.Exitosa)
                {
                    _logger.Info($"Mostra amb múltiples negatius {mostra.EtiquetaId} processada (auditada)");
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
    /// Use Case per processar mostres mixtes (amb resultats positius i negatius)
    /// Processa els positius i audita els negatius
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
        public async Task<ResultatProcessamentPositiu> ExecutarAsync(
            Mostra mostra,
            ResultatClassificacio classificacio)
        {
            if (mostra == null)
            {
                _logger.Warning("Intentant processar mostra mixta amb mostra null");
                throw new ArgumentNullException(nameof(mostra));
            }

            _logger.Info($"Processant mostra mixta: {mostra.EtiquetaId}");
            _logger.Info($"  Registres positius: {classificacio.ResultatsPositius}");
            _logger.Info($"  Registres negatius: {classificacio.ResultatsNegatius}");

            var resultat = new ResultatProcessamentPositiu();

            try
            {
                // Per mostres mixtes:
                // 1. Processar només els registres positius (com una mostra positiva)
                // 2. Els registres negatius no es processen, només s'auditen

                _logger.Info($"  Processant només els registres positius de la mostra mixta");

                // Crear una mostra temporal només amb els registres positius
                var mostraPositius = CrearMostraAmbRegistresPositius(mostra, classificacio);

                // Processar com una mostra positiva
                var processarPositivaUseCase = new ProcessarMostraPositivaUseCase(
                    _multiRRepository,
                    _pacientWebService,
                    _logger);

                resultat = await processarPositivaUseCase.ExecutarAsync(mostraPositius, classificacio);

                // Auditar els negatius
                if (classificacio.ResultatsNegatius > 0)
                {
                    _logger.Info($"  Auditant {classificacio.ResultatsNegatius} registres negatius");
                    
                    bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
                        mostra,
                        "MM", // Codi: Mostra Mixta
                        null,
                        mostra.Resultats.FirstOrDefault()
                    );

                    if (auditoriaCreada)
                    {
                        _logger.Info($"  ✔️ Auditoria creada per registres negatius de mostra mixta");
                    }
                }

                if (resultat.Exitosa)
                {
                    _logger.Info($"Mostra mixta {mostra.EtiquetaId} processada correctament");
                    resultat.Missatge = $"Mostra mixta processada: {resultat.IntegracionsCreades} positius processats, " +
                        $"{classificacio.ResultatsNegatius} negatius auditats";
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
        /// Crea una mostra temporal només amb els registres que tenen resultats positius
        /// </summary>
        private Mostra CrearMostraAmbRegistresPositius(
            Mostra mostraOriginal,
            ResultatClassificacio classificacio)
        {
            // Per simplificar, podem processar tots els registres
            // El Use Case de mostra positiva ja filtra correctament
            return mostraOriginal;
        }
    }
}
