using MultirRevisioVigencia.Domain.Interfaces;
using MultirRevisioVigencia.Domain.Interfaces;
using Serilog;
using Serilog.Events;
using System;
using System.Configuration;
using System.IO;

namespace MultirRevisioVigencia.Infrastructure.Logging
{
    /// <summary>
    /// Servei de logging basat en Serilog
    /// Proporciona logging estructurat amb suport per consola i fitxer
    /// </summary>
    public class SerilogLoggerService : ILoggerService, IDisposable
    {
        private readonly string _logFilePath;
        private readonly ILogger _logger;
        private bool _disposed = false;

        /// <summary>
        /// Constructor que inicialitza Serilog amb configuració personalitzada i suport per Seq
        /// </summary>
        /// <param name="logFilePath">Ruta completa del fitxer de log</param>
        /// <param name="nivellMinim">Nivell mínim de log (per defecte: Information)</param>
        public SerilogLoggerService(string logFilePath, LogEventLevel nivellMinim = LogEventLevel.Information)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                throw new ArgumentNullException(nameof(logFilePath));
            }

            _logFilePath = logFilePath;

            // Crear el directori si no existeix
            var directori = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrWhiteSpace(directori) && !Directory.Exists(directori))
            {
                Directory.CreateDirectory(directori);
            }

            // Obtenir configuració de Seq
            var seqActiu = bool.Parse(ConfigurationManager.AppSettings["Seq:Actiu"] ?? "false");
            var seqServerUrl = ConfigurationManager.AppSettings["Seq:ServerUrl"] ?? "http://localhost:5341";
            var seqApiKey = ConfigurationManager.AppSettings["Seq:ApiKey"];
            var entorn = ConfigurationManager.AppSettings["Entorn"] ?? "Preproduccio";

            // Configurar Serilog
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(nivellMinim)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "MultirRevisioVigencia")
                .Enrich.WithProperty("Environment", entorn)
                .WriteTo.File(
                    path: _logFilePath,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));

            // TODO: Afegir Seq quan el paquet estigui correctament referenciat
            // if (seqActiu)
            // {
            //     if (!string.IsNullOrWhiteSpace(seqApiKey))
            //     {
            //         loggerConfig.WriteTo.Seq(seqServerUrl, apiKey: seqApiKey);
            //     }
            //     else
            //     {
            //         loggerConfig.WriteTo.Seq(seqServerUrl);
            //     }
            // }

            _logger = loggerConfig.CreateLogger();

            // Assignar al logger global de Serilog per compatibilitat
            if (Log.Logger == null || Log.Logger.GetType().Name == "SilentLogger")
            {
                Log.Logger = _logger;
            }
        }

        /// <summary>
        /// Registra un missatge d'informació
        /// </summary>
        /// <param name="missatge">Missatge a registrar</param>
        public void Info(string missatge)
        {
            if (_disposed)
            {
                Console.WriteLine($"[WARNING] Logger disposed - missatge no registrat: {missatge}");
                return;
            }

            _logger.Information(missatge);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INF] {missatge}");
        }

        /// <summary>
        /// Registra un missatge d'advertència
        /// </summary>
        /// <param name="missatge">Missatge a registrar</param>
        public void Warning(string missatge)
        {
            if (_disposed)
            {
                Console.WriteLine($"[WARNING] Logger disposed - missatge no registrat: {missatge}");
                return;
            }

            _logger.Warning(missatge);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [WRN] {missatge}");
        }

        /// <summary>
        /// Registra un missatge d'error amb excepció opcional
        /// </summary>
        /// <param name="missatge">Missatge d'error</param>
        /// <param name="exception">Excepció associada (opcional)</param>
        public void Error(string missatge, Exception exception = null)
        {
            if (_disposed)
            {
                Console.WriteLine($"[ERROR] Logger disposed - error no registrat: {missatge}");
                if (exception != null)
                {
                    Console.WriteLine($"Exception: {exception.GetType().Name} - {exception.Message}");
                }
                return;
            }

            if (exception != null)
            {
                _logger.Error(exception, missatge);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERR] {missatge}");
                Console.WriteLine($"Exception: {exception.GetType().Name} - {exception.Message}");
            }
            else
            {
                _logger.Error(missatge);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERR] {missatge}");
            }
        }

        /// <summary>
        /// Obté la ruta del fitxer de log
        /// </summary>
        /// <returns>Ruta completa del fitxer de log</returns>
        public string GetLogFilePath()
        {
            return _logFilePath;
        }

        /// <summary>
        /// Allibera els recursos utilitzats pel logger
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Allibera els recursos gestionats i no gestionats
        /// </summary>
        /// <param name="disposing">True si s'està disposant explícitament</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Tancar i flush el logger de Serilog
                    (_logger as IDisposable)?.Dispose();
                    Log.CloseAndFlush();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalitzador per assegurar que es fa flush dels logs pendents
        /// </summary>
        ~SerilogLoggerService()
        {
            Dispose(false);
        }
    }
}
