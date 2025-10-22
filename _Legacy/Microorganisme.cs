using System;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Representa un microorganisme de la base de dades MySQL
    /// </summary>
    public class Microorganisme
    {
        /// <summary>
        /// Identificador únic del microorganisme
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Codi del microorganisme
        /// </summary>
        public string Codi { get; set; }

        /// <summary>
        /// Descripció del microorganisme
        /// </summary>
        public string Descripcio { get; set; }

        /// <summary>
        /// Data d'eliminació (null si no està eliminat)
        /// </summary>
        public DateTime? DtDelete { get; set; }

        /// <summary>
        /// Indica si el microorganisme està actiu (1 = sí, 0 = no)
        /// </summary>
        public int Actiu { get; set; }

        /// <summary>
        /// Dies de vigència del microorganisme
        /// </summary>
        public int DiesVigencia { get; set; }

        /// <summary>
        /// Indica si el microorganisme és especial
        /// </summary>
        public bool Especial { get; set; }

        /// <summary>
        /// Indica si el microorganisme està eliminat
        /// </summary>
        public bool EstaEliminat => DtDelete.HasValue;

        /// <summary>
        /// Indica si el microorganisme està actiu i no eliminat
        /// </summary>
        public bool EstaDisponible => Actiu == 1 && !EstaEliminat;

        public override string ToString()
        {
            return $"{Codi} - {Descripcio} (Especial: {(Especial ? "Sí" : "No")}, Actiu: {(EstaDisponible ? "Sí" : "No")})";
        }
    }
}