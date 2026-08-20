using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;

namespace MultirIntegraModulab.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implementació del repositori per accedir a dades de Modulab (Oracle)
    /// Aquesta classe adapta ModulabDbService a la interfície del domini
    /// </summary>
    public class ModulabRepository : IModulabRepository
    {
        private readonly ModulabDbService _modulabDbService;
        private readonly MultiRDbService _multiRDbService;
        private readonly ILoggerService _logger;

        public ModulabRepository(ModulabDbService modulabDbService, MultiRDbService multiRDbService, ILoggerService logger)
        {
            _modulabDbService = modulabDbService ?? throw new ArgumentNullException(nameof(modulabDbService));
            _multiRDbService = multiRDbService ?? throw new ArgumentNullException(nameof(multiRDbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Carrega les mostres des de Modulab
        /// </summary>
        public async Task<ColeccioMostres> CarregarMostresAsync(int diesEndarrera, int limitRegistres = 0)
        {
            try
            {
                _logger.Info($"Carregant mostres de Modulab: {diesEndarrera} dies enrere i límit: {limitRegistres} (0 = sense límit)");
                
                // El mètode és síncron a ModulabDbService, però el wrappem per consistència
                var resultat = await Task.Run(() => 
                    _modulabDbService.CarregarResultatsDeMostres(diesEndarrera, _multiRDbService, limitRegistres));
                
                _logger.Info($"Mostres a carregar de Modulab {resultat.NombreTotalMostres}");
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error carregant mostres de Modulab: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Carrega les mostres per un pacient específic
        /// </summary>
        public async Task<ColeccioMostres> CarregarMostresPerPacientAsync(string pacientSap, int diesEndarrera)
        {
            try
            {
                _logger.Info($"Carregant mostres de Modulab per pacient {pacientSap}");
                
                var resultat = await Task.Run(() => 
                    _modulabDbService.CarregarResultatsDeMostresPerPacient(pacientSap, diesEndarrera, 0, _multiRDbService));
                
                _logger.Info($"Carregades {resultat.NombreTotalMostres} mostres per pacient {pacientSap}");
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error carregant mostres per pacient {pacientSap}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Carrega les mostres per un rang de dates
        /// </summary>
        public async Task<ColeccioMostres> CarregarMostresPerRangDatesAsync(DateTime dataInici, DateTime dataFi)
        {
            try
            {
                _logger.Info($"Carregant mostres de Modulab entre {dataInici:dd/MM/yyyy} i {dataFi:dd/MM/yyyy}");
                
                var resultat = await Task.Run(() => 
                    _modulabDbService.CarregarResultatsDeMostresPerRangDates(dataInici, dataFi, _multiRDbService));
                
                _logger.Info($"Carregades {resultat.NombreTotalMostres} mostres per rang de dates");
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error carregant mostres per rang de dates: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Carrega resultats de mostres dels últims X dies enrere
        /// </summary>
        public ColeccioMostres CarregarResultatsDiesEndarrera(int diesEndarrera, int limit = 0)
        {
            try
            {
                _logger.Info($"Carregant mostres de Modulab: {diesEndarrera} dies enrere, límit: {limit}");
                
                var resultat = _modulabDbService.CarregarResultatsDeMostres(diesEndarrera, _multiRDbService, limit);
                
                _logger.Info($"Carregades {resultat.NombreTotalMostres} mostres de Modulab");
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error carregant mostres de Modulab: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Carrega resultats per un pacient específic
        /// </summary>
        public ColeccioMostres CarregarResultatsPerPacient(string pacientSap, int diesEndarrera, int limit = 0)
        {
            try
            {
                _logger.Info($"Carregant mostres de Modulab per pacient {pacientSap}");
                
                var resultat = _modulabDbService.CarregarResultatsDeMostresPerPacient(pacientSap, diesEndarrera, limit, _multiRDbService);
                
                _logger.Info($"Carregades {resultat.NombreTotalMostres} mostres per pacient {pacientSap}");
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error carregant mostres per pacient {pacientSap}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Carrega resultats per rang de dates
        /// </summary>
        public ColeccioMostres CarregarResultatsPerRangDates(DateTime dataInici, DateTime dataFi, int limit = 0)
        {
            try
            {
                _logger.Info($"Carregant mostres de Modulab entre {dataInici:dd/MM/yyyy} i {dataFi:dd/MM/yyyy}");
                
                var resultat = _modulabDbService.CarregarResultatsDeMostresPerRangDates(dataInici, dataFi, _multiRDbService);
                
                _logger.Info($"Carregades {resultat.NombreTotalMostres} mostres per rang de dates");
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error carregant mostres per rang de dates: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Obté la data actual del sistema Oracle
        /// </summary>
        public DateTime GetCurrentDate()
        {
            return (DateTime)_modulabDbService.GetCurrentDate();
        }

        /// <summary>
        /// Obté el tipus de base de dades
        /// </summary>
        public string GetDatabaseType()
        {
            return _modulabDbService.GetDatabaseType();
        }

        /// <summary>
        /// Carrega resultats de forma incremental utilitzant filtres de darreres dates de resultat i validació processades
        /// </summary>
        public ColeccioMostres CarregarResultatsIncremental(DadesSincronitzacio dadesSincronitzacio, int limit = 0)
        {
            try
            {
                _logger.Info("🔄 Carregant mostres amb càrrega incremental (segons filtres de darreres dates de resultat i validació processades)");

                var resultat = _modulabDbService.CarregarResultatsAmbSincronitzacio(
                    dadesSincronitzacio, 
                    _multiRDbService, 
                    limit);

                _logger.Info($"✅ Carregades {resultat.NombreTotalMostres} mostres de forma incremental");
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Error carregant mostres de forma incremental: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Carrega mostres segons l'últim canvi (RESULTLASTCHANGE) de forma síncrona
        /// </summary>
        public ColeccioMostres CarregarResultatsPerUltimCanvi(DateTime? dataUltimCanviReference, int diasEnRe, int limit = 0)
        {
            try
            {
                _logger.Info($"🔄 Carregant mostres per últim canvi (dies enrere: {diasEnRe})");

                var resultat = _modulabDbService.CarregarResultatsDeMostresPerUltimCanvi(
                    dataUltimCanviReference,
                    diasEnRe,
                    _multiRDbService,
                    limit);

                _logger.Info($"✅ Carregades {resultat.NombreTotalMostres} mostres per últim canvi");
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Error carregant mostres per últim canvi: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Carrega mostres segons l'últim canvi (RESULTLASTCHANGE) de forma asíncrona
        /// </summary>
        public async Task<ColeccioMostres> CarregarResultatsPerUltimCanviAsync(DateTime? dataUltimCanviReference, int diasEnRe, int limit = 0)
        {
            try
            {
                _logger.Info($"🔄 Carregant mostres per últim canvi de forma asíncrona (dies enrere: {diasEnRe})");

                var resultat = await Task.Run(() => 
                    _modulabDbService.CarregarResultatsDeMostresPerUltimCanvi(
                        dataUltimCanviReference,
                        diasEnRe,
                        _multiRDbService,
                        limit));

                _logger.Info($"✅ Carregades {resultat.NombreTotalMostres} mostres per últim canvi (async)");
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Error carregant mostres per últim canvi (async): {ex.Message}", ex);
                throw;
            }
        }
    }
}
