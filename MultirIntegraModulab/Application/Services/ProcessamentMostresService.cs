using System;
using System.Threading.Tasks;
using MultirIntegraModulab.Application.DTOs;
using MultirIntegraModulab.Application.Interfaces;
using MultirIntegraModulab.Application.UseCases.ProcessarMostres;
using MultirIntegraModulab.Application.UseCases.ClassificarMostres;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;

namespace MultirIntegraModulab.Application.Services
{
    /// <summary>
    /// Servei principal d'aplicació per processar mostres
    /// Coordina els diferents Use Cases seguint Clean Architecture
    /// </summary>
    public class ProcessamentMostresService : IProcessamentMostresService
    {
        private readonly IModulabRepository _modulabRepository;
        private readonly IMultiRRepository _multiRRepository;
        private readonly IPacientWebService _pacientWebService;
        private readonly ILoggerService _logger;
        private readonly IConfigurationService _configurationService;
        private readonly ProcessarMostresUseCase _processarMostresUseCase;
        private readonly ValidarMostraUseCase _validarMostraUseCase;
        private readonly ClassificarMostraUseCase _classificarMostraUseCase;

        public ProcessamentMostresService(
            IModulabRepository modulabRepository,
            IMultiRRepository multiRRepository,
            IPacientWebService pacientWebService,
            ILoggerService logger,
            IConfigurationService configurationService)
        {
            _modulabRepository = modulabRepository ?? throw new ArgumentNullException(nameof(modulabRepository));
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _pacientWebService = pacientWebService; // Pot ser null
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            // Inicialitzar Use Cases
            _validarMostraUseCase = new ValidarMostraUseCase(_logger);
            _classificarMostraUseCase = new ClassificarMostraUseCase(_multiRRepository, _logger);
            _processarMostresUseCase = new ProcessarMostresUseCase(
                _modulabRepository, 
                _multiRRepository,
                _pacientWebService,
                _logger,
                _configurationService,
                _validarMostraUseCase);
        }

        /// <summary>
        /// Processa una col·lecció de mostres
        /// </summary>
        public async Task<ResumProcessamentDto> ProcessarMostresAsync(ColeccioMostres mostres)
        {
            try
            {
                return await _processarMostresUseCase.ExecutarAsync(mostres);
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Error processant mostres: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Processa una mostra individual
        /// </summary>
        public async Task<bool> ProcessarMostraAsync(Mostra mostra)
        {
            try
            {
                if (!ValidarMostra(mostra))
                {
                    _logger.Warning($"Mostra {mostra?.EtiquetaId} no vàlida");
                    return false;
                }

                // Classificar la mostra
                var classificacio = _classificarMostraUseCase.Executar(mostra);
                _logger.Info($"Mostra {mostra.EtiquetaId} classificada com: {classificacio.TipusMostra.ToString().ToUpper()}");

                // Processar utilitzant el Use Case principal
                var colleccio = new ColeccioMostres();
                colleccio.AfegirResultat(mostra.Resultats[0]); // Afegir el primer resultat per crear la mostra

                var resum = await _processarMostresUseCase.ExecutarAsync(colleccio);

                return resum.TotalProcessats > 0;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processant mostra {mostra?.EtiquetaId}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Valida una mostra abans de processar-la
        /// </summary>
        public bool ValidarMostra(Mostra mostra)
        {
            return _validarMostraUseCase.Executar(mostra);
        }
    }
}
