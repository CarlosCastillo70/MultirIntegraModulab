using System;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Informació d'un diagnòstic de pacient
    /// </summary>
    public class DiagnosticInfo
    {
        /// <summary>
        /// ID del diagnòstic
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador del pacient
        /// </summary>
        public string PacientSap { get; set; }

        /// <summary>
        /// Codi del microorganisme
        /// </summary>
        public string MicroorganismeCodi { get; set; }

        /// <summary>
        /// ID del mecanisme de resistència
        /// </summary>
        public string MecanismeId { get; set; }

        /// <summary>
        /// Descripció del mecanisme de resistència
        /// </summary>
        public string MecanismeDescrip { get; set; }

        /// <summary>
        /// Data del diagnòstic
        /// </summary>
        public DateTime? DataDiagnostic { get; set; }

        /// <summary>
        /// Indica si és un diagnòstic positiu (té mecanisme o és microorganisme especial)
        /// </summary>
        public bool EsPositiu { get; set; }

        public override string ToString()
        {
            return $"Diagnòstic #{Id}: {MicroorganismeCodi} [{MecanismeId}]";
        }
    }
}