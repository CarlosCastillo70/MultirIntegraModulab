using System.Collections.Generic;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Resultat de la comparació entre dues mostres
    /// </summary>
    public class ResultatComparacioMostres
    {
        public bool HiHaCanvis { get; set; }
        public List<string> CanvisDetectats { get; set; }

        public ResultatComparacioMostres()
        {
            CanvisDetectats = new List<string>();
        }

        public string ObtenirResum()
        {
            if (!HiHaCanvis)
                return "No hi ha canvis";
            
            return $"{CanvisDetectats.Count} canvi(s) detectat(s): {string.Join(", ", CanvisDetectats)}";
        }
    }
}
