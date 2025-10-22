namespace MultirIntegraModulab.Domain.Interfaces
{
    /// <summary>
    /// Port (interfície) per al sistema de configuració
    /// Permet canviar la font de configuració sense afectar el domini
    /// </summary>
    public interface IConfigurationService
    {
        // Connection Strings
        string OracleConnectionString { get; }
        string MySqlConnectionString { get; }

        // Configuració de càrrega
        int DiesEndarreraCarrega { get; }
        int LimitResultatsProves { get; }
        bool EntornProduccion { get; }

        // Configuració de logging
        string LogDirectory { get; }
        string LogLevel { get; }

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
