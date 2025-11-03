using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using MultirIntegraModulab.Domain.Interfaces;

namespace MultirIntegraModulab.Infrastructure.Configuration
{
    /// <summary>
    /// Implementació del servei de configuració
    /// Llegeix la configuració directament de App.config
    /// Suporta múltiples entorns (Producció i Preproducció)
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private const string ENTORN_PRODUCCIO = "Produccio";
        private const string ENTORN_PREPRODUCCIO = "Preproduccio";

        public ConfigurationService()
        {
            // Validar configuració en la construcció
            ValidarConfiguracio();
        }

        #region Configuració d'Entorn

        public string Entorn
        {
            get
            {
                var entorn = LlegirStringConfiguracio("Entorn", ENTORN_PREPRODUCCIO);
                
                // Normalitzar el valor
                if (entorn.Equals(ENTORN_PRODUCCIO, StringComparison.OrdinalIgnoreCase))
                    return ENTORN_PRODUCCIO;
                if (entorn.Equals(ENTORN_PREPRODUCCIO, StringComparison.OrdinalIgnoreCase))
                    return ENTORN_PREPRODUCCIO;
                
                // Per defecte, preproducció
                return ENTORN_PREPRODUCCIO;
            }
        }

        public bool EsEntornProduccio
        {
            get { return Entorn.Equals(ENTORN_PRODUCCIO, StringComparison.OrdinalIgnoreCase); }
        }

        public bool EsEntornPreproduccio
        {
            get { return Entorn.Equals(ENTORN_PREPRODUCCIO, StringComparison.OrdinalIgnoreCase); }
        }

        #endregion

        #region Connection Strings

        public string OracleConnectionString
        {
            get 
            {
                var connectionName = EsEntornProduccio 
                    ? "OracleModulab_Produccio" 
                    : "OracleModulab_Preproduccio";

                var connectionString = ConfigurationManager.ConnectionStrings[connectionName]?.ConnectionString;
                
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new ConfigurationErrorsException(
                        $"No s'ha trobat la connexió '{connectionName}' a App.config per l'entorn {Entorn}");

                return connectionString;
            }
        }

        public string MySqlConnectionString
        {
            get 
            {
                var connectionName = EsEntornProduccio 
                    ? "MySqlMultiR_Produccio" 
                    : "MySqlMultiR_Preproduccio";

                var connectionString = ConfigurationManager.ConnectionStrings[connectionName]?.ConnectionString;
                
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new ConfigurationErrorsException(
                        $"No s'ha trobat la connexió '{connectionName}' a App.config per l'entorn {Entorn}");

                return connectionString;
            }
        }

        #endregion

        #region WebServices

        public string WebServicePacientsUrl
        {
            get
            {
                var configKey = EsEntornProduccio 
                    ? "WebServicePacients_Produccio" 
                    : "WebServicePacients_Preproduccio";

                var url = LlegirStringConfiguracio(configKey, "");
                
                if (string.IsNullOrWhiteSpace(url))
                    throw new ConfigurationErrorsException(
                        $"No s'ha trobat la configuració '{configKey}' a App.config per l'entorn {Entorn}");

                return url;
            }
        }

        public int WebServiceTimeout
        {
            get { return LlegirIntConfiguracio("WebServiceTimeout", 30); }
        }

        #endregion

        #region Configuració de Càrrega

        public int DiesEndarreraCarrega
        {
            get { return LlegirIntConfiguracio("DiesEndarreraCarrega", 1); }
        }

        public int LimitResultatsProves
        {
            get { return LlegirIntConfiguracio("LimitResultatsProves", 0); }
        }

        public bool EntornProduccion
        {
            get { return EsEntornProduccio; }
        }

        #endregion

        #region Configuració de Logging

        public string LogDirectory
        {
            get { return LlegirStringConfiguracio("LogDirectory", "Logs"); }
        }

        public string LogLevel
        {
            get { return LlegirStringConfiguracio("LogLevel", "Info"); }
        }

        #endregion

        #region Configuració de Cache

        public int MinutsVigenciaCache
        {
            get { return LlegirIntConfiguracio("MinutsVigenciaCache", 30); }
        }

        #endregion

        #region Configuració de Manteniment

        public int DiesRetencioHistorial
        {
            get { return LlegirIntConfiguracio("DiesRetencioHistorial", 90); }
        }

        #endregion

        #region Configuració de Processament

        public bool ProcessarMostresEnParalel
        {
            get { return LlegirBoolConfiguracio("ProcessarMostresEnParalel", false); }
        }

        public int MaxGrauParalelisme
        {
            get { return LlegirIntConfiguracio("MaxGrauParalelisme", 4); }
        }

