using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab.Application.UseCases.ProcessarMostres
{
    /// <summary>
    /// Use Case per validar una mostra
    /// Aplica les regles de negoci de validació
    /// </summary>
    public class ValidarMostraUseCase
    {
        private readonly ILoggerService _logger;

        public ValidarMostraUseCase(ILoggerService logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Valida que una mostra compleixi totes les regles de negoci
        /// </summary>
        public bool Executar(Mostra mostra)
        {
            // Validacions generals de la mostra
            
            // Validació 1: La mostra no pot ser null
            if (mostra == null)
            {
                _logger.Warning("Validació fallida: mostra és null");
                return false;
            }

            // Validació 2: Ha de tenir EtiquetaId
            if (string.IsNullOrWhiteSpace(mostra.EtiquetaId))
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Validació fallida: mostra sense EtiquetaId");
                return false;
            }

            // Validació 3: Ha de tenir PacientSap
            if (string.IsNullOrWhiteSpace(mostra.PacientSap))
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Validació fallida: mostra {mostra.EtiquetaId} sense PacientSap");
                return false;
            }

            // Validació 4: Ha de tenir almenys un resultat
            if (mostra.Resultats == null || mostra.Resultats.Count == 0)
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Validació fallida: mostra {mostra.EtiquetaId} sense resultats");
                return false;
            }

            // Validacions de resultats

            // Validació 5: Tots els resultats han de tenir DataResultat
            foreach (var resultat in mostra.Resultats)
            {
                if (resultat.DataResultat == default)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Validació fallida: resultat de mostra {mostra.EtiquetaId} sense DataResultat");
                    return false;
                }
            }

            return true;
        }
    }
}
