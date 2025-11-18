namespace MultirIntegraModulab.Domain.Interfaces
{
    /// <summary>
    /// Port (interfície) per al sistema de configuració
    /// Permet canviar la font de configuració sense afectar el domini
    /// </summary>
    public interface IConfigurationService
    {
        // Configuració d'entorn
        string Entorn { get; }
        bool EsEntornProduccio { get; }
        bool EsEntornPreproduccio { get; }

        // Connection Strings
        string OracleConnectionString { get; }
        string MySqlConnectionString { get; }

        // WebServices
        string WebServicePacientsUrl { get; }
        int WebServiceTimeout { get; }

        // Configuració de càrrega
        int DiesEndarreraCarrega { get; }
        int LimitResultatsProves { get; }
        bool EntornProduccion { get; }

        // Configuració de filtratge
        System.Collections.Generic.List<string> EtiquetesMostresAProcessar { get; }

        // Configuració de logging
        string LogDirectory { get; }
        string LogLevel { get; }

        // Configuració d'email
        bool EnviarEmailLog { get; }
        string SmtpServer { get; }
        int SmtpPort { get; }
        string SmtpUsuari { get; }
        string SmtpPassword { get; }
        bool SmtpUsarSSL { get; }
        string EmailFrom { get; }
        System.Collections.Generic.List<string> EmailsDestinataris { get; }
        bool EmailNomesEnErrors { get; }

        // Configuració de cache
        int MinutsVigenciaCache { get; }

        // Configuració de manteniment
        int DiesRetencioHistorial { get; }

        // Configuració de processament
        bool ProcessarMostresEnParalel { get; }
        int MaxGrauParalelisme { get; }

        void ValidarConfiguracio();
        string ObtenirResumConfiguracio();
    }
}