        #endregion

        #region Configuració d'Email

        public bool EnviarEmailLog
        {
            get { return LlegirBoolConfiguracio("EnviarEmailLog", false); }
        }

        public string SmtpServer
        {
            get { return LlegirStringConfiguracio("SmtpServer", ""); }
        }

        public int SmtpPort
        {
            get { return LlegirIntConfiguracio("SmtpPort", 587); }
        }

        public string SmtpUsuari
        {
            get { return LlegirStringConfiguracio("SmtpUsuari", ""); }
        }

        public string SmtpPassword
        {
            get { return LlegirStringConfiguracio("SmtpPassword", ""); }
        }

        public bool SmtpUsarSSL
        {
            get { return LlegirBoolConfiguracio("SmtpUsarSSL", true); }
        }

        public string EmailFrom
        {
            get { return LlegirStringConfiguracio("EmailFrom", ""); }
        }

        public List<string> EmailsDestinataris
        {
            get 
            { 
                var emails = LlegirStringConfiguracio("EmailsDestinataris", "");
                return emails.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
            }
        }

        public bool EmailNomesEnErrors
        {
            get { return LlegirBoolConfiguracio("EmailNomesEnErrors", false); }
        }

        #endregion

        #region Validació i Resum

        public void ValidarConfiguracio()
        {
            var errors = new List<string>();

            // Validar entorn
            var entornConfig = ConfigurationManager.AppSettings["Entorn"];
            if (string.IsNullOrWhiteSpace(entornConfig))
            {
                errors.Add("Falta la configuració 'Entorn' (ha de ser 'Produccio' o 'Preproduccio')");
            }
            else if (!entornConfig.Equals(ENTORN_PRODUCCIO, StringComparison.OrdinalIgnoreCase) &&
                     !entornConfig.Equals(ENTORN_PREPRODUCCIO, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Valor invàlid per 'Entorn': '{entornConfig}'. Ha de ser 'Produccio' o 'Preproduccio'");
            }

            // Validar connection strings per l'entorn actiu
            var oracleConnectionName = EsEntornProduccio 
                ? "OracleModulab_Produccio" 
                : "OracleModulab_Preproduccio";
            
            var mysqlConnectionName = EsEntornProduccio 
                ? "MySqlMultiR_Produccio" 
                : "MySqlMultiR_Preproduccio";

            if (string.IsNullOrWhiteSpace(ConfigurationManager.ConnectionStrings[oracleConnectionName]?.ConnectionString))
                errors.Add($"Falta la cadena de connexió '{oracleConnectionName}' per l'entorn {Entorn}");

            if (string.IsNullOrWhiteSpace(ConfigurationManager.ConnectionStrings[mysqlConnectionName]?.ConnectionString))
                errors.Add($"Falta la cadena de connexió '{mysqlConnectionName}' per l'entorn {Entorn}");

            // Validar WebService per l'entorn actiu
            var webServiceKey = EsEntornProduccio 
                ? "WebServicePacients_Produccio" 
                : "WebServicePacients_Preproduccio";

            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[webServiceKey]))
                errors.Add($"Falta la configuració '{webServiceKey}' per l'entorn {Entorn}");

            // Validar configuració d'email si està activat
            if (EnviarEmailLog)
            {
                if (string.IsNullOrWhiteSpace(SmtpServer))
                    errors.Add("'EnviarEmailLog' està activat però 'SmtpServer' està buit");

                if (string.IsNullOrWhiteSpace(EmailFrom))
                    errors.Add("'EnviarEmailLog' està activat però 'EmailFrom' està buit");

                if (!EmailsDestinataris.Any())
                    errors.Add("'EnviarEmailLog' està activat però 'EmailsDestinataris' està buit");

                // NOTA: No validem SmtpUsuari i SmtpPassword perquè alguns servidors
                // SMTP interns (com smtp.trueta.intranet) no requereixen autenticació
            }

            // Validar valors numèrics
            if (DiesEndarreraCarrega < 0)
                errors.Add("'DiesEndarreraCarrega' ha de ser >= 0");

            if (LimitResultatsProves < 0)
                errors.Add("'LimitResultatsProves' ha de ser >= 0");

            if (MinutsVigenciaCache < 0)
                errors.Add("'MinutsVigenciaCache' ha de ser >= 0");

            if (DiesRetencioHistorial < 0)
                errors.Add("'DiesRetencioHistorial' ha de ser >= 0");

            if (MaxGrauParalelisme < 1)
                errors.Add("'MaxGrauParalelisme' ha de ser >= 1");

