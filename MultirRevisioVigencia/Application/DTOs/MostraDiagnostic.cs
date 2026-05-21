using System;

namespace MultirRevisioVigencia.Application.DTOs
{
    /// <summary>
    /// DTO que representa una mostra (positiva o negativa) posterior a la data de diagnòstic
    /// </summary>
    public class MostraDiagnostic
    {
        public int Id { get; set; }
        public string TipusMostraM { get; set; }
        public DateTime DataMostra { get; set; }
        public string Valoracio { get; set; }  // '2' = Positiu, altres = Negatiu

        public bool EsPositiva => Valoracio == "2";
    }
}
