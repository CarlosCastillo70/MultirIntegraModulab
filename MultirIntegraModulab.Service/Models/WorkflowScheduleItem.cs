using System.Collections.Generic;

namespace MultirIntegraModulab.Service.Models
{
    /// <summary>
    /// Defineix la configuració d'una tasca programada
    /// </summary>
    public class WorkflowScheduleItem
    {
        /// <summary>
        /// Nom descriptiu del workflow (per logging)
        /// </summary>
        public string WorkflowFile { get; set; }

        /// <summary>
        /// Expressió CRON per programar l'execució
        /// Format: segons minuts hores dia_mes mes dia_setmana
        /// Exemples:
        /// - "0 0/15 * * * ?" = cada 15 minuts
        /// - "0 0 4 * * ?" = cada dia a les 4:00 AM
        /// </summary>
        public string Cron { get; set; }

        /// <summary>
        /// Descripció llegible de la tasca
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Si és true, s'executa immediatament en iniciar el servei
        /// </summary>
        public bool RunOnStartup { get; set; }

        /// <summary>
        /// Assembly on es troba la classe del Job (ex: "MultirIntegraModulab.Service")
        /// </summary>
        public string Assembly { get; set; }

        /// <summary>
        /// Nom complet de la classe que implementa IJob
        /// Format: Namespace.ClassName
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Paràmetres opcionals per passar al Job
        /// </summary>
        public Dictionary<string, WorkflowParameter> Parameters { get; set; }
    }
}