            if (WebServiceTimeout < 1)
                errors.Add("'WebServiceTimeout' ha de ser >= 1");

            if (errors.Any())
            {
                throw new ConfigurationErrorsException(
                    "Errors de configuració:\n" + string.Join("\n", errors));
            }
        }

        public string ObtenirResumConfiguracio()
        {
            // Determinar si s'utilitza autenticació SMTP
            bool utilitzaAutenticacio = !string.IsNullOrWhiteSpace(SmtpUsuari) && 
                                        !EsValorPerDefecte(SmtpUsuari) &&
                                        !string.IsNullOrWhiteSpace(SmtpPassword) &&
                                        !EsValorPerDefecte(SmtpPassword);

            // Obtenir URL del webservice (sense mostrar tot el path per seguretat)
            string wsUrl = WebServicePacientsUrl;
            if (wsUrl.Length > 50)
                wsUrl = wsUrl.Substring(0, 47) + "...";

            return $@"
==============================================
CONFIGURACIÓ DE L'APLICACIÓ
==============================================

ENTORN:
  - Entorn actiu: {Entorn.ToUpper()}
  - És producció: {(EsEntornProduccio ? "SÍ" : "NO")}

CÀRREGA DE DADES:
  - Dies enrere: {DiesEndarreraCarrega}
  - Límit resultats: {(LimitResultatsProves == 0 ? "Il·limitat" : LimitResultatsProves.ToString())}

LOGGING:
  - Directori: {LogDirectory}
  - Nivell: {LogLevel}

EMAIL:
  - Enviar email: {(EnviarEmailLog ? "SÍ" : "NO")}
  - Servidor SMTP: {SmtpServer}
  - Port: {SmtpPort}
  - Usar SSL: {(SmtpUsarSSL ? "SÍ" : "NO")}
  - Autenticació: {(utilitzaAutenticacio ? "SÍ" : "NO (connexió anònima)")}
  - Des de: {EmailFrom}
  - Destinataris: {string.Join(", ", EmailsDestinataris)}
  - Només errors: {(EmailNomesEnErrors ? "SÍ" : "NO")}

CACHE:
  - Vigència: {MinutsVigenciaCache} minuts

MANTENIMENT:
  - Retenció historial: {DiesRetencioHistorial} dies

PROCESSAMENT:
  - En paral·lel: {(ProcessarMostresEnParalel ? "SÍ" : "NO")}
  - Grau paral·lelisme: {MaxGrauParalelisme}

CONNEXIONS PER {Entorn.ToUpper()}:
  - Modulab (Oracle): Configurada
  - MultiR (MySQL): Configurada
  - WebService Pacients: {wsUrl}
  - WebService Timeout: {WebServiceTimeout}s

==============================================";
        }

        #endregion

        #region Mètodes Auxiliars Privats

        private string LlegirStringConfiguracio(string clau, string valorPerDefecte)
        {
            var valor = ConfigurationManager.AppSettings[clau];
            return string.IsNullOrWhiteSpace(valor) ? valorPerDefecte : valor;
        }

        private int LlegirIntConfiguracio(string clau, int valorPerDefecte)
        {
            var valor = ConfigurationManager.AppSettings[clau];
            if (string.IsNullOrWhiteSpace(valor))
                return valorPerDefecte;

            if (int.TryParse(valor, out int resultat))
                return resultat;

            return valorPerDefecte;
        }

        private bool LlegirBoolConfiguracio(string clau, bool valorPerDefecte)
        {
            var valor = ConfigurationManager.AppSettings[clau];
            if (string.IsNullOrWhiteSpace(valor))
                return valorPerDefecte;

            if (bool.TryParse(valor, out bool resultat))
                return resultat;

            // Acceptar també "1"/"0", "yes"/"no", etc.
            valor = valor.ToLower();
            if (valor == "1" || valor == "yes" || valor == "true" || valor == "sí" || valor == "si")
                return true;
            if (valor == "0" || valor == "no" || valor == "false")
                return false;

            return valorPerDefecte;
        }

        /// <summary>
        /// Comprova si un valor és un valor per defecte o d'exemple que no s'hauria d'utilitzar
        /// </summary>
        private bool EsValorPerDefecte(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return true;

            // Valors d'exemple comunes que indiquen que no s'ha configurat
            var valorsExemple = new[]
            {
                "usuari@exemple.com",
                "example@example.com",
                "PASSWORD_SMTP",
                "password",
                "changeme",
                "CHANGE_ME",
                "exemple",
                "example"
            };

            return valorsExemple.Any(ve => valor.IndexOf(ve, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        #endregion
    }
}
