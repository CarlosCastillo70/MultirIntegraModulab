using System;
using System.Collections.Generic;

namespace MultirRevisioVigencia.Application.DTOs
{
    /// <summary>
    /// DTO amb el resum de la revisió de vigència
    /// </summary>
    public class ResumRevisioVigenciaDto
    {
        public DateTime DataRevisio { get; set; }
        public int TotalRevisats { get; set; }
        public int MarcatsNoVigents { get; set; }
        public int MarcatsPerExitus { get; set; }
        public int MarcatsPerVigencia { get; set; }
        public int MarcatsPerVigenciaVR { get; set; }
        public int MarcatsPerMostresNegatives { get; set; }
        public int Errors { get; set; }
        public List<DiagnosticMarcat> DiagnosticsMarcats { get; set; }

        public ResumRevisioVigenciaDto()
        {
            DiagnosticsMarcats = new List<DiagnosticMarcat>();
        }
    }

    /// <summary>
    /// Informació d'un diagnòstic marcat com a no vigent
    /// </summary>
    public class DiagnosticMarcat
    {
        public int DiagnosticId { get; set; }
        public string PacientSap { get; set; }
        public string Microorganisme { get; set; }
        public string Mecanisme { get; set; }
        public DateTime? DataUltimaMostra { get; set; }
        public int? DiesVigencia { get; set; }
        public string Motiu { get; set; }
    }
}
