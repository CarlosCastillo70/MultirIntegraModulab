using System;
using System.Diagnostics;
using System.ServiceProcess;
using MultirIntegraModulab.Service.Services;

namespace MultirIntegraModulab.Service
{
    /// <summary>
    /// Programa principal del Windows Service
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Punt d'entrada principal de l'aplicació
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                // Escriure al Event Log
                EventLog.WriteEntry("MultirIntegraModulabService",
                    "Iniciant servei windows MultiR Integra Modulab Service ...", 
                    EventLogEntryType.Information);

                // Iniciar el servei
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new WorkflowService()
                };
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
                catch
                {
                    // Si no es pot escriure al Event Log, ignorar
                }
                throw;
            }
        }
    }
}
