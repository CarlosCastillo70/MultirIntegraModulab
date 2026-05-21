using System;
using Serilog;
using Serilog.Events;
using MultirIntegraModulab.Domain.Interfaces;

namespace MultirIntegraModulab.Infrastructure.ExternalServices.Logger
{
    public class LoggerService : ILoggerService, IDisposable
    {
        private DateTime _iniciExecucio;
        private readonly string _rutaLogFile;
        private readonly ILogger _logger;
        private bool _disposed = false;

        public LoggerService()
        {
            // Obtenir entorn de configuració
            var entorn = System.Configuration.ConfigurationManager.AppSettings["Entorn"] ?? "Preproduccio";
            var suffixEntorn = entorn.Equals("Produccio", StringComparison.OrdinalIgnoreCase) ? "pro" : "pre";

            // Obtenir ruta del log des de configuració
            var rutaTemplate = System.Configuration.ConfigurationManager.AppSettings["RutaFitxerLog"] 
                ?? "Logs\\multir{0:yyyy-MM-dd_HH-mm-ss}_{1}.log";
            
            _rutaLogFile = string.Format(rutaTemplate, DateTime.Now, suffixEntorn);
            
            // Assegurar que existeix el directori
            var directoriLog = System.IO.Path.GetDirectoryName(_rutaLogFile);
            if (!string.IsNullOrEmpty(directoriLog) && !System.IO.Directory.Exists(directoriLog))
            {
                System.IO.Directory.CreateDirectory(directoriLog);
            }

            // Obtenir nivell mínim des de configuració
            var nivellConfig = System.Configuration.ConfigurationManager.AppSettings["Serilog:MinimumLevel"] ?? "Information";
            var nivellMinim = ParseLogLevel(nivellConfig);

            // Obtenir configuració de Seq
            var seqActiu = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["Seq:Actiu"] ?? "false");
            var seqServerUrl = System.Configuration.ConfigurationManager.AppSettings["Seq:ServerUrl"] ?? "http://localhost:5341";
            var seqApiKey = System.Configuration.ConfigurationManager.AppSettings["Seq:ApiKey"];

