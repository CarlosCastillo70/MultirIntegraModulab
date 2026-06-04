using System;

namespace MultirIntegraModulab.Application.DTOs
{
    /// <summary>
    /// Resultat del processament d'una mostra mixta amb MMR i VR
    /// Conté estadístiques separades per cada tipus de microorganisme
    /// </summary>
    public class ResultatProcessamentMixtMMRVR
    {
        /// <summary>
        /// Indica si el processament ha estat exitós
        /// </summary>
        public bool Exitosa { get; set; }

        /// <summary>
        /// Missatge descriptiu del resultat
        /// </summary>
        public string Missatge { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ESTADÍSTIQUES MULTIRESISTENTS (MMR)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Nombre de resultats MMR positius processats
        /// </summary>
        public int ResultatsMMRPositius { get; set; }

        /// <summary>
        /// Nombre de resultats MMR negatius processats
        /// </summary>
        public int ResultatsMMRNegatius { get; set; }

        /// <summary>
        /// Indica si s'ha afegit algun positiu MMR (per nota curs clínic)
        /// </summary>
        public bool PositiuAfegit { get; set; }

        /// <summary>
        /// Nombre real de positius MMR incorporats (auditories OKP)
        /// </summary>
        public int PositiusMMRIncorporats { get; set; }

        /// <summary>
        /// Nombre real de negatius MMR incorporats (auditories OKN)
        /// </summary>
        public int NegatiusMMRIncorporats { get; set; }

        /// <summary>
        /// Nombre real de negatius MMR contraresta positiu incorporats (auditories OKNCP)
        /// </summary>
        public int NegatiusMMRContrarestaPositiuIncorporats { get; set; }

        // ═══════════════════════════════════════════════════════════
        // ESTADÍSTIQUES VIRUS RESPIRATORIS (VR)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Nombre de resultats VR processats (sempre positius)
        /// </summary>
        public int ResultatsVRProcessats { get; set; }

        /// <summary>
        /// Nombre de diagnòstics VR creats
        /// </summary>
        public int DiagnosticsVRCreats { get; set; }

        /// <summary>
        /// Nombre real de positius VR incorporats (auditories OKVR)
        /// </summary>
        public int PositiusVRIncorporats { get; set; }

        /// <summary>
        /// Constructor per defecte amb valors inicials
        /// </summary>
        public ResultatProcessamentMixtMMRVR()
        {
            Exitosa = false;
            Missatge = string.Empty;
            ResultatsMMRPositius = 0;
            ResultatsMMRNegatius = 0;
            PositiuAfegit = false;
            PositiusMMRIncorporats = 0;
            NegatiusMMRIncorporats = 0;
            NegatiusMMRContrarestaPositiuIncorporats = 0;
            ResultatsVRProcessats = 0;
            DiagnosticsVRCreats = 0;
            PositiusVRIncorporats = 0;
        }

        /// <summary>
        /// Representació en text del resultat
        /// </summary>
        public override string ToString()
        {
            return $"ResultatProcessamentMixtMMRVR [" +
                   $"Exitosa={Exitosa}, " +
                   $"MMR Positius={ResultatsMMRPositius}, " +
                   $"MMR Negatius={ResultatsMMRNegatius}, " +
                   $"VR Processats={ResultatsVRProcessats}, " +
                   $"PositiuAfegit={PositiuAfegit}]";
        }
    }
}
