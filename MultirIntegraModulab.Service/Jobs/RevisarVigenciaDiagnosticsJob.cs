using Quartz;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MultirIntegraModulab.Service.Jobs
{
    /// <summary>
    /// Job que executa la revisió de vigència de diagnòstics 1 cop al dia
    /// </summary>
    [DisallowConcurrentExecution]
    public class RevisarVigenciaDiagnosticsJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            DateTime dataInici = DateTime.Now;

            // Recuperem el nom del servei que ens ha passat el llançador
            string logSource = context.JobDetail.JobDataMap.GetString("serviceName") ?? "Application";

            try
            {
                // Escriure al Event Log de Windows
                EventLog.WriteEntry(logSource, 
                    $"[{dataInici:dd/MM/yyyy HH:mm:ss}] Iniciant revisió de vigència de diagnòstics ...", 
                    EventLogEntryType.Information);

                // Cridar l'executable de MultirRevisioVigencia
                var exePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MultirRevisioVigencia.exe");

                if (System.IO.File.Exists(exePath))
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();

                    // Llegir outputs de forma asíncrona per evitar deadlocks
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    // Esperar màxim 30 minuts
                    int timeoutMs = 30 * 60 * 1000;
                    bool finished = await Task.Run(() => process.WaitForExit(timeoutMs));

                    if (!finished)
                    {
                        process.Kill();
                        process.WaitForExit();

                        EventLog.WriteEntry(logSource, 
                            $"TIMEOUT: La revisió ha excedit els 30 minuts i s'ha finalitzat forçadament", 
                            EventLogEntryType.Warning);
                        return;
                    }

                    // Obtenir outputs
                    string output = await outputTask;
                    string error = await errorTask;

                    TimeSpan durada = DateTime.Now - dataInici;

                    // Loggar error si n'hi ha
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        EventLog.WriteEntry(logSource, 
                            $"STDERR del procés:\n{error.Substring(0, Math.Min(error.Length, 30000))}", 
                            EventLogEntryType.Warning);
                    }

                    EventLog.WriteEntry(logSource, 
                        $"Revisió finalitzada. Durada: {durada.TotalSeconds:F2}s. Exit code: {process.ExitCode}", 
                        process.ExitCode == 0 ? EventLogEntryType.Information : EventLogEntryType.Warning);
                }
                else
                {
                    EventLog.WriteEntry(logSource, 
                        $"ERROR: No s'ha trobat l'executable: {exePath}", 
                        EventLogEntryType.Error);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(logSource, 
                    $"ERROR: {ex.Message}\n{ex.StackTrace}", 
                    EventLogEntryType.Error);
            }

            await Task.CompletedTask;
        }
    }
}
