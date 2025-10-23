using System;
using System.Linq;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Domain.Enums;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab.Application.UseCases.DeterminarTipus
{
    /// <summary>
    /// Use Case per determinar el tipus d'incorporació d'una mostra
    /// Compara l'estat de la mostra a Oracle amb MySQL per decidir l'acció a prendre
    /// </summary>
    public class DeterminarTipusIncorporacioUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly ILoggerService _logger;

        public DeterminarTipusIncorporacioUseCase(
            IMultiRRepository multiRRepository,
            ILoggerService logger)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executa la determinació del tipus d'incorporació
        /// </summary>
        /// <param name="mostra">Mostra a analitzar</param>
        /// <returns>Tipus d'incorporació determinat</returns>
        public TipusIncorporacio Executar(Mostra mostra)
        {
            if (mostra == null)
            {
                _logger.Warning("Intentant determinar tipus incorporació amb mostra null");
                throw new ArgumentNullException(nameof(mostra));
            }

            _logger.Info($"🔎 Determinant tipus incorporació per mostra {mostra.EtiquetaId}");

            try
            {
                // Obtenir les dates de la mostra de Modulab (Oracle)
                var dataResultatOracle = mostra.DataUltimResultat;
                var dataValidacioOracle = ObtenirDataValidacioMaxima(mostra);

                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Mostra {mostra.EtiquetaId}: DataResultat = {dataResultatOracle:dd/MM/yyyy HH:mm}, DataValidacio = {dataValidacioOracle?.ToString("dd/MM/yyyy HH:mm") ?? "null"}");

                // Classificar l'estat comparant amb una possible mostra de MultiR (MySQL)
                var tipusEstat = _multiRRepository.ClassificarEstatResultat(
                    mostra.EtiquetaId, 
                    dataResultatOracle, 
                    dataValidacioOracle);

                // Convertir TipusEstatResultat a TipusIncorporacio
                var tipusIncorporacio = ConvertirTipusEstat(tipusEstat);

                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Mostra {mostra.EtiquetaId} amb tipus d'incorporació {tipusIncorporacio} (estat: {tipusEstat})");

                return tipusIncorporacio;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error determinant tipus incorporació per mostra {mostra.EtiquetaId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Obté la data de validació màxima de la mostra
        /// </summary>
        private DateTime? ObtenirDataValidacioMaxima(Mostra mostra)
        {
            if (!mostra.AlgunResultatValidat)
            {
                return null;
            }

            var resultatsValidats = mostra.Resultats.Where(r => r.DataValidacio.HasValue);
            
            if (!resultatsValidats.Any())
            {
                return null;
            }

            return resultatsValidats.Max(r => r.DataValidacio.Value);
        }

        /// <summary>
        /// Converteix TipusEstatResultat a TipusIncorporacio
        /// </summary>
        private TipusIncorporacio ConvertirTipusEstat(TipusEstatResultat tipusEstat)
        {
            switch (tipusEstat)
            {
                case TipusEstatResultat.Nova:
                    return TipusIncorporacio.Nova;
                    
                case TipusEstatResultat.Antiga:
                    return TipusIncorporacio.Antiga;
                    
                case TipusEstatResultat.Repetida:
                    return TipusIncorporacio.Repetida;
                    
                case TipusEstatResultat.Desvalidada:
                    return TipusIncorporacio.Desvalidada;
                    
                case TipusEstatResultat.Validada:
                    return TipusIncorporacio.Validada;
                    
                case TipusEstatResultat.Revalidada:
                    return TipusIncorporacio.Revalidada;
                    
                case TipusEstatResultat.Canviada:
                default:
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}TipusEstatResultat.Canviada convertit a TipusIncorporacio.Revalidada");
                    return TipusIncorporacio.Revalidada; // Tractar canvis generals com revalidació
            }
        }
    }
}
