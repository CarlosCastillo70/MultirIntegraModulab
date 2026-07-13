using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using MultirIntegraModulab.Service.Models;

namespace MultirIntegraModulab.Service.Services
{
    /// <summary>
    /// Servei de Windows que gestiona l'execució programada de tasques amb Quartz.NET
    /// </summary>
    public partial class WorkflowService : ServiceBase
    {
        private IScheduler _scheduler;
        private Thread _fallbackThread;  // ✅ Thread background para monitoreo de triggers
        private volatile bool _stopFallbackThread = false;

        /// <summary>
        /// Propietat pública per permetre a l'EXE principal enllaçar el motor amb CrystalQuartz de forma segura.
        /// </summary>
        public IScheduler Scheduler => _scheduler;

        public WorkflowService()
        {
            this.ServiceName = "MultirIntegraModulabService";
            this.CanStop = true;
            this.CanPauseAndContinue = false;
            this.AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                SafeWriteEventLog($"Iniciant workflow servei {this.ServiceName} ...", EventLogEntryType.Information);

                // ✅ FILTRE: Llançem la inicialització en segon pla i alliberem l'OnStart immediatament.
                // Windows veurà que el servei arrenca a l'instant sense bloquejos de fils.
                InitializeQuartzAsync();

                // Netejar i arrencar fil de Fallback
                _stopFallbackThread = false;
                _fallbackThread = new Thread(() => FallbackMonitorThread())
                {
                    IsBackground = true,
                    Name = "QuartzFallbackMonitor"
                };
                _fallbackThread.Start();
            }
            catch (Exception ex)
            {
                SafeWriteEventLog($"ERROR crític a l'OnStart del servei: {ex}", EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Inicialitza el motor de Quartz i programa les tasques sota el context del fil principal del servei.
        /// </summary>
        private async void InitializeQuartzAsync()
        {
            try
            {
                // 1. Forcem el proveïdor de logs buit per evitar el conflicte amb Serilog
                Quartz.Logging.LogProvider.SetCurrentLogProvider(new NullLogProvider());

                // Propietats de configuració base per a Quartz 3.x
                var properties = new System.Collections.Specialized.NameValueCollection
                {
                    { "quartz.serializer.type", "json" },
                    { "quartz.jobStore.type", "Quartz.Simpl.RAMJobStore, Quartz" },
                    { "quartz.jobStore.misfireThreshold", "300000" },
                    { "quartz.threadPool.threadCount", "5" }
                };

                var schedulerFactory = new StdSchedulerFactory(properties);

                // 🚀 ELIMINAT ConfigureAwait(false): Mantenim el fil de context del servei de Windows
                _scheduler = await schedulerFactory.GetScheduler();

                // 2. Registrar el listener d'errors amb el ServiceName dinàmic
                _scheduler.ListenerManager.AddJobListener(new QuartzErrorListener(this.ServiceName));

                // 3. Llegir i carregar la configuració del JSON
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "workflow-schedule.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var workflows = JsonConvert.DeserializeObject<List<WorkflowScheduleItem>>(json);

                    // 4. Programem primer TOTS els Triggers a la cua de Quartz
                    foreach (var wf in workflows)
                    {
                        ScheduleWorkflow(wf);
                    }

                    // 5. Executem les tasques d'arrencada inicial en paral·lel
                    foreach (var wf in workflows)
                    {
                        if (wf.RunOnStartup)
                        {
                            _ = System.Threading.Tasks.Task.Run(() => ExecuteWorkflowOnStartup(wf));
                        }
                    }
                }

                // 6. L'ÚLTIM PAS: Forcem l'arrencada de Quartz
                await _scheduler.Start();

                // Afegim l'estat real al canal global per confirmar l'obertura de comportes
                EventLog.WriteEntry(this.ServiceName,
                    $"Motor de Quartz iniciat i operatiu correctament. Started={_scheduler.IsStarted}, InStandby={_scheduler.InStandbyMode}",
                    EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(this.ServiceName,
                    $"[INITIALIZATION ERROR] No s'ha pogut configurar Quartz: {ex.Message}\n{ex.StackTrace}",
                    EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Programa una tasca amb Quartz
        /// </summary>
        private void ScheduleWorkflow(WorkflowScheduleItem wf)
        {
            try
            {
                Type type = null;

                // 1. Intentem la càrrega robusta forçant l'Assembly a memòria RAM
                try
                {
                    if (!string.IsNullOrWhiteSpace(wf.Assembly))
                    {
                        var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                        type = currentAssembly.GetType(wf.Type);

                        if (type == null)
                        {
                            var loadedAssembly = System.Reflection.Assembly.Load(wf.Assembly);
                            type = loadedAssembly.GetType(wf.Type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry(this.ServiceName,
                        $"[REFLECTION NOTE] No s'ha pogut carregar l'assembly '{wf.Assembly}' per reflexió directa: {ex.Message}",
                        EventLogEntryType.Warning);
                }

                // 2. PLA B: Si el mètode anterior falla
                if (type == null)
                {
                    type = Type.GetType($"{wf.Type}, {wf.Assembly}");
                }

                // 3. PLA C
                if (type == null)
                {
                    type = System.Reflection.Assembly.GetExecutingAssembly().GetType(wf.Type);
                }

                // 4. Validació de seguretat final
                if (type == null)
                {
                    EventLog.WriteEntry(this.ServiceName,
                        $"CRITICAL ERROR: No s'ha pogut resoldre el tipus del Job: '{wf.Type}' a l'assembly '{wf.Assembly}'. Verifica que el nom del Job i del Namespace siguin exactes al teu JSON.",
                        EventLogEntryType.Error);
                    return;
                }

                // Crear el Job durable
                var job = JobBuilder.Create(type)
                    .WithIdentity($"{wf.WorkflowFile}-job")
                    .UsingJobData("workflowFile", wf.WorkflowFile)
                    .UsingJobData("serviceName", this.ServiceName)
                    .StoreDurably()
                    .Build();

                if (wf.Parameters != null)
                {
                    foreach (var kvp in wf.Parameters)
                    {
                        if (kvp.Value?.Expression != null)
                        {
                            job.JobDataMap[kvp.Key] = kvp.Value.Expression;
                        }
                    }
                }

                // Crear el trigger amb la política de misfire de Quartz 3.x
                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"{wf.WorkflowFile}-trigger")
                    .ForJob(job)
                    .WithCronSchedule(wf.Cron, x => x
                        .InTimeZone(TimeZoneInfo.Local)
                        .WithMisfireHandlingInstructionDoNothing())
                    .WithDescription(wf.Description)
                    .StartAt(DateTimeOffset.Now.AddMinutes(-1))
                    .Build();

                _scheduler.ScheduleJob(job, trigger);

                var nextFireTime = trigger.GetNextFireTimeUtc();
                var nextFireTimeLocal = nextFireTime.HasValue ?
                    TimeZoneInfo.ConvertTimeFromUtc(nextFireTime.Value.DateTime, TimeZoneInfo.Local) :
                    DateTime.MinValue;

                var nextFireTimes = new List<string>();
                var fireTime = nextFireTime;
                for (int i = 0; i < 3 && fireTime.HasValue; i++)
                {
                    nextFireTimes.Add(TimeZoneInfo.ConvertTimeFromUtc(fireTime.Value.DateTime, TimeZoneInfo.Local).ToString("dd/MM/yyyy HH:mm:ss"));
                    fireTime = trigger.GetFireTimeAfter(fireTime.Value);
                }

                EventLog.WriteEntry(this.ServiceName,
                    $"[Tasca Activada] {wf.Description}\n" +
                    $"• Tipus resolt: {type.FullName}\n" +
                    $"• Patró CRON: {wf.Cron}\n" +
                    $"• Propera execució: {nextFireTimeLocal:dd/MM/yyyy HH:mm:ss}\n" +
                    $"• Següents cicles: {string.Join(" | ", nextFireTimes)}",
                    EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(this.ServiceName,
                    $"ERROR programant tasca {wf.WorkflowFile}: {ex.Message}\n{ex.StackTrace}",
                    EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Executa una tasca immediatament en iniciar el servei
        /// </summary>
        private void ExecuteWorkflowOnStartup(WorkflowScheduleItem wf)
        {
            try
            {
                var type = Type.GetType($"{wf.Type}, {wf.Assembly}");
                if (type != null && typeof(IJob).IsAssignableFrom(type))
                {
                    var jobInstance = (IJob)Activator.CreateInstance(type);
                    var context = new JobExecutionContextMock(wf.WorkflowFile, wf.Parameters);

                    EventLog.WriteEntry(this.ServiceName,
                        $"Executant tasca a l'inici (runOnStartup = true): {wf.Description}",
                        EventLogEntryType.Information);

                    jobInstance.Execute(context).Wait();
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(this.ServiceName,
                    $"ERROR executant tasca a l'inici {wf.WorkflowFile}: {ex.Message}",
                    EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            try
            {
                RequestAdditionalTime(15000);

                // Detener el thread fallback
                _stopFallbackThread = true;
                if (_fallbackThread != null && _fallbackThread.IsAlive && !_fallbackThread.Join(3000))
                {
                    SafeWriteEventLog("Timeout aturant el thread de fallback. Es continua amb l'aturada del servei.", EventLogEntryType.Warning);
                }

                // Tancat net del motor de Quartz sense bloquejar el Service Control Manager
                if (_scheduler != null)
                {
                    try
                    {
                        var shutdownTask = _scheduler.Shutdown(waitForJobsToComplete: false);
                        if (!shutdownTask.Wait(TimeSpan.FromSeconds(8)))
                        {
                            SafeWriteEventLog("Timeout aturant Quartz. Es finalitza l'OnStop sense bloquejar el SCM.", EventLogEntryType.Warning);
                        }
                    }
                    catch (Exception schEx)
                    {
                        SafeWriteEventLog($"Error aturant Quartz: {schEx.Message}", EventLogEntryType.Warning);
                    }
                    finally
                    {
                        _scheduler = null;
                    }
                }

                SafeWriteEventLog($"Servei {this.ServiceName} aturat correctament", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                SafeWriteEventLog($"ERROR aturant servei {this.ServiceName}: {ex}", EventLogEntryType.Error);
            }
        }

        private void SafeWriteEventLog(string message, EventLogEntryType entryType)
        {
            try
            {
                EventLog.WriteEntry(this.ServiceName, message, entryType);
            }
            catch
            {
                try
                {
                    EventLog.WriteEntry("Application", $"[{this.ServiceName}] {message}", entryType);
                }
                catch
                {
                    // Evitem excepcions d'EventLog en cicle de vida del servei
                }
            }
        }

        /// <summary>
        /// Thread background que monitoreja els triggers cada 30 segons
        /// </summary>
        private void FallbackMonitorThread()
        {
            EventLog.WriteEntry(this.ServiceName,
                "Fallback monitor thread inicia bucle de monitoreo",
                EventLogEntryType.Information);

            while (!_stopFallbackThread)
            {
                try
                {
                    Thread.Sleep(30000);
                    CheckAndExecuteTriggers();
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry(this.ServiceName,
                        $"[FALLBACK MONITOR ERROR] {ex.Message}",
                        EventLogEntryType.Warning);
                }
            }

            EventLog.WriteEntry(this.ServiceName,
                "Fallback monitor thread finalizado",
                EventLogEntryType.Information);
        }

        /// <summary>
        /// Fallback: actua com a xarxa de seguretat si el motor perd un cicle
        /// </summary>
        private void CheckAndExecuteTriggers()
        {
            try
            {
                if (_scheduler == null || !_scheduler.IsStarted)
                {
                    return;
                }

                var triggerKeys = _scheduler.GetTriggerKeys(null).ConfigureAwait(false).GetAwaiter().GetResult();
                var now = DateTime.Now;

                foreach (var triggerKey in triggerKeys)
                {
                    if (triggerKey.Name.StartsWith("MT_"))
                        continue;

                    var trigger = _scheduler.GetTrigger(triggerKey).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (trigger == null)
                        continue;

                    var nextFireTimeUtc = trigger.GetNextFireTimeUtc();
                    if (!nextFireTimeUtc.HasValue)
                        continue;

                    var nextFireLocal = TimeZoneInfo.ConvertTimeFromUtc(nextFireTimeUtc.Value.DateTime, TimeZoneInfo.Local);
                    var segonsDeRetard = (now - nextFireLocal).TotalSeconds;

                    if (segonsDeRetard >= 0 && segonsDeRetard <= 45)
                    {
                        var jobKey = trigger.JobKey;

                        EventLog.WriteEntry(this.ServiceName,
                            $"[FALLBACK ACTIVAT] El motor principal no ha llançat el Job '{jobKey.Name}' a temps. Retard actual: {segonsDeRetard:F0}s. Executant contingència manual...",
                            EventLogEntryType.Warning);

                        try
                        {
                            _scheduler.TriggerJob(jobKey).ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                        catch (Exception jobEx)
                        {
                            EventLog.WriteEntry(this.ServiceName,
                                $"[FALLBACK ERROR] Error crític forçant el job {jobKey.Name}: {jobEx.Message}",
                                EventLogEntryType.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(this.ServiceName, $"[FALLBACK CRITICAL] Error general: {ex.Message}", EventLogEntryType.Warning);
            }
        }
    }

    /* ========================================================================
       CLASSES DE SUPORT I LISTENERS DE QUARTZ
       ======================================================================== */

    public class QuartzErrorListener : IJobListener
    {
        private readonly string _logSource;
        public string Name => "GlobalQuartzErrorListener";

        public QuartzErrorListener(string logSource)
        {
            _logSource = string.IsNullOrWhiteSpace(logSource) ? "Application" : logSource;
        }

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            try
            {
                if (jobException != null)
                {
                    EventLog.WriteEntry(_logSource,
                        $"[LISTENER ERROR] El Job '{context.JobDetail.Key}' ha llançat una excepció durant l'execució:\n" +
                        $"Message: {jobException.Message}\n" +
                        $"InnerException: {jobException.InnerException?.Message}\n" +
                        $"StackTrace: {jobException.StackTrace}",
                        EventLogEntryType.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error crític al Listener de logs: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }

    public class NullLogProvider : Quartz.Logging.ILogProvider
    {
        public Quartz.Logging.Logger GetLogger(string name) => (level, func, exception, parameters) => false;
        public IDisposable OpenNestedContext(string message) => null;
        public IDisposable OpenMappedContext(string key, object value, bool destructure = false) => null;
    }

    public class AuditedJobFactory : Quartz.Simpl.SimpleJobFactory
    {
        private readonly string _serviceName;

        public AuditedJobFactory(string serviceName)
        {
            _serviceName = serviceName;
        }

        public override IJob NewJob(Quartz.Spi.TriggerFiredBundle bundle, IScheduler scheduler)
        {
            try
            {
                EventLog.WriteEntry(_serviceName, $"[FACTORY] Intentant instanciar el Job: '{bundle.JobDetail.Key}'", EventLogEntryType.Information);
                IJob job = base.NewJob(bundle, scheduler);
                EventLog.WriteEntry(_serviceName, $"[FACTORY] Job '{bundle.JobDetail.Key}' instanciat amb èxit.", EventLogEntryType.Information);
                return job;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(_serviceName, $"[FACTORY CRITICAL ERROR] No s'ha pogut instanciar el Job '{bundle.JobDetail.Key}': {ex.Message}", EventLogEntryType.Error);
                throw;
            }
        }
    }
}