            // Configurar Serilog amb suport per Seq
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(nivellMinim)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "MultirIntegraModulab")
                .Enrich.WithProperty("Environment", entorn)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: _rutaLogFile,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));

            // Afegir Seq si està actiu (amb gestió d'errors resilient)
            string seqStatusMessage = null;
            Exception seqException = null;
            
            if (seqActiu)
            {
                try
                {
                    // Comprovar si Seq està accessible abans de configurar-lo
                    if (ComprovarSeqDisponible(seqServerUrl))
                    {
                        if (!string.IsNullOrWhiteSpace(seqApiKey))
                        {
                            loggerConfig.WriteTo.Seq(seqServerUrl, apiKey: seqApiKey);
                        }
                        else
                        {
                            loggerConfig.WriteTo.Seq(seqServerUrl);
                        }
                        
                        seqStatusMessage = $"✅ Seq connectat correctament a {seqServerUrl}";
                    }
                    else
                    {
                        seqStatusMessage = $"⚠️ Seq no està disponible a {seqServerUrl}. Els logs es guardaran només a fitxer i consola.";
                    }
                }
                catch (Exception ex)
                {
                    seqException = ex;
                    seqStatusMessage = $"⚠️ Error configurant Seq a {seqServerUrl}. Els logs es guardaran només a fitxer i consola.";
                }
            }

            // Crear el logger una sola vegada
            _logger = loggerConfig.CreateLogger();
            
            // Ara sí podem registrar els missatges sobre Seq
            if (!string.IsNullOrEmpty(seqStatusMessage))
            {
                if (seqException != null)
                {
                    _logger.Warning(seqException, seqStatusMessage);
                }
                else if (seqStatusMessage.Contains("⚠️"))
                {
                    _logger.Warning(seqStatusMessage);
                }
                else
                {
                    _logger.Information(seqStatusMessage);
                }
            }

            // MILLORA: Només assignar al Log global si és null (evita sobreescriure)
            if (Log.Logger == null || Log.Logger.GetType().Name == "SilentLogger")
            {
                Log.Logger = _logger;
            }
        }

        /// <summary>
        /// Converteix el nivell de log des de string a LogEventLevel
        /// </summary>
        private LogEventLevel ParseLogLevel(string nivell)
        {
            switch (nivell?.ToLowerInvariant())
            {
                case "verbose":
                case "debug":
                    return LogEventLevel.Debug;
                case "info":
                case "information":
                    return LogEventLevel.Information;
                case "warning":
                case "warn":
                    return LogEventLevel.Warning;
                case "error":
                    return LogEventLevel.Error;
                case "fatal":
                    return LogEventLevel.Fatal;
                default:
                    return LogEventLevel.Information;
            }
        }

        public void MarcarIniciExecucio()
        {
            _iniciExecucio = DateTime.Now;
            _logger.Information("========================================");
            _logger.Information("INICI EXECUCIÓ: {DataHora}", _iniciExecucio.ToString("dd/MM/yyyy HH:mm:ss"));
            _logger.Information("========================================");
        }

        public void MarcarFinalExecucio()
        {
            if (_disposed) return;

            var durada = DateTime.Now - _iniciExecucio;
            _logger.Information("========================================");
            _logger.Information("FINAL EXECUCIÓ: {DataHora}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            _logger.Information("DURADA TOTAL: {Durada}", durada.ToString(@"hh\:mm\:ss"));
            _logger.Information("========================================");
            
            FlushLogs();
        }

        public void Info(string missatge)
        {
            if (_disposed) return;
            _logger.Information(missatge);
        }

        public void Warning(string missatge)
        {
            if (_disposed) return;
            _logger.Warning(missatge);
        }

        public void Error(string missatge, Exception ex = null)
        {
            if (_disposed) return;
            
            if (ex != null)
            {
                _logger.Error(ex, missatge);
            }
            else
            {
                _logger.Error(missatge);
            }
        }

        public void Debug(string missatge)
        {
            if (_disposed) return;
            _logger.Debug(missatge);
        }

        /// <summary>
        /// Genera logs de prova amb diferents nivells per configurar Seq
        /// NOTA: Aquest mètode és temporal per configurar signals a Seq
        /// </summary>
        public void GenerarLogsDeProva()
        {
            _logger.Debug("🔍 Log de DEBUG - Aquest és un missatge de debugging");
            _logger.Information("ℹ️ Log de INFORMATION - Aquest és un missatge informatiu");
            _logger.Warning("⚠️ Log de WARNING - Aquest és un avís");
            _logger.Error("❌ Log de ERROR - Aquest és un error");
            _logger.Fatal("💀 Log de FATAL - Aquest és un error crític");
        }

        public string ObtenirRutaLogAvui()
        {
            return _rutaLogFile;
        }

        public bool ExisteixLogAvui()
        {
            return System.IO.File.Exists(_rutaLogFile);
        }

        public long ObtenirMidaLogAvui()
        {
            if (!System.IO.File.Exists(_rutaLogFile))
                return 0;
            
            try
            {
                var fileInfo = new System.IO.FileInfo(_rutaLogFile);
                return fileInfo.Length;
            }
            catch
            {
                return 0;
            }
        }

        public void FlushLogs()
        {
            if (_disposed) return;
            Log.CloseAndFlush();
        }

        /// <summary>
        /// Comprova si el servidor Seq està disponible i accessible
        /// </summary>
        private bool ComprovarSeqDisponible(string seqServerUrl)
        {
            try
            {
                var uri = new Uri(seqServerUrl);
                
                // Utilitzar HttpWebRequest per tenir més control sobre la petició
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(uri);
                request.Method = "GET";
                request.Timeout = 5000; // 5 segons - més generós per Seq que s'està iniciant
                request.ReadWriteTimeout = 5000;
                request.UserAgent = "MultirIntegraModulab-HealthCheck/1.0";
                request.KeepAlive = false;
                request.AllowAutoRedirect = true;
                
                try
                {
                    using (var response = (System.Net.HttpWebResponse)request.GetResponse())
                    {
                        // Si arriba aquí amb codi 200, Seq està disponible i funcional
                        return response.StatusCode == System.Net.HttpStatusCode.OK;
                    }
                }
                catch (System.Net.WebException webEx)
                {
                    // Analitzar la resposta per determinar si Seq està actiu
                    if (webEx.Response != null)
                    {
                        var httpResponse = (System.Net.HttpWebResponse)webEx.Response;
                        
                        // Aquests codis HTTP indiquen que Seq està actiu i responent
                        // 200: OK - Seq disponible
                        // 302/301: Redirect - Seq està redirigint (probablement a /login o similar)
                        // 401/403: Autenticació requerida - Seq està actiu però protegit
                        if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK ||
                            httpResponse.StatusCode == System.Net.HttpStatusCode.Redirect ||
                            httpResponse.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                            httpResponse.StatusCode == System.Net.HttpStatusCode.Found ||
                            httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                            httpResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            return true;
                        }
                        
                        // 404 o altres errors del servidor - Seq pot estar actiu però amb problemes
                        // En aquest cas, millor intentar connectar-se igualment
                        if ((int)httpResponse.StatusCode >= 400 && (int)httpResponse.StatusCode < 500)
                        {
                            return true; // Seq està responent, encara que amb error
                        }
                    }
                    
                    // Analitzar l'estat de l'excepció
                    // ConnectFailure: No es pot connectar al servidor (Seq no està en funcionament)
                    // NameResolutionFailure: No es pot resoldre el nom del servidor
                    // Timeout: El servidor no respon en el temps establert
                    if (webEx.Status == System.Net.WebExceptionStatus.ConnectFailure ||
                        webEx.Status == System.Net.WebExceptionStatus.NameResolutionFailure ||
                        webEx.Status == System.Net.WebExceptionStatus.Timeout)
                    {
                        return false; // Seq definitivament no està disponible
                    }
                    
                    // Per altres errors de WebException, assumir que Seq podria estar disponible
                    // (millor intentar enviar logs i que Serilog gestioni l'error)
                    return true;
                }
            }
            catch (System.UriFormatException)
            {
                // URL mal formada - error de configuració
                return false;
            }
            catch (System.NotSupportedException)
            {
                // Protocol no suportat
                return false;
            }
            catch
            {
                // Per qualsevol altre error inesperat, ser optimista i intentar-ho
                // Serilog gestionarà els errors de connexió de manera resilient
                return true;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                Log.CloseAndFlush();
            }
            catch
            {
                // Ignorar errors al tancar
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
