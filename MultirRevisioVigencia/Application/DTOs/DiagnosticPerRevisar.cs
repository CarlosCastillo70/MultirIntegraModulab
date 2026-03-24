using System;

namespace MultirRevisioVigencia.Application.DTOs
{
    /// <summary>
    /// DTO amb les dades d'un diagnòstic que cal revisar
    /// </summary>
    public class DiagnosticPerRevisar
    {
        public int Id { get; set; }
        public string PacientSap { get; set; }
        public string Microorganisme { get; set; }
        public string Mecanisme { get; set; }
        public DateTime? DataUltimaMostra { get; set; }
        public int? DiesVigencia { get; set; }
        public DateTime? DataExitus { get; set; }
        public DateTime? DataDarrergPositiu { get; set; }
        public int? VigenciaInactiu { get; set; }
        public bool DataDarrergPositiuEsDeDataDiagnostic { get; set; }
    }
}
