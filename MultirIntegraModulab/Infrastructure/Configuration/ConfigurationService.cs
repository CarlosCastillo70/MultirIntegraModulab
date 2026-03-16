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

        // ──────────────────────────────────────────────────────────────
        // TIPUS 1: CÀRREGA INCREMENTAL
        // ──────────────────────────────────────────────────────────────
        
        /// <summary>
        /// Indica si la càrrega incremental està activada
        /// Prioritat: ALTA (s'executa primer si està activat)
        /// </summary>
        public bool CarregaIncremental_Activa
        {
            get { return LlegirBoolConfiguracio("CarregaIncremental_Activa", false); }
        }

        /// <summary>
        /// Dies de revisió de seguretat per la càrrega incremental
        /// </summary>
        public int CarregaIncremental_DiesRevisioSeguretat
        {
            get { return LlegirIntConfiguracio("CarregaIncremental_DiesRevisioSeguretat", 7); }
        }

        // ──────────────────────────────────────────────────────────────
        // TIPUS 2: CÀRREGA PER DIES ENRERE
        // ──────────────────────────────────────────────────────────────
        
        /// <summary>
        /// Indica si la càrrega per dies enrere està activada
        /// Prioritat: MITJANA (s'executa si Incremental no està activa)
        /// </summary>
        public bool CarregaDiesEnrere_Activa
        {
            get { return LlegirBoolConfiguracio("CarregaDiesEnrere_Activa", true); }
        }

        /// <summary>
        /// Nombre de dies enrere per carregar mostres
        /// </summary>
        public int CarregaDiesEnrere_NombreDies
        {
            get { return LlegirIntConfiguracio("CarregaDiesEnrere_NombreDies", 1); }
        }

        // ──────────────────────────────────────────────────────────────
        // TIPUS 3: CÀRREGA PER RANG DE DATES
        // ──────────────────────────────────────────────────────────────
        
        /// <summary>
        /// Indica si la càrrega per rang de dates està activada
        /// Prioritat: BAIXA (s'executa si Incremental i DiesEnrere no estan actives)
        /// </summary>
        public bool CarregaRangDates_Activa
        {
            get { return LlegirBoolConfiguracio("CarregaRangDates_Activa", false); }
        }

        /// <summary>
        /// Data d'inici del rang per la càrrega
        /// Format esperat: dd/MM/yyyy
        /// </summary>
        public DateTime? CarregaRangDates_DataInici
        {
            get 
            { 
                var dataStr = LlegirStringConfiguracio("CarregaRangDates_DataInici", "");
                if (string.IsNullOrWhiteSpace(dataStr))
                    return null;

                if (DateTime.TryParseExact(dataStr, "dd/MM/yyyy", 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out DateTime data))
                {
                    return data;
                }

                return null;
            }
        }

        /// <summary>
        /// Data de fi del rang per la càrrega
        /// Format esperat: dd/MM/yyyy
        /// </summary>
        public DateTime? CarregaRangDates_DataFi
        {
            get 
            { 
                var dataStr = LlegirStringConfiguracio("CarregaRangDates_DataFi", "");
                if (string.IsNullOrWhiteSpace(dataStr))
                    return null;

                if (DateTime.TryParseExact(dataStr, "dd/MM/yyyy", 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out DateTime data))
                {
                    return data;
                }

                return null;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // PROPIETATS DE COMPATIBILITAT (per no trencar codi existent)
        // ──────────────────────────────────────────────────────────────
        
        /// <summary>
        /// OBSOLET: Utilitzar CarregaIncremental_Activa
        /// Mantingut per compatibilitat amb codi existent
        /// </summary>
        [Obsolete("Utilitzar CarregaIncremental_Activa en el seu lloc")]
        public bool CarregaIncremental
        {
            get { return CarregaIncremental_Activa; }
        }

        /// <summary>
        /// OBSOLET: Utilitzar CarregaDiesEnrere_NombreDies
        /// Mantingut per compatibilitat amb codi existent
        /// </summary>
        [Obsolete("Utilitzar CarregaDiesEnrere_NombreDies en el seu lloc")]
        public int DiesEndarreraCarrega
        {
            get { return CarregaDiesEnrere_NombreDies; }
        }

        // ──────────────────────────────────────────────────────────────
        // PARÀMETRES COMUNS
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Límit de resultats per proves (0 = il·limitat)
        /// </summary>
        public int LimitResultatsProves
        {
            get { return LlegirIntConfiguracio("LimitResultatsProves", 0); }
        }

        /// <summary>
        /// Indica si l'entorn és de producció
        /// </summary>
        public bool EntornProduccion
        {
            get { return EsEntornProduccio; }
        }

        #endregion

        #region Configuració de Filtratge

        public List<string> EtiquetesMostresAProcessar
        {
            get 
            { 
                var etiquetes = LlegirStringConfiguracio("EtiquetesMostresAProcessar", "");
                return etiquetes.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
            }
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

        public virtual int MinutsVigenciaCache
        {
            get { return LlegirIntConfiguracio("MinutsVigenciaCache", 30); }
        }

        #endregion

        #region Configuració de Manteniment

        public virtual int DiesRetencioHistorial
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

        public virtual bool EnviarEmailLog
        {
            get { return LlegirBoolConfiguracio("EnviarEmailLog", false); }
        }

        public virtual string SmtpServer
        {
            get { return LlegirStringConfiguracio("SmtpServer", ""); }
        }

        public virtual int SmtpPort
        {
            get { return LlegirIntConfiguracio("SmtpPort", 587); }
        }

        public virtual string SmtpUsuari
        {
            get { return LlegirStringConfiguracio("SmtpUsuari", ""); }
        }

        public virtual string SmtpPassword
        {
            get { return LlegirStringConfiguracio("SmtpPassword", ""); }
        }

        public virtual bool SmtpUsarSSL
        {
            get { return LlegirBoolConfiguracio("SmtpUsarSSL", true); }
        }

        public virtual string EmailFrom
        {
            get { return LlegirStringConfiguracio("EmailFrom", ""); }
        }

        public virtual List<string> EmailsDestinataris
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

        public virtual bool EmailNomesEnErrors
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

            // Validar configuració de càrrega
            int tipusCarregaActius = 0;
            if (CarregaIncremental_Activa) tipusCarregaActius++;
            if (CarregaDiesEnrere_Activa) tipusCarregaActius++;
            if (CarregaRangDates_Activa) tipusCarregaActius++;

            if (tipusCarregaActius == 0)
            {
                errors.Add("Cap tipus de càrrega està activat. Activar almenys un tipus: CarregaIncremental_Activa, CarregaDiesEnrere_Activa o CarregaRangDates_Activa");
            }

            // Validar paràmetres específics de càrrega per rang de dates
            if (CarregaRangDates_Activa)
            {
                if (!CarregaRangDates_DataInici.HasValue)
                    errors.Add("'CarregaRangDates_Activa' està activat però 'CarregaRangDates_DataInici' no és vàlid (format esperat: dd/MM/yyyy)");

                if (!CarregaRangDates_DataFi.HasValue)
                    errors.Add("'CarregaRangDates_Activa' està activat però 'CarregaRangDates_DataFi' no és vàlid (format esperat: dd/MM/yyyy)");

                if (CarregaRangDates_DataInici.HasValue && CarregaRangDates_DataFi.HasValue)
                {
                    if (CarregaRangDates_DataInici.Value > CarregaRangDates_DataFi.Value)
                        errors.Add("'CarregaRangDates_DataInici' ha de ser anterior o igual a 'CarregaRangDates_DataFi'");
                }
            }

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
            if (CarregaDiesEnrere_NombreDies < 0)
                errors.Add("'CarregaDiesEnrere_NombreDies' ha de ser >= 0");

            if (CarregaIncremental_DiesRevisioSeguretat < 0)
                errors.Add("'CarregaIncremental_DiesRevisioSeguretat' ha de ser >= 0");

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

        public virtual string ObtenirResumConfiguracio()
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
                
            // Obtenir resum d'etiquetes a processar
            var etiquetesResum = EtiquetesMostresAProcessar.Any() 
                ? $"{EtiquetesMostresAProcessar.Count} etiqueta(es): {string.Join(", ", EtiquetesMostresAProcessar)}"
                : "Totes les mostres";

            // Determinar quin tipus de càrrega s'utilitzarà
            string tipusCarrega = "CAP (ERROR: Cap tipus activat)";
            string detallsCarrega = "";

            if (CarregaIncremental_Activa)
            {
                tipusCarrega = "INCREMENTAL (Prioritat Alta)";
                detallsCarrega = $@"
  • Dies revisió seguretat: {CarregaIncremental_DiesRevisioSeguretat} dies
  • Descripció: Carrega només dades noves des de l'última sincronització";
            }
            else if (CarregaDiesEnrere_Activa)
            {
                tipusCarrega = "DIES ENRERE (Prioritat Mitjana)";
                detallsCarrega = $@"
  • Nombre de dies: {CarregaDiesEnrere_NombreDies} dies enrere
  • Descripció: Carrega dades dels últims N dies cap enrere";
            }
            else if (CarregaRangDates_Activa)
            {
                tipusCarrega = "RANG DE DATES (Prioritat Baixa)";
                string dataIniciStr = CarregaRangDates_DataInici.HasValue 
                    ? CarregaRangDates_DataInici.Value.ToString("dd/MM/yyyy") 
                    : "NO CONFIGURAT";
                string dataFiStr = CarregaRangDates_DataFi.HasValue 
                    ? CarregaRangDates_DataFi.Value.ToString("dd/MM/yyyy") 
                    : "NO CONFIGURAT";
                
                detallsCarrega = $@"
  • Data inici: {dataIniciStr}
  • Data fi: {dataFiStr}
  • Descripció: Carrega dades d'un període específic";
            }

            // Mostrar avís si hi ha més d'un tipus activat
            string avisMultiplesActius = "";
            int comptadorActius = 0;
            if (CarregaIncremental_Activa) comptadorActius++;
            if (CarregaDiesEnrere_Activa) comptadorActius++;
            if (CarregaRangDates_Activa) comptadorActius++;

            if (comptadorActius > 1)
            {
                avisMultiplesActius = $@"
  ⚠️  AVÍS: Hi ha {comptadorActius} tipus de càrrega activats simultàniament
      Només s'executarà el PRIMER tipus (segons prioritat)";
            }

            return $@"
==============================================
CONFIGURACIÓ DE L'APLICACIÓ
==============================================

ENTORN:
  - Entorn actiu: {Entorn.ToUpper()}
  - És producció: {(EsEntornProduccio ? "SÍ" : "NO")}

CÀRREGA DE DADES:
  - Tipus de càrrega: {tipusCarrega}{detallsCarrega}{avisMultiplesActius}
  - Límit resultats: {(LimitResultatsProves == 0 ? "Il·limitat" : LimitResultatsProves.ToString())}
  
  Estats de càrrega:
    1. Incremental: {(CarregaIncremental_Activa ? "✅ ACTIVA" : "✗ Inactiva")}
    2. Dies Enrere: {(CarregaDiesEnrere_Activa ? "✅ ACTIVA" : "✗ Inactiva")}
    3. Rang Dates:  {(CarregaRangDates_Activa ? "✅ ACTIVA" : "✗ Inactiva")}

FILTRATGE DE MOSTRES:
  - Etiquetes a processar: {etiquetesResum}

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
