using System;

namespace MultirIntegraModulab.Application.Helpers
{
    /// <summary>
    /// Helper per gestionar la indentació jeràrquica dels logs
    /// Facilita la lectura dels logs aplicant nivells d'indentació consistents
    /// </summary>
    public static class LogIndentHelper
    {
        private const int ESPAIS_PER_NIVELL = 2;

        /// <summary>
        /// Genera una cadena d'espais segons el nivell d'indentació
        /// </summary>
        /// <param name="nivell">Nivell d'indentació (0 = cap, 1 = 2 espais, 2 = 4 espais, etc.)</param>
        /// <returns>Cadena amb els espais corresponents</returns>
        public static string Indent(int nivell)
        {
            if (nivell <= 0) return string.Empty;
            return new string(' ', nivell * ESPAIS_PER_NIVELL);
        }

        /// <summary>
        /// Constants per als nivells d'indentació estàndard
        /// Utilitza aquests nivells per mantenir consistència en tots els logs
        /// </summary>
        public static class Nivells
        {
            /// <summary>
            /// Nivell 0: Missatges principals (inici/final execució, separadors)
            /// Sense indentació
            /// </summary>
            public const int Principal = 0;

            /// <summary>
            /// Nivell 1: Use Case principal, mètodes principals
            /// Indentació: 2 espais
            /// </summary>
            public const int UseCase = 1;

            /// <summary>
            /// Nivell 2: Fases de processament, comprovacions principals
            /// Indentació: 4 espais
            /// </summary>
            public const int Fase = 2;

            /// <summary>
            /// Nivell 3: Detalls de comprovacions, operacions específiques
            /// Indentació: 6 espais
            /// </summary>
            public const int Comprovacio = 3;

            /// <summary>
            /// Nivell 4: Operacions internes, detalls tècnics
            /// Indentació: 8 espais
            /// </summary>
            public const int Operacio = 4;

            /// <summary>
            /// Nivell 5: Detalls molt específics (rarament utilitzat)
            /// Indentació: 10 espais
            /// </summary>
            public const int Detall = 5;
        }

        /// <summary>
        /// Afegeix indentació a un missatge existent
        /// </summary>
        /// <param name="missatge">Missatge original</param>
        /// <param name="nivell">Nivell d'indentació a aplicar</param>
        /// <returns>Missatge amb indentació aplicada</returns>
        public static string Format(string missatge, int nivell)
        {
            return Indent(nivell) + missatge;
        }

        /// <summary>
        /// Afegeix indentació a múltiples línies de text
        /// </summary>
        /// <param name="linies">Array de línies</param>
        /// <param name="nivell">Nivell d'indentació a aplicar</param>
        /// <returns>Línies amb indentació aplicada</returns>
        public static string[] FormatLinies(string[] linies, int nivell)
        {
            if (linies == null || linies.Length == 0)
                return linies;

            string[] resultat = new string[linies.Length];
            string indentacio = Indent(nivell);

            for (int i = 0; i < linies.Length; i++)
            {
                resultat[i] = indentacio + linies[i];
            }

            return resultat;
        }
    }
}
