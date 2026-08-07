using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace MultirRevisioVigencia.Infrastructure.Configuration
{
    /// <summary>
    /// Gestiona la càrrega de configuració des d'App.config
    /// </summary>
    public static class ConfiguracioManager
    {
        public static ConfiguracioApp CarregarConfiguracio()
        {
            try
            {
                // Determinar l'entorn (Produccio o Preproduccio)
                string entorn = ConfigurationManager.AppSettings["Entorn"] ?? "Preproduccio";
                bool esProducció = entorn.Equals("Produccio", StringComparison.OrdinalIgnoreCase);

                Console.WriteLine($"=======================================================");
                Console.WriteLine($"  ENTORN: {(esProducció ? "PRODUCCIÓ" : "PREPRODUCCIÓ")}");
                Console.WriteLine($"=======================================================");
                Console.WriteLine();

                // Seleccionar la connexió segons l'entorn
                string nomConnexio = esProducció ? "MySqlMultiR_Produccio" : "MySqlMultiR_Preproduccio";
                string connectionString = ConfigurationManager.ConnectionStrings[nomConnexio]?.ConnectionString;

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    Console.WriteLine($"❌ ERROR: No s'ha trobat la connexió '{nomConnexio}' a App.config");
                    return null;
                }

                // Obtenir suffix d'entorn per al nom del fitxer de log
                string suffixEntorn = esProducció ? "pro" : "pre";

                var config = new ConfiguracioApp
                {
                    Entorn = entorn,
                    EsProducció = esProducció,
                    ConnectionStringMySQL = connectionString,

                    // Logging
                    RutaFitxerLog = ConfigurationManager.AppSettings["RutaFitxerLog"] ?? "Logs\\revigio{0:yyyy-MM-dd_HH-mm-ss}_{1}.log",

                    // Filtratge
                    PacientsAProcessar = ConfigurationManager.AppSettings["PacientsAProcessar"]?
                        .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList() ?? new List<string>(),

                    LimitDiagnosticsAProcessar = int.TryParse(ConfigurationManager.AppSettings["LimitDiagnosticsAProcessar"], out int limit) ? limit : 0,

                    // Email
                    SmtpServer = ConfigurationManager.AppSettings["SmtpServer"],
                    SmtpPort = int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out int port) ? port : 25,
                    SmtpUsuari = ConfigurationManager.AppSettings["SmtpUsuari"],
                    SmtpPassword = ConfigurationManager.AppSettings["SmtpPassword"],
                    UsarSSL = bool.TryParse(ConfigurationManager.AppSettings["UsarSSL"], out bool usarSsl) && usarSsl,
                    EmailFrom = ConfigurationManager.AppSettings["EmailFrom"],
                    EmailsDestinataris = ConfigurationManager.AppSettings["EmailsDestinataris"]?
                        .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .ToList() ?? new List<string>(),

                    // Nova propietat
                    NovaPropietat = ConfigurationManager.AppSettings["NovaPropietat"]
                };

                // Format del fitxer de log amb la data i entorn
                if (config.RutaFitxerLog.Contains("{0") && config.RutaFitxerLog.Contains("{1"))
                {
                    config.RutaFitxerLog = string.Format(config.RutaFitxerLog, DateTime.Now, suffixEntorn);
                }
                else if (config.RutaFitxerLog.Contains("{0"))
                {
                    config.RutaFitxerLog = string.Format(config.RutaFitxerLog, DateTime.Now);
                }

                // Si la ruta és relativa, resoldre-la respecte al directori de l'executable
                if (!Path.IsPathRooted(config.RutaFitxerLog))
                {
                    config.RutaFitxerLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.RutaFitxerLog);
                }

                config.RutaFitxerLog = Path.GetFullPath(config.RutaFitxerLog);

                // Validacions
                if (string.IsNullOrWhiteSpace(config.SmtpServer))
                {
                    Console.WriteLine("⚠️ WARNING: Servidor SMTP no configurat");
                }

                if (!config.EmailsDestinataris.Any())
                {
                    Console.WriteLine("⚠️ WARNING: No hi ha emails destinataris configurats");
                }

                Console.WriteLine($"✅ Configuració carregada correctament");
                Console.WriteLine($"   - Base de dades: {ExtreureDatabaseDeConnectionString(connectionString)}");
                Console.WriteLine($"   - Servidor SMTP: {config.SmtpServer}:{config.SmtpPort}");
                Console.WriteLine($"   - Destinataris: {config.EmailsDestinataris.Count}");
                if (config.PacientsAProcessar != null && config.PacientsAProcessar.Any())
                {
                    Console.WriteLine($"   - FILTRE PACIENTS: {config.PacientsAProcessar.Count} pacient(s) específic(s)");
                }
                if (config.LimitDiagnosticsAProcessar > 0)
                {
                    Console.WriteLine($"   - LÍMIT: {config.LimitDiagnosticsAProcessar} diagnòstic(s) màxim");
                }
                Console.WriteLine();

                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error carregant configuració: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extreu el nom de la base de dades de la cadena de connexió
        /// </summary>
        private static string ExtreureDatabaseDeConnectionString(string connectionString)
        {
            try
            {
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    if (part.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                    {
                        return part.Split('=')[1].Trim();
                    }
                }
                return "desconeguda";
            }
            catch
            {
                return "desconeguda";
            }
        }
    }

    /// <summary>
    /// Classe amb la configuració de l'aplicació
    /// </summary>
    public class ConfiguracioApp
    {
        public string Entorn { get; set; }
        public bool EsProducció { get; set; }
        public string ConnectionStringMySQL { get; set; }
        public string RutaFitxerLog { get; set; }
        public List<string> PacientsAProcessar { get; set; }
        public int LimitDiagnosticsAProcessar { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsuari { get; set; }
        public string SmtpPassword { get; set; }
        public bool UsarSSL { get; set; }
        public string EmailFrom { get; set; }
        public List<string> EmailsDestinataris { get; set; }
        public string NovaPropietat { get; set; } // Nova propietat afegida
    }
}
