using System;

namespace MultirRevisioVigencia.Application.DTOs
{
    /// <summary>
    /// DTO que representa una mostra positiva d'un diagnòstic
    /// </summary>
    public class MostraPositivaDiagnostic
    {
        public int Id { get; set; }
        public string TipusMostraM { get; set; }
        public DateTime DataMostra { get; set; }
    }
}
