using System;
using System.IO;
using System.Threading;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Sistema de logging que escriu els missatges a fitxers organitzats per execució
    /// Cada execució crea un fitxer de log independent amb timestamp
    /// </summary>
    public static class Logger
    {
        private static readonly object _lockObject = new object();
        private static readonly string _baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        
        // Timestamp d'inici de l'execució actual (fixat al primer ús)
        private static DateTime? _dataInici = null;
        
        // Ruta del fitxer de log de l'execució actual
        private static string _rutaLogActual = null;

        /// <summary>
        /// Tipus de missatge de log
        /// </summary>
        public enum TipusLog
        {
            INFO,
            ERROR,
            WARNING,
            DEBUG,
            TRACE
        }

        /// <summary>
        /// Assegura que la carpeta de logs existeix
        /// </summary>
        static Logger()
        {
            try
            {
                if (!Directory.Exists(_baseDirectory))
                {
                    Directory.CreateDirectory(_baseDirectory);
                }
            }
            catch (Exception ex)
            {
                // En cas d'error creant la carpeta, usar Console com a fallback
                Console.WriteLine($"ERROR: No s'ha pogut crear la carpeta de logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Inicialitza una nova execució amb un timestamp fix
        /// Aquest mètode s'ha de cridar al principi de cada execució
        /// </summary>
        private static void InicialitzarExecucio()
        {
            if (_dataInici == null)
            {
                lock (_lockObject)
                {
                    if (_dataInici == null)
                    {
                        _dataInici = DateTime.Now;
                        // Format: multir2025-01-27_08-30-15.log
                        string nomFitxer = $"multir{_dataInici.Value:yyyy-MM-dd_HH-mm-ss}.log";
                        _rutaLogActual = Path.Combine(_baseDirectory, nomFitxer);
                    }
                }
            }
        }

        /// <summary>
        /// Escriu un missatge d'informació al log
        /// </summary>
        /// <param name="missatge">Missatge a escriure</param>
        public static void Info(string missatge)
        {
            EscriureMissatge(TipusLog.INFO, missatge);
        }

        /// <summary>
        /// Escriu un missatge d'error al log
        /// </summary>
        /// <param name="missatge">Missatge a escriure</param>
        public static void Error(string missatge)
        {
            EscriureMissatge(TipusLog.ERROR, missatge);
        }

        /// <summary>
        /// Escriu un missatge d'error amb excepció al log
        /// </summary>
        /// <param name="missatge">Missatge a escriure</param>
        /// <param name="ex">Excepció associada</param>
        public static void Error(string missatge, Exception ex)
        {
            string missatgeComplet = $"{missatge} | Exception: {ex.GetType().Name} - {ex.Message}";
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                missatgeComplet += $" | StackTrace: {ex.StackTrace.Replace(Environment.NewLine, " | ")}";
            }
            EscriureMissatge(TipusLog.ERROR, missatgeComplet);
        }

        /// <summary>
        /// Escriu un missatge d'advertència al log
        /// </summary>
        /// <param name="missatge">Missatge a escriure</param>
        public static void Warning(string missatge)
        {
            EscriureMissatge(TipusLog.WARNING, missatge);
        }

        /// <summary>
        /// Escriu un missatge de debug al log
        /// </summary>
        /// <param name="missatge">Missatge a escriure</param>
        public static void Debug(string missatge)
        {
            EscriureMissatge(TipusLog.DEBUG, missatge);
        }

        /// <summary>
        /// Escriu un missatge de trace al log
        /// </summary>
        /// <param name="missatge">Missatge a escriure</param>
        public static void Trace(string missatge)
        {
            EscriureMissatge(TipusLog.TRACE, missatge);
        }

        /// <summary>
        /// Escriu un missatge al fitxer de log de l'execució actual
        /// </summary>
        /// <param name="tipus">Tipus de missatge</param>
        /// <param name="missatge">Missatge a escriure</param>
        private static void EscriureMissatge(TipusLog tipus, string missatge)
        {
            try
            {
                lock (_lockObject)
                {
                    // Assegurar que tenim inicialitzada l'execució
                    InicialitzarExecucio();
                    
                    DateTime ara = DateTime.Now;

                    // Format: data hora minut segon actual tipus : missatge
                    string lineaLog = $"{ara:yyyy-MM-dd HH:mm:ss} {tipus} : {missatge}";

                    // Escriure al fitxer de l'execució actual
                    using (var writer = new StreamWriter(_rutaLogActual, append: true))
                    {
                        writer.WriteLine(lineaLog);
                        writer.Flush();
                    }

                    // També mostrar a la consola per a debug immediat (opcional)
                    #if DEBUG
                    Console.WriteLine($"[{tipus}] {missatge}");
                    #endif
                }
            }
            catch (Exception ex)
            {
                // Si hi ha problemes amb el logging, usar Console com a fallback
                Console.WriteLine($"ERROR LOGGING: {ex.Message} | Missatge original: [{tipus}] {missatge}");
            }
        }

        /// <summary>
        /// Obté la ruta del fitxer de log de l'execució actual
        /// </summary>
        /// <returns>Ruta completa del fitxer de log de l'execució actual</returns>
        public static string ObtenirRutaLogActual()
        {
            InicialitzarExecucio();
            return _rutaLogActual;
        }

        /// <summary>
        /// Comprova si existeix el fitxer de log de l'execució actual
        /// </summary>
        /// <returns>True si existeix el fitxer</returns>
        public static bool ExisteixLogActual()
        {
            return File.Exists(ObtenirRutaLogActual());
        }

        /// <summary>
        /// Obté la mida del fitxer de log de l'execució actual en bytes
        /// </summary>
        /// <returns>Mida en bytes o 0 si no existeix</returns>
        public static long ObtenirMidaLogActual()
        {
            string rutaLog = ObtenirRutaLogActual();
            return File.Exists(rutaLog) ? new FileInfo(rutaLog).Length : 0;
        }

        /// <summary>
        /// Obté la ruta del fitxer de log d'avui (mètode obsolet, mantingut per compatibilitat)
        /// </summary>
        /// <returns>Ruta completa del fitxer de log de l'execució actual</returns>
        [Obsolete("Utilitzeu ObtenirRutaLogActual() en lloc d'aquest mètode")]
        public static string ObtenirRutaLogAvui()
        {
            return ObtenirRutaLogActual();
        }

        /// <summary>
        /// Comprova si existeix el fitxer de log d'avui (mètode obsolet, mantingut per compatibilitat)
        /// </summary>
        /// <returns>True si existeix el fitxer</returns>
        [Obsolete("Utilitzeu ExisteixLogActual() en lloc d'aquest mètode")]
        public static bool ExisteixLogAvui()
        {
            return ExisteixLogActual();
        }

        /// <summary>
        /// Obté la mida del fitxer de log d'avui en bytes (mètode obsolet, mantingut per compatibilitat)
        /// </summary>
        /// <returns>Mida en bytes o 0 si no existeix</returns>
        [Obsolete("Utilitzeu ObtenirMidaLogActual() en lloc d'aquest mètode")]
        public static long ObtenirMidaLogAvui()
        {
            return ObtenirMidaLogActual();
        }

        /// <summary>
        /// Neteja logs antics (més vells que el nombre de dies especificat)
        /// </summary>
        /// <param name="diesAMantenir">Nombre de dies de logs a mantenir</param>
        /// <returns>Nombre de fitxers esborrats</returns>
        public static int NetejaarLogsAntics(int diesAMantenir = 30)
        {
            int fitxersEsborrats = 0;

            try
            {
                if (!Directory.Exists(_baseDirectory))
                    return 0;

                DateTime limitData = DateTime.Now.AddDays(-diesAMantenir);
                var fitxers = Directory.GetFiles(_baseDirectory, "multir*.log");

                foreach (var fitxer in fitxers)
                {
                    var infoFitxer = new FileInfo(fitxer);
                    if (infoFitxer.CreationTime < limitData)
                    {
                        try
                        {
                            File.Delete(fitxer);
                            fitxersEsborrats++;
                            Info($"Log antic esborrat: {infoFitxer.Name}");
                        }
                        catch (Exception ex)
                        {
                            Error($"Error esborrant log antic {infoFitxer.Name}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error("Error durant la neteja de logs antics", ex);
            }

            return fitxersEsborrats;
        }

        /// <summary>
        /// Escriu una línia separadora al log per marcar l'inici d'una nova execució
        /// </summary>
        public static void MarcarIniciExecucio()
        {
            // Forçar inicialització amb el timestamp actual
            InicialitzarExecucio();
            
            string separador = new string('=', 80);
            Info(separador);
            Info($"INICI NOVA EXECUCIÓ - {_dataInici.Value:yyyy-MM-dd HH:mm:ss}");
            Info($"Fitxer de log: {Path.GetFileName(_rutaLogActual)}");
            Info($"Versió de l'aplicació: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            Info(separador);
        }

        /// <summary>
        /// Escriu una línia separadora al log per marcar el final d'una execució
        /// </summary>
        public static void MarcarFinalExecucio()
        {
            string separador = new string('=', 80);
            Info(separador);
            Info($"FINAL EXECUCIÓ - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Info(separador);
            
            // Petit delay per assegurar que el fitxer s'ha tancat completament
            Thread.Sleep(100);
        }

        /// <summary>
        /// Força l'espera per assegurar que tots els logs s'han escrit al disc
        /// Útil abans d'adjuntar el log a un email
        /// </summary>
        public static void FlushLogs()
        {
            lock (_lockObject)
            {
                // Petit delay inicial per assegurar que tots els StreamWriter s'han tancat
                Thread.Sleep(100);
                
                // Forçar recollida de memòria per alliberar tots els StreamWriter pendents
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                // Delay addicional per assegurar que el sistema operatiu ha alliberat el fitxer
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Reinicia el logger per una nova execució (útil per testing o múltiples execucions en un mateix procés)
        /// </summary>
        public static void ReiniciarPerNovaExecucio()
        {
            lock (_lockObject)
            {
                _dataInici = null;
                _rutaLogActual = null;
            }
        }
    }
}