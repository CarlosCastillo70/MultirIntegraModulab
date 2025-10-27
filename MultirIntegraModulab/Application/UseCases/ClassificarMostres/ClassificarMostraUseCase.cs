using System.Linq;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Domain.Enums;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab.Application.UseCases.ClassificarMostres
{
    /// <summary>
    /// Resultat de la classificació d'una mostra
    /// </summary>
    public class ResultatClassificacio
    {
        public TipusMostra TipusMostra { get; set; }
        public int ResultatsPositius { get; set; }
        public int ResultatsNegatius { get; set; }
    }

    /// <summary>
    /// Use Case per classificar una mostra segons els seus resultats
    /// </summary>
    public class ClassificarMostraUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly ILoggerService _logger;

        public ClassificarMostraUseCase(IMultiRRepository multiRRepository, ILoggerService logger)
        {
            _multiRRepository = multiRRepository;
            _logger = logger;
        }

        /// <summary>
        /// Classifica una mostra segons els seus resultats positius/negatius
        /// </summary>
        public ResultatClassificacio Executar(Mostra mostra)
        {
            var resultat = new ResultatClassificacio();
            
            foreach (var resultatMostra in mostra.Resultats)
            {
                var comptatge = ComptarPositiusINegatius(resultatMostra);
                resultat.ResultatsPositius += comptatge.Positius;
                resultat.ResultatsNegatius += comptatge.Negatius;
            }

            // Determinar el tipus de mostra : un sol positiu, un sol negatiu, mix, ...
            resultat.TipusMostra = DeterminarTipusMostra(resultat.ResultatsPositius, resultat.ResultatsNegatius);

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Mostra es classifica com '{resultat.TipusMostra.ToString().ToUpper()}' ({resultat.ResultatsPositius} positius, {resultat.ResultatsNegatius} negatius)");

            return resultat;
        }

        /// <summary>
        /// Compta el nombre de positius i negatius d'un resultat
        /// </summary>
        /// <param name="resultatMostra">Resultat a analitzar</param>
        /// <returns>Tuple amb (Positius, Negatius)</returns>
        private (int Positius, int Negatius) ComptarPositiusINegatius(ResultatMostra resultatMostra)
        {
            int positius = 0;
            int negatius = 0;

            // Comprovar si té microorganisme
            bool teMicroorganisme = !string.IsNullOrWhiteSpace(resultatMostra.AillamentDescripcio);

            if (!teMicroorganisme)
            {
                // Si no hi ha microorganisme, és un negatiu
                negatius = 1;
                return (positius, negatius);
            }

            // Comptar mecanismes de resistència
            int nombreMecanismes = ComptarMecanismesResistencia(resultatMostra);

            // Si és microorganisme especial
            if (resultatMostra.EsMicroorganismeEspecial == true)
            {
                if (nombreMecanismes == 0)
                {
                    // Microorganisme especial sense mecanismes = 1 positiu
                    positius = 1;
                }
                else
                {
                    // Microorganisme especial amb N mecanismes = N positius
                    positius = nombreMecanismes;
                }
            }
            else
            {
                // Microorganisme no especial
                if (nombreMecanismes == 0)
                {
                    // Microorganisme no especial sense mecanismes = 1 negatiu
                    negatius = 1;
                }
                else
                {
                    // Microorganisme no especial amb N mecanismes = N positius
                    positius = nombreMecanismes;
                }
            }

            return (positius, negatius);
        }

        /// <summary>
        /// Compta el nombre de mecanismes de resistència presents en un resultat
        /// </summary>
        private int ComptarMecanismesResistencia(ResultatMostra resultatMostra)
        {
            int nombre = 0;

            if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia1Id))
                nombre++;
            if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia2Id))
                nombre++;
            if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia3Id))
                nombre++;
            if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia4Id))
                nombre++;
            if (!string.IsNullOrWhiteSpace(resultatMostra.MecanismeResistencia5Id))
                nombre++;

            return nombre;
        }

        private TipusMostra DeterminarTipusMostra(int positius, int negatius)
        {
            if (positius == 1 && negatius == 0)
                return TipusMostra.UnSolResultatPositiu;

            if (positius > 1 && negatius == 0)
                return TipusMostra.MultiplesResultatsTotsPositius;

            if (positius == 0 && negatius == 1)
                return TipusMostra.UnSolResultatNegatiu;

            if (positius == 0 && negatius > 1)
                return TipusMostra.MultiplesResultatsTotsNegatius;

            if (positius > 0 && negatius > 0)
                return TipusMostra.MultiplesResultatsPositiusINegatius;

            // Per defecte, si no hi ha registres, considerem com sol negatiu
            return TipusMostra.UnSolResultatNegatiu;
        }
    }
}
