using System;
using System.Configuration;

namespace MultirIntegraModulab.Configuration
{
    /// <summary>
    /// Gestiona la configuració de l'aplicació de forma centralitzada i tipada
    /// </summary>
    public class AppConfiguration
    {
        private static AppConfiguration _instance;
        private static readonly object _lock = new object();

        // Singleton pattern per evitar múltiples lectures del fitxer
        public static AppConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AppConfiguration();
                        }
                    }
                }
                return _instance;
            }
        }

        private AppConfiguration()
        {
            CarregarConfiguracio();
        }

        // CONNECTION STRINGS
        // ------------------
        public string OracleConnectionString { get; private set; }
        public string MySqlConnectionString { get; private set; }

        // CONFIGURACIÓ DE CÀRREGA
        // ------------------------
        public int DiesEndarreraCarrega { get; private set; }
        public int LimitResultatsProves { get; private set; }
        public bool EntornProduccion { get; private set; }

        // CONFIGURACIÓ DE LOGGING
        // ------------------------
        public string LogDirectory { get; private set; }
        public string LogLevel { get; private set; }

        // CONFIGURACIÓ DE CACHE
        // ---------------------
        public int MinutsVigenciaCache { get; private set; }

        // CONFIGURACIÓ DE MANTENIMENT
        // ---------------------------
        public int DiesRetencioHistorial { get; private set; }

        // CONFIGURACIÓ DE PROCESSAMENT
        // ----------------------------
        public bool ProcessarMostresEnParalel { get; private set; }
        public int MaxGrauParalelisme { get; private set; }

        /// <summary>
        /// Carrega tota la configuració des de App.config
        /// </summary>
        private void CarregarConfiguracio()
        {
            try
            {
                // Connection Strings
                OracleConnectionString = ObtenirConnectionString("OracleModulab");
                MySqlConnectionString = ObtenirConnectionString("MySqlMultiR");

                // AppSettings
                DiesEndarreraCarrega = ObtenirAppSettingInt("DiesEndarreraCarrega", 1);
                LimitResultatsProves = ObtenirAppSettingInt("LimitResultatsProves", 50);
                EntornProduccion = ObtenirAppSettingBool("EntornProduccion", false);

                LogDirectory = ObtenirAppSettingString("LogDirectory", "Logs");
                LogLevel = ObtenirAppSettingString("LogLevel", "Info");

                MinutsVigenciaCache = ObtenirAppSettingInt("MinutsVigenciaCache", 30);
                DiesRetencioHistorial = ObtenirAppSettingInt("DiesRetencioHistorial", 90);

                ProcessarMostresEnParalel = ObtenirAppSettingBool("ProcessarMostresEnParalel", false);
                MaxGrauParalelisme = ObtenirAppSettingInt("MaxGrauParalelisme", 4);
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(
                    "Error carregant la configuració de l'aplicació. " +
                    "Verifica que el fitxer App.config existeix i està ben format.", ex);
            }
        }

        /// <summary>
        /// Obté un connection string del fitxer de configuració
        /// </summary>
        private string ObtenirConnectionString(string name)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[name];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    $"La connection string '{name}' no està definida a App.config");
            }
            return connectionString.ConnectionString;
        }

        /// <summary>
        /// Obté un valor string de AppSettings
        /// </summary>
        private string ObtenirAppSettingString(string key, string defaultValue = null)
        {
            string value = ConfigurationManager.AppSettings[key];
            return !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
        }

        /// <summary>
        /// Obté un valor int de AppSettings
        /// </summary>
        private int ObtenirAppSettingInt(string key, int defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Obté un valor bool de AppSettings
        /// </summary>
        private bool ObtenirAppSettingBool(string key, bool defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Valida que la configuració és correcta
        /// </summary>
        public void ValidarConfiguracio()
        {
            if (string.IsNullOrWhiteSpace(OracleConnectionString))
                throw new ConfigurationErrorsException("OracleConnectionString no pot estar buida");

            if (string.IsNullOrWhiteSpace(MySqlConnectionString))
                throw new ConfigurationErrorsException("MySqlConnectionString no pot estar buida");

            if (DiesEndarreraCarrega < 0)
                throw new ConfigurationErrorsException("DiesEndarreraCarrega ha de ser >= 0");

            if (LimitResultatsProves < 0)
                throw new ConfigurationErrorsException("LimitResultatsProves ha de ser >= 0");

            if (MinutsVigenciaCache <= 0)
                throw new ConfigurationErrorsException("MinutsVigenciaCache ha de ser > 0");

            if (DiesRetencioHistorial <= 0)
                throw new ConfigurationErrorsException("DiesRetencioHistorial ha de ser > 0");

            if (MaxGrauParalelisme <= 0)
                throw new ConfigurationErrorsException("MaxGrauParalelisme ha de ser > 0");
        }

        /// <summary>
        /// Mostra un resum de la configuració actual
        /// </summary>
        public string ObtenirResumConfiguracio()
        {
            return $@"
    ================================================================================
    CONFIGURACIÓ DE L'APLICACIÓ
    ================================================================================

    BASES DE DADES:
    - Oracle: {MascaraConnectionString(OracleConnectionString)}
    - MySQL:  {MascaraConnectionString(MySqlConnectionString)}
                 
    CÀRREGA DE DADES:
    - Dies enrere: {DiesEndarreraCarrega}
    - Límit resultats: {(EntornProduccion ? "Il·limitat (Producció)" : LimitResultatsProves.ToString())}
    - Entorn: {(EntornProduccion ? "PRODUCCIÓ" : "PROVES")}
 
    LOGGING:
    - Directori: {LogDirectory}
    - Nivell: {LogLevel}

    CACHE:
    - Vigència: {MinutsVigenciaCache} minuts

    ================================================================================
                ";
        }

        /// <summary>
        /// Mascara una connection string per no mostrar credencials
        /// </summary>
        private string MascaraConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return "[No definida]";

            // Amagar passwords
            var masked = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Password|Pwd)\s*=\s*[^;]+",
                "$1=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Si és molt llarga, abreujar-la
            if (masked.Length > 80)
            {
                return masked.Substring(0, 77) + "...";
            }

            return masked;
        }

        /// <summary>
        /// Força la recàrrega de la configuració (útil per testing)
        /// </summary>
        public static void RecarregarConfiguracio()
        {
            lock (_lock)
            {
                _instance = null;
                ConfigurationManager.RefreshSection("connectionStrings");
                ConfigurationManager.RefreshSection("appSettings");
            }
        }
    }
}
