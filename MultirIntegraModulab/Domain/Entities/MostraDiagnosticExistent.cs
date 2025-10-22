using System;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Representa una mostra diagnòstic existent a la base de dades
    /// </summary>
    public class MostraDiagnosticExistent
    {
        public int Id { get; set; }
        public string PacientSap { get; set; }
        public DateTime? DataMostra { get; set; }
        public string TipusMostra { get; set; }
        public string TipusProva { get; set; }
        public string Etiqueta { get; set; }
        public DateTime? DataResultat { get; set; }
        public DateTime? DataValidacio { get; set; }
        public string Valoracio { get; set; }
        public string EstatIntegracio { get; set; }
        public DateTime? DataCreacio { get; set; }
        public DateTime? DataActualitzacio { get; set; }

        public override string ToString()
        {
            return $"Mostra {Etiqueta} - Pacient {PacientSap} - Data {DataMostra:dd/MM/yyyy}";
        }
    }
}
