using System;

namespace MultirIntegraModulab.Application.DTOs
{
    /// <summary>
    /// DTO per representar el resum d'un processament de mostres
    /// </summary>
    public class ResumProcessamentDto
    {
        /// <summary>
        /// Nombre total de mostres processades
        /// </summary>
        public int TotalProcessats { get; set; }

        /// <summary>
        /// Nombre de noves incorporacions
        /// </summary>
        public int NovesIncorporacions { get; set; }

        /// <summary>
        /// Nombre antigues
        /// </summary>
        public int MostresAntigues { get; set; }

        /// <summary>
        /// Nombre de mostres repetides
        /// </summary>
        public int MostresRepetides { get; set; }

        /// <summary>
        /// Nombre de mostres desvalidades
        /// </summary>
        public int MostresDesvalidades { get; set; }

        /// <summary>
        /// Nombre de mostres revalidades
        /// </summary>
        public int MostresRevalidades { get; set; }

        /// <summary>
        /// Nombre de mostres validades
        /// </summary>
        public int MostresValidades { get; set; }

        /// <summary>
        /// Nombre de mostres amb error
        /// </summary>
        public int MostresAmbError { get; set; }

        /// <summary>
        /// Nombre de mostres positives processades
        /// </summary>
        public int MostresPositives { get; set; }

        /// <summary>
        /// Nombre de mostres negatives processades
        /// </summary>
        public int MostresNegatives { get; set; }

        /// <summary>
        /// Nombre de mostres mixtes processades (amb resultats positius i negatius)
        /// </summary>
        public int MostresMixtes { get; set; }

        /// <summary>
        /// Temps d'inici del processament
        /// </summary>
        public DateTime DataIniciProcessament { get; set; }

        /// <summary>
        /// Temps de finalització del processament
        /// </summary>
        public DateTime DataFiProcessament { get; set; }

        /// <summary>
        /// Durada total del processament
        /// </summary>
        public TimeSpan DuradaProcessament => DataFiProcessament - DataIniciProcessament;

        /// <summary>
        /// Missatges d'error o avisos
        /// </summary>
        public string[] Missatges { get; set; }

        public ResumProcessamentDto()
        {
            Missatges = new string[0];
            DataIniciProcessament = DateTime.Now;
        }

        public override string ToString()
        {
            return $"S'han processat: {TotalProcessats} mostres | " +
                   $"Noves -> {NovesIncorporacions} | " +
                   $"Validades -> {MostresValidades} | " +
                   $"Desvalidades -> {MostresDesvalidades} | " +
                   $"Revalidades -> {MostresRevalidades} | " +
                   $"Repetides -> {MostresRepetides} | " +
                   $"Antigues -> {MostresAntigues} ||| " +
                   $"Positives -> {MostresPositives} | " +
                   $"Negatives -> {MostresNegatives} | " +
                   $"Mixtes -> {MostresMixtes} | " +
                   $"Errors -> {MostresAmbError} | " +
                   $"Durada : {DuradaProcessament.TotalSeconds:F2}s";
        }
    }
}
