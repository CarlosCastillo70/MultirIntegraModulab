using System;

namespace MultirRevisioVigencia.Application.DTOs
{
    /// <summary>
    /// DTO que representa una regla de la taula tipusmostra_referencia
    /// </summary>
    public class ReglaTipusMostra
    {
        public int Id { get; set; }
        public string MicroorganismePatro { get; set; }
        public string MecanismePatro { get; set; }
        public string Resultat { get; set; }
        public int Prioritat { get; set; }
        public bool Actiu { get; set; }
    }
}
