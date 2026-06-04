using System;
using System.Text;

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
        /// Nombre de positius realment incorporats
        /// </summary>
        public int PositiusIncorporats { get; set; }

        /// <summary>
        /// Nombre de mostres negatives processades
        /// </summary>
        public int MostresNegatives { get; set; }

        /// <summary>
        /// Nombre de negatius realment incorporats
        /// </summary>
        public int NegatiusIncorporats { get; set; }

        /// <summary>
        /// Nombre d'auditories OKVR (Positius virus respiratoris)
        /// </summary>
        public int PositiusVirusRespiratorisIncorporats { get; set; }

        /// <summary>
        /// Nombre d'auditories OKNCP (Negatius contraresta positiu)
        /// </summary>
        public int NegatiusContrarestaPositiuIncorporats { get; set; }

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
            var sb = new StringBuilder();
            
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("  RESUM FINAL DEL PROCESSAMENT");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            
            // Total mostres processades
            sb.AppendLine($"📊 TOTAL MOSTRES PROCESSADES: {TotalProcessats}");
            sb.AppendLine();
            
            // Tipus d'incorporació
            sb.AppendLine("┌─────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│  SEGONS TIPUS D'INCORPORACIÓ                                │");
            sb.AppendLine("└─────────────────────────────────────────────────────────────┘");
            sb.AppendLine($"   🆕 Noves incorporacions  : {NovesIncorporacions,6}");
            sb.AppendLine($"   ✅ Validades             : {MostresValidades,6}");
            sb.AppendLine($"   ⬇️ Desvalidades          : {MostresDesvalidades,6}");
            sb.AppendLine($"   🔄 Revalidades           : {MostresRevalidades,6}");
            sb.AppendLine($"   🔁 Repetides             : {MostresRepetides,6}");
            sb.AppendLine($"   🕐 Antigues              : {MostresAntigues,6}");
            sb.AppendLine();
            
            // Tipus de resultat
            sb.AppendLine("┌─────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│  SEGONS TIPUS DE RESULTAT                                   │");
            sb.AppendLine("└─────────────────────────────────────────────────────────────┘");
            sb.AppendLine($"   ⚡ Positives             : {MostresPositives,6}");
            sb.AppendLine($"   🔵 Negatives             : {MostresNegatives,6}");
            sb.AppendLine($"   🟢🔵 Mixtes              : {MostresMixtes,6}");
            sb.AppendLine();
            
            // Errors i durada
            sb.AppendLine("┌─────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│  ALTRES DADES                                               │");
            sb.AppendLine("└─────────────────────────────────────────────────────────────┘");
            sb.AppendLine($"   ❌ Errors                : {MostresAmbError,6}");
            sb.AppendLine($"   ⏱️ Durada                : {DuradaProcessament.TotalSeconds:F2}s");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            
            return sb.ToString();
        }
    }
}
