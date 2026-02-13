using MultirRevisioVigencia.Domain.Interfaces;
using System;
using System.IO;
using System.Text;

namespace MultirRevisioVigencia.Infrastructure.Logging
{
    /// <summary>
    /// Servei de logging que escriu a un fitxer de text
    /// </summary>
    public class FileLoggerService : ILoggerService
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        public FileLoggerService(string logFilePath)
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
        }

        public void Info(string missatge)
        {
            EscriureLog("INFO", missatge);
        }

        public void Warning(string missatge)
        {
            EscriureLog("WARNING", missatge);
        }

        public void Error(string missatge, Exception exception = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine(missatge);

            if (exception != null)
            {
                sb.AppendLine($"Exception: {exception.GetType().Name}");
                sb.AppendLine($"Message: {exception.Message}");
                sb.AppendLine($"StackTrace: {exception.StackTrace}");

                if (exception.InnerException != null)
                {
                    sb.AppendLine($"InnerException: {exception.InnerException.Message}");
                }
            }

            EscriureLog("ERROR", sb.ToString());
        }

        public string GetLogFilePath()
        {
            return _logFilePath;
        }

        private void EscriureLog(string nivell, string missatge)
        {
            lock (_lockObject)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var linia = $"[{timestamp}] [{nivell}] {missatge}";

                    using (var writer = new StreamWriter(_logFilePath, append: true, encoding: Encoding.UTF8))
                    {
                        writer.WriteLine(linia);
                    }

                    // També escriure a consola
                    Console.WriteLine(linia);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error escrivint al log: {ex.Message}");
                }
            }
        }
    }
}
