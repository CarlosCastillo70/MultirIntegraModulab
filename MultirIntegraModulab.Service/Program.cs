using System;
using System.Diagnostics;
using System.ServiceProcess;
using MultirIntegraModulab.Service.Services;
using Microsoft.Owin.Hosting;
using MultirIntegraModulab;

namespace MultirIntegraModulab.Service
{
    static class Program
    {
        private static IDisposable _webApp;

        static void Main(string[] args)
        {
            try
            {
                EventLog.WriteEntry("MultirIntegraModulabService",
                    "Iniciant servei windows MultiR Integra Modulab Service ...",
                    EventLogEntryType.Information);

                // 1. Instanciem PRIMER el servei (així evitem que Quartz estigui buit)
                var workflowService = new WorkflowService();

                // 2. Engeguem el servei de Windows 
                // Nota: Com que ServiceBase.Run bloqueja el fil principal, 
                // aixequem OWIN exactament un segon després en un fil de fons 
                // per garantir que el motor de Quartz ja ha fet el seu InitializeQuartzAsync.

                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        // Esperem 2 segons a que el servei estigui "Running" i el motor llest
                        await System.Threading.Tasks.Task.Delay(2000);

                        string url = "http://+:9000";
                        _webApp = WebApp.Start<Startup>(url);

                        EventLog.WriteEntry("MultirIntegraModulabService",
                            $"[OK] Panell gràfic de CrystalQuartz actiu a: http://localhost:9000/quartz/",
                            EventLogEntryType.Information);
                    }
                    catch (Exception owinEx)
                    {
                        // Anem a extreure l'error real (InnerException) per saber què xoca
                        var realException = owinEx.InnerException ?? owinEx;
                        EventLog.WriteEntry("MultirIntegraModulabService",
                            $"[ERROR CRÍTIC OWIN] No s'ha pogut iniciar el panell web.\n" +
                            $"Detall: {realException.Message}\nStackTrace: {realException.StackTrace}",
                            EventLogEntryType.Error);
                    }
                });

                // 3. Li passem el control a Windows (Bloqueja aquí fins que s'aturi el servei)
                ServiceBase[] ServicesToRun = new ServiceBase[] { workflowService };
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception ex)
            {
                try
                {
                    EventLog.WriteEntry("MultirIntegraModulabService",
                        $"Error fatal iniciant servei: {ex.Message}\n{ex.StackTrace}",
                        EventLogEntryType.Error);
                }
                catch { }
                throw;
            }
        }
    }
}