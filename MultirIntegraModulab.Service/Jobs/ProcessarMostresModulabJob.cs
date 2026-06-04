using Quartz;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MultirIntegraModulab.Service.Jobs
{
    /// <summary>
    /// Job que executa el processament de mostres de Modulab cada X minuts
    /// </summary>
    // [DisallowConcurrentExecution]
    public class ProcessarMostresModulabJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            DateTime dataInici = DateTime.Now;

            // Recuperem el nom del servei que ens ha passat el llançador
            string logSource = context.JobDetail.JobDataMap.GetString("serviceName") ?? "Application";

            try
            {
                EventLog.WriteEntry(logSource,
                    $"[{dataInici:dd/MM/yyyy HH:mm:ss}] Iniciant processament de mostres Modulab ...",
                    EventLogEntryType.Information);

                // Cridar l'executable de MultirIntegraModulab
                var exePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MultirIntegraModulab.exe");

                if (System.IO.File.Exists(exePath))
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,  // Directori de treball
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    // Gràcies als builders de text moderns gestionem millor la memòria en segon pla
                    var outputBuilder = new System.Text.StringBuilder();
                    var errorBuilder = new System.Text.StringBuilder();

                    // Sospitem del buffer: Buidem el buffer a mesura que es genera (Asynchronous Event-driven)
                    process.OutputDataReceived += (sender, args) => { if (args.Data != null) outputBuilder.AppendLine(args.Data); };
                    process.ErrorDataReceived += (sender, args) => { if (args.Data != null) errorBuilder.AppendLine(args.Data); };

                    // Iniciem el procés i comencem a llegir els fluxos immediatament
                    process.Start();

                    // Comencem a escoltar els fluxos immediatament després del Start
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    int timeoutMs = 30 * 60 * 1000;

                    // Esperem de forma asíncrona real sense bloquejar fils de Quartz
                    bool finished = await Task.Run(() => process.WaitForExit(timeoutMs));

                    if (!finished)
                    {
                        process.Kill();
                        process.WaitForExit();

                        EventLog.WriteEntry(logSource,
                            "TIMEOUT: El processament ha excedit els 30 minuts i s'ha finalitzat forçadament",
                            EventLogEntryType.Warning);
                        return;
                    }

                    string error = errorBuilder.ToString();
                    TimeSpan durada = DateTime.Now - dataInici;

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        EventLog.WriteEntry(logSource,
                            $"STDERR del procés:\n{error.Substring(0, Math.Min(error.Length, 30000))}",
                            EventLogEntryType.Warning);
                    }

                    EventLog.WriteEntry(logSource,
                        $"[{dataInici:dd/MM/yyyy HH:mm:ss}] Processament finalitzat. Durada: {durada.TotalSeconds:F2}s. Exit code: {process.ExitCode}",
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
        }
    }
}
