using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                EventLog.WriteEntry(this.ServiceName,
                    "Iniciant workflow servei " + this.ServiceName + " ...",
                    EventLogEntryType.Information);

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
                EventLog.WriteEntry(this.ServiceName, $"ERROR crític a l'OnStart del servei: {ex.Message}", EventLogEntryType.Error);
                throw;
            }
        }

        /// <summary>
        /// Inicialitza el motor de Quartz i programa les tasques sense bloquejar el fil principal de Windows.
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
                _scheduler = await schedulerFactory.GetScheduler().ConfigureAwait(false);

                // 2. Registrar el listener d'errors (Apuntant ara a "Application")
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


                // ====================================================================
                // 🚀 PROVA DEL COTÓ CORREGIDA PER A CONSOLE APPLICATION (.EXE)
                // ====================================================================
                //try
                //{
                //    // En lloc de buscar una DLL a cegues, agafem directament l'executable que s'està executant ara mateix
                //    var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();

                //    // Cerquem el tipus dins del propi domini del procés executant
                //    var typeTest = currentAssembly.GetType("MultirIntegraModulab.Service.Jobs.ProcessarMostresModulabJob");

                //    if (typeTest != null)
                //    {
                //        // Forcem la creació de l'objecte a memòria en aquest fil
                //        var jobInstance = Activator.CreateInstance(typeTest);

                //        // Comprovem si .NET reconeix que implementa la interfície local IJob de Quartz
                //        bool isIJob = jobInstance is Quartz.IJob;

                //        EventLog.WriteEntry("MultirIntegraModulab",
                //            $"[DIAGNOSTIC TEST] Èxit creant instància des de l'EXE principal: {jobInstance.GetType().FullName}. " +
                //            $"Implementa IJob correctament? = {isIJob}",
                //            EventLogEntryType.Information);
                //    }
                //    else
                //    {
                //        EventLog.WriteEntry("MultirIntegraModulab",
                //            $"[DIAGNOSTIC TEST WARNING] No s'ha trobat el tipus 'ProcessarMostresModulab.Service.Jobs.ProcessarMostresModulabJob' dins del propi EXE principal.",
                //            EventLogEntryType.Warning);
                //    }
                //}
                //catch (Exception ex)
                //{
                //    EventLog.WriteEntry("MultirIntegraModulab",
                //        $"[DIAGNOSTIC TEST CRITICAL] L'Activator de l'EXE ha petat a l'arrencada:\n" +
                //        $"Missatge: {ex.Message}\n" +
                //        $"StackTrace: {ex.StackTrace}",
                //        EventLogEntryType.Error);
                //}
                // ====================================================================



                // 6. L'ÚLTIM PAS: Forcem l'arrencada de Quartz
                await _scheduler.Start().ConfigureAwait(false);

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
                // Obtenir el tipus de la classe del Job
                Type type = null;

                // 1. Intentem la càrrega robusta forçant l'Assembly a memòria RAM
                try
                {
                    if (!string.IsNullOrWhiteSpace(wf.Assembly))
                    {
                        // 🚀 MILLORA PER A CONSOLE APPLICATIONS (.EXE) / DLLs
                        // Si el nom de l'assembly coincideix amb el procés actual o acaba en .Service,
                        // mirem primer si el tipus existeix dins del mateix executable executant.
                        var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                        type = currentAssembly.GetType(wf.Type);

                        // Si no s'ha trobat al .exe actual, intentem carregar-lo com a DLL externa (Pla B original)
                        if (type == null)
                        {
                            var loadedAssembly = System.Reflection.Assembly.Load(wf.Assembly);
                            type = loadedAssembly.GetType(wf.Type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Loguegem l'avís al canal global, però deixem que intenti el següent pas
                    EventLog.WriteEntry(this.ServiceName,
                        $"[REFLECTION NOTE] No s'ha pogut carregar l'assembly '{wf.Assembly}' per reflexió directa: {ex.Message}",
                        EventLogEntryType.Warning);
                }

                // 2. PLA B: Si el mètode anterior falla, usem el mètode estàndard per si ja estigués carregat
                if (type == null)
                {
                    type = Type.GetType($"{wf.Type}, {wf.Assembly}");
                }

                // 3. PLA C (Última oportunitat per a projectes Console unificats): 
                // Si encara és null, busquem el tipus net a qualsevol lloc de l'executable corrent
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

                // Log de confirmació de resolució universal
                //EventLog.WriteEntry(this.ServiceName,
                //    $"[RESOLVED] Tipus trobat correctament al context d'execució: {type.FullName}",
                //    EventLogEntryType.Information);


                // Crear el Job
                // 1. FORCEM EL JOB A SER DURABLE
                var job = JobBuilder.Create(type)
                    .WithIdentity($"{wf.WorkflowFile}-job")
                    .UsingJobData("workflowFile", wf.WorkflowFile)
                    .UsingJobData("serviceName", this.ServiceName)
                    .StoreDurably() // Manté el Job al JobStore encara que no tingui triggers actius un mil·lisegon
                    .Build();

                // Afegir paràmetres si n'hi ha
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

                // Crear el trigger amb expressió CRON de forma robusta per a Quartz 3.x
                // 2. AJUSTEM EL TRIGGER AMB LA POLÍTICA DE MISFIRE DE QUARTZ 3.x
                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"{wf.WorkflowFile}-trigger")
                    .ForJob(job)
                    .WithCronSchedule(wf.Cron, x => x
                        .InTimeZone(TimeZoneInfo.Local)
                        // Ignorem desajustos de mil·lisegons a l'arrencada
                        .WithMisfireHandlingInstructionDoNothing())
                    .WithDescription(wf.Description)
                    // 🚀 FORCEM L'INICI UN MINUT ENRERE: Evita el conflicte de mil·lisegons a l'arrencada
                    .StartAt(DateTimeOffset.Now.AddMinutes(-1))
                    .Build();

                // Programar el Job
                _scheduler.ScheduleJob(job, trigger);

                // Verificar que s'ha programat correctament
                var nextFireTime = trigger.GetNextFireTimeUtc();
                var nextFireTimeLocal = nextFireTime.HasValue ?
                    TimeZoneInfo.ConvertTimeFromUtc(nextFireTime.Value.DateTime, TimeZoneInfo.Local) :
                    DateTime.MinValue;

                // Obtenir més fires per verificar el patró
                var nextFireTimes = new List<string>();
                var fireTime = nextFireTime;
                for (int i = 0; i < 3 && fireTime.HasValue; i++)
                {
                    nextFireTimes.Add(TimeZoneInfo.ConvertTimeFromUtc(fireTime.Value.DateTime, TimeZoneInfo.Local).ToString("dd/MM/yyyy HH:mm:ss"));
                    fireTime = trigger.GetFireTimeAfter(fireTime.Value);
                }

                EventLog.WriteEntry(this.ServiceName,
                    $"[Tasca Activada] {wf.Description}\n" +
                    $"• Tipus resolt: {type.FullName}\n" + // 👈 Fussionat aquí!
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
                // Detener el thread fallback
                _stopFallbackThread = true;
                if (_fallbackThread != null && _fallbackThread.IsAlive)
                {
                    _fallbackThread.Join(5000);  // Esperar máximo 5 segundos
                }

                _scheduler?.Shutdown(true).Wait();
                EventLog.WriteEntry(this.ServiceName, 
                    $"Servei {this.ServiceName} aturat correctament", 
                    EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(this.ServiceName, 
                    $"ERROR aturant servei {this.ServiceName}: {ex.Message}", 
                    EventLogEntryType.Error);
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


            int checkCount = 0;
            while (!_stopFallbackThread)
            {
                try
                {
                    Thread.Sleep(30000);  // Esperar 30 segundos
                    checkCount++;

                    //EventLog.WriteEntry(this.ServiceName, 
                    //    $"[FALLBACK CHECK #{checkCount}] Verificando triggers a las {DateTime.Now:HH:mm:ss}", 
                    //    EventLogEntryType.Information);

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
        /// Fallback: Verifica de forma ultra-precisa si una tasca s'ha quedat enrere en el temps real
        /// i l'executa només si Quartz ha perdut el seu torn (misfire silent).
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

                // Filtrar i processar només triggers reals (evitem els volàtils creats a mà per codi que comencen per MT_)
                foreach (var triggerKey in triggerKeys)
                {
                    if (triggerKey.Name.StartsWith("MT_"))
                        continue;

                    var trigger = _scheduler.GetTrigger(triggerKey).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (trigger == null)
                        continue;

                    // Anem a buscar quan s'HAURIA d'haver executat de forma immediata anterior (o la teòrica actual)
                    var nextFireTimeUtc = trigger.GetNextFireTimeUtc();
                    if (!nextFireTimeUtc.HasValue)
                        continue;

                    var nextFireLocal = TimeZoneInfo.ConvertTimeFromUtc(nextFireTimeUtc.Value.DateTime, TimeZoneInfo.Local);

                    // Calculem el retard en segons de manera lineal: si és positiu, la tasca és FUTURA. Si és negatiu, és PASSADA.
                    var segonsDeRetard = (now - nextFireLocal).TotalSeconds;

                    // LOG DE DIAGNÒSTIC INTEGRAT
                    // Ens serveix per auditar el comportament real al visor d'esdeveniments
                    //EventLog.WriteEntry(this.ServiceName,
                    //    $"[FALLBACK EVALUATION] Trigger: '{triggerKey.Name}' | Pròxima execució oficial: {nextFireLocal:dd/MM/yyyy HH:mm:ss} | Balanç: {segonsDeRetard:F0}s (enrere)",
                    //    EventLogEntryType.Information);

                    // =========================================================================
                    // CRITERI EXACTE DE DISPAR: 
                    // Si estem EXACTAMENT en el minut de l'execució o la tasca porta un retard de 
                    // fins a 45 segons passats de l'hora teòrica, actua com a xarxa de seguretat.
                    // =========================================================================
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

    public class QuartzErrorListener : IJobListener
    {
        private readonly string _logSource;

        public string Name => "GlobalQuartzErrorListener";

        // 🚀 CONSTRUCTOR: Reben el 'this.ServiceName' des del llançador del servei
        public QuartzErrorListener(string logSource)
        {
            // Si per algun motiu vingués buit, ens defensem apuntant al canal genèric
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
                    // ✅ Ús de l'origen dinàmic del servei sense el text fix antic
                    EventLog.WriteEntry(_logSource,
                        $"[LISTENER ERROR] El Job '{context.JobDetail.Key}' ha llançat una excepció durant l'execució:\n" +
                        $"Message: {jobException.Message}\n" +
                        $"InnerException: {jobException.InnerException?.Message}\n" +
                        $"StackTrace: {jobException.StackTrace}",
                        EventLogEntryType.Error);
                }
                //else
                //{
                //    // ✅ ÈXIT: També unificat amb el nom del servei corrent
                //    EventLog.WriteEntry(_logSource,
                //        $"[LISTENER OK] El motor de Quartz confirma que '{context.JobDetail.Key}' s'ha executat amb èxit (Sense excepcions).",
                //        EventLogEntryType.Information);
                //}
            }
            catch (Exception ex)
            {
                // Si el propi EventLog peta, com a mínim ho intentem escriure per consola/traça bàsica
                System.Diagnostics.Trace.WriteLine($"Error crític al Listener de logs: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }

    public class NullLogProvider : Quartz.Logging.ILogProvider
    {
        public Quartz.Logging.Logger GetLogger(string name)
        {
            // Retornem un delegat que compleix amb la firma de Quartz i que ignora els logs
            return (level, func, exception, parameters) => false;
        }

        public IDisposable OpenNestedContext(string message)
        {
            return null;
        }

        public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
        {
            return null;
        }
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
                EventLog.WriteEntry(_serviceName,
                    $"[FACTORY] Intentant instanciar el Job: '{bundle.JobDetail.Key}' per al Trigger: '{bundle.Trigger.Key}'",
                    EventLogEntryType.Information);

                IJob job = base.NewJob(bundle, scheduler);

                EventLog.WriteEntry(_serviceName,
                    $"[FACTORY] Job '{bundle.JobDetail.Key}' instanciat amb èxit. Tipus real: {job.GetType().FullName}",
                    EventLogEntryType.Information);

                return job;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(_serviceName,
                    $"[FACTORY CRITICAL ERROR] No s'ha pogut instanciar el Job '{bundle.JobDetail.Key}'. " +
                    $"Error: {ex.Message}\n{ex.StackTrace}",
                    EventLogEntryType.Error);
                throw;
            }
        }
    }


}
