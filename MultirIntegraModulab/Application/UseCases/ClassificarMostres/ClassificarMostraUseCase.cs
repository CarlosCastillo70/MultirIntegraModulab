using System.Linq;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Domain.Enums;

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
                if (EsResultatPositiu(resultatMostra))
                {
                    resultat.ResultatsPositius++;
                }
                else
                {
                    resultat.ResultatsNegatius++;
                }
            }

            // Determinar el tipus de mostra : un sol positiu, un sol negatiu, mix, ...
            resultat.TipusMostra = DeterminarTipusMostra(resultat.ResultatsPositius, resultat.ResultatsNegatius);

            _logger.Info($"Mostra {mostra.EtiquetaId} classificada com {resultat.TipusMostra.ToString().ToUpper()}");

            return resultat;
        }

        private bool EsResultatPositiu(ResultatMostra registre)
        {
            // Un resultat és positiu si:
            // 1. Té microorganisme especial
            if (registre.EsMicroorganismeEspecial == true)
                return true;

            // 2. Té microorganisme i algun mecanisme de resistència
            if (!string.IsNullOrWhiteSpace(registre.AillamentDescripcio))
            {
                if (!string.IsNullOrWhiteSpace(registre.MecanismeResistencia1Id) ||
                    !string.IsNullOrWhiteSpace(registre.MecanismeResistencia2Id) ||
                    !string.IsNullOrWhiteSpace(registre.MecanismeResistencia3Id) ||
                    !string.IsNullOrWhiteSpace(registre.MecanismeResistencia4Id) ||
                    !string.IsNullOrWhiteSpace(registre.MecanismeResistencia5Id))
                {
                    return true;
                }
            }

            return false;
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
