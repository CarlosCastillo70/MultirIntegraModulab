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
        public string Valoracio { get; set; }  // '1' = Negatiu, '2' = Positiu, altres valors no computen

        public bool EsNegativa => Valoracio == "1";
        public bool EsPositiva => Valoracio == "2";
    }
}
