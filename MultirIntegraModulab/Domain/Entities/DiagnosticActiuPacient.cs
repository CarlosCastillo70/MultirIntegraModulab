using System;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Representa un diagnòstic actiu (vigent) d'un pacient amb el darrer positiu associat
    /// </summary>
    public class DiagnosticActiuPacient
    {
        /// <summary>
        /// ID del diagnòstic
        /// </summary>
        public int DiagnosticId { get; set; }

        /// <summary>
        /// Número de pacient (npat)
        /// </summary>
        public string PacientSap { get; set; }

        /// <summary>
        /// Codi del microorganisme
        /// </summary>
        public string Microorganisme { get; set; }

        /// <summary>
        /// Codi del mecanisme de resistència (pot ser null si el positiu és per microorganisme especial)
        /// </summary>
        public string Mecanisme { get; set; }

        /// <summary>
        /// Tipus/descripció del mecanisme
        /// </summary>
        public string TipusMecanisme { get; set; }

        /// <summary>
        /// Data del diagnòstic
        /// </summary>
        public DateTime? DataDiagnostic { get; set; }

        /// <summary>
        /// Data del darrer positiu associat a aquest diagnòstic
        /// </summary>
        public DateTime? DataDarrerPositiu { get; set; }

        /// <summary>
        /// Codi del tipus de mostra del darrer positiu
        /// </summary>
        public string TipusMostra { get; set; }

        /// <summary>
        /// Descripció del tipus de mostra del darrer positiu
        /// </summary>
        public string DescripcioTipusMostra { get; set; }

        /// <summary>
        /// Indica si el mecanisme requereix nota al curs clínic (taula mecanismes.nota_curs_clinic)
        /// </summary>
        public bool? MecanismeNotaCursClinic { get; set; }

        /// <summary>
        /// Indica si el microorganisme requereix nota al curs clínic (taula microorganismes.nota_curs_clinic)
        /// </summary>
        public bool? MicroorganismeNotaCursClinic { get; set; }

        /// <summary>
        /// Retorna una representació textual del diagnòstic actiu
        /// </summary>
        public override string ToString()
        {
            string mecanisme = !string.IsNullOrWhiteSpace(Mecanisme) ? $" + {Mecanisme}" : "";
            string tipusMostra = !string.IsNullOrWhiteSpace(DescripcioTipusMostra) 
                ? DescripcioTipusMostra 
                : TipusMostra;

            return $"Diagnòstic {DiagnosticId}: {Microorganisme}{mecanisme} " +
                   $"(Darrer positiu: {DataDarrerPositiu:dd/MM/yyyy}, Tipus: {tipusMostra})";
        }
    }
}
