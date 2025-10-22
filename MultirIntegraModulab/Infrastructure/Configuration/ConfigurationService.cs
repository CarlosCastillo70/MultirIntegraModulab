using System;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Configuration;

namespace MultirIntegraModulab.Infrastructure.Configuration
{
    /// <summary>
    /// Implementació del servei de configuració
    /// Adapta AppConfiguration a la interfície del domini
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly AppConfiguration _appConfig;

        public ConfigurationService()
        {
            _appConfig = AppConfiguration.Instance;
        }

        public string OracleConnectionString
        {
            get { return _appConfig.OracleConnectionString; }
        }

        public string MySqlConnectionString
        {
            get { return _appConfig.MySqlConnectionString; }
        }

        public int DiesEndarreraCarrega
        {
            get { return _appConfig.DiesEndarreraCarrega; }
        }

        public int LimitResultatsProves
        {
            get { return _appConfig.LimitResultatsProves; }
        }

        public bool EntornProduccion
        {
            get { return _appConfig.EntornProduccion; }
        }

        public string LogDirectory
        {
            get { return _appConfig.LogDirectory; }
        }

        public string LogLevel
        {
            get { return _appConfig.LogLevel; }
        }

        public int MinutsVigenciaCache
        {
            get { return _appConfig.MinutsVigenciaCache; }
        }

        public int DiesRetencioHistorial
        {
            get { return _appConfig.DiesRetencioHistorial; }
        }

        public bool ProcessarMostresEnParalel
        {
            get { return _appConfig.ProcessarMostresEnParalel; }
        }

        public int MaxGrauParalelisme
        {
            get { return _appConfig.MaxGrauParalelisme; }
        }

        public void ValidarConfiguracio()
        {
            _appConfig.ValidarConfiguracio();
        }

        public string ObtenirResumConfiguracio()
        {
            return _appConfig.ObtenirResumConfiguracio();
        }
    }
}
