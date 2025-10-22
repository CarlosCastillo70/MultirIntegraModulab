using System;

namespace MultirIntegraModulab.Application.DTOs
{
    /// <summary>
    /// DTO per representar una mostra amb informació simplificada
    /// </summary>
    public class MostraDto
    {
        /// <summary>
        /// Identificador de l'etiqueta
        /// </summary>
        public string EtiquetaId { get; set; }

        /// <summary>
        /// Identificador del pacient
        /// </summary>
        public string PacientSap { get; set; }

        /// <summary>
        /// CIP del pacient
        /// </summary>
        public string Cip { get; set; }

        /// <summary>
        /// Data del resultat
        /// </summary>
        public DateTime DataResultat { get; set; }

        /// <summary>
        /// Indica si està validada
        /// </summary>
        public bool EstaValidada { get; set; }

        /// <summary>
        /// Microorganisme principal
        /// </summary>
        public string Microorganisme { get; set; }

        /// <summary>
        /// Tipus de prova
        /// </summary>
        public string TipusProva { get; set; }

        /// <summary>
        /// Nombre de registres associats
        /// </summary>
        public int NombreRegistres { get; set; }
    }
}
