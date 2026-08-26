using System;
using System.Linq;
using System.Threading.Tasks;
using MultirIntegraModulab.Application.DTOs;
using MultirIntegraModulab.Application.Helpers;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Enums;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Application.UseCases.ClassificarMostres;

namespace MultirIntegraModulab.Application.UseCases.ProcessarMostres
{
    /// <summary>
    /// Use Case per processar mostres mixtes amb MMR i VR
    /// Processa primer els MMR i després els VR
    /// </summary>
    public class ProcessarMostraMixtaMMRVRUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly IPacientWebService _pacientWebService;
        private readonly ILoggerService _logger;
        private readonly ClassificarMostraUseCase _classificarMostraUseCase;
        private readonly ProcessarMostraPositivaUseCase _processarPositivaMMR;
        private readonly ProcessarMostraNegativaUseCase _processarNegativaMMR;
        private readonly ProcessarMostresPositivesUseCase _processarPositivesMMR;
        private readonly ProcessarMostresNegativesUseCase _processarNegativesMMR;
        private readonly ProcessarMostraMixtaUseCase _processarMixtaMMR;
        private readonly ProcessarMostraVirusRespiratoriUseCase _processarVR;

        public ProcessarMostraMixtaMMRVRUseCase(
            IMultiRRepository multiRRepository,
            IPacientWebService pacientWebService,
            ILoggerService logger,
            ClassificarMostraUseCase classificarMostraUseCase,
            ProcessarMostraPositivaUseCase processarPositivaMMR,
            ProcessarMostraNegativaUseCase processarNegativaMMR,
            ProcessarMostresPositivesUseCase processarPositivesMMR,
            ProcessarMostresNegativesUseCase processarNegativesMMR,
            ProcessarMostraMixtaUseCase processarMixtaMMR,
            ProcessarMostraVirusRespiratoriUseCase processarVR)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _pacientWebService = pacientWebService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _classificarMostraUseCase = classificarMostraUseCase ?? throw new ArgumentNullException(nameof(classificarMostraUseCase));
            _processarPositivaMMR = processarPositivaMMR ?? throw new ArgumentNullException(nameof(processarPositivaMMR));
            _processarNegativaMMR = processarNegativaMMR ?? throw new ArgumentNullException(nameof(processarNegativaMMR));
            _processarPositivesMMR = processarPositivesMMR ?? throw new ArgumentNullException(nameof(processarPositivesMMR));
            _processarNegativesMMR = processarNegativesMMR ?? throw new ArgumentNullException(nameof(processarNegativesMMR));
            _processarMixtaMMR = processarMixtaMMR ?? throw new ArgumentNullException(nameof(processarMixtaMMR));
            _processarVR = processarVR ?? throw new ArgumentNullException(nameof(processarVR));
        }

        /// <summary>
        /// Executa el processament d'una mostra mixta (MMR + VR)
        /// </summary>
        public async Task<ResultatProcessamentMixtMMRVR> ExecutarAsync(Mostra mostra)
        {
            var resultat = new ResultatProcessamentMixtMMRVR();

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🔀 FLUX MIXT (MMR + VR) activat");

            // PART 1: PROCESSAR RESULTATS MMR
            
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🦠 PROCESSANT RESULTATS MULTIRESISTENTS");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");

            var mostraMMR = CrearMostraAmbResultatsMMR(mostra);
            
            if (mostraMMR.Resultats.Any())
            {
                var classificacioMMR = _classificarMostraUseCase.Executar(mostraMMR);
                await ProcessarPerTipusMostraMMRAsync(mostraMMR, classificacioMMR, resultat);
            }

            // PART 2: PROCESSAR RESULTATS VR
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🦠 PROCESSANT RESULTATS VIRUS RESPIRATORIS");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}------------------------------------------------------------------------------");

            var mostraVR = CrearMostraAmbResultatsVR(mostra);
            
            if (mostraVR.Resultats.Any())
            {
                var resultatVR = await _processarVR.ExecutarAsync(mostraVR);
                
                if (resultatVR != null && resultatVR.Exitosa)
                {
                    resultat.ResultatsVRProcessats = resultatVR.ResultatsProcessats;
                    resultat.DiagnosticsVRCreats = resultatVR.DiagnosticsCreats;
                    resultat.PositiusVRIncorporats = resultatVR.PositiusVirusRespiratorisIncorporats;
                }
            }

            resultat.Exitosa = true;
            return resultat;
        }

        /// <summary>
        /// Crea una mostra temporal només amb els resultats MMR
        /// </summary>
        private Mostra CrearMostraAmbResultatsMMR(Mostra mostraOriginal)
        {
            var mostraMMR = new Mostra(mostraOriginal.EtiquetaId, mostraOriginal.PacientSap);

            foreach (var resultat in mostraOriginal.Resultats)
            {
                if (!string.IsNullOrWhiteSpace(resultat.AillamentDescripcio))
                {
                    var tipus = _multiRRepository.ObtenirTipusMicroorganisme(resultat.AillamentDescripcio);
                    
                    if (tipus == TipusMicroorganisme.VirusRespiratori)
                        continue;
                }

                mostraMMR.AfegirResultat(resultat);
            }

            return mostraMMR;
        }

        /// <summary>
        /// Crea una mostra temporal només amb els resultats VR
        /// </summary>
        private Mostra CrearMostraAmbResultatsVR(Mostra mostraOriginal)
        {
            var mostraVR = new Mostra(mostraOriginal.EtiquetaId, mostraOriginal.PacientSap);

            foreach (var resultat in mostraOriginal.Resultats)
            {
                if (string.IsNullOrWhiteSpace(resultat.AillamentDescripcio))
                    continue;

                var tipus = _multiRRepository.ObtenirTipusMicroorganisme(resultat.AillamentDescripcio);
                
                if (tipus == TipusMicroorganisme.VirusRespiratori)
                    mostraVR.AfegirResultat(resultat);
            }

            return mostraVR;
        }

        /// <summary>
        /// Processa mostra MMR segons tipus
        /// </summary>
        private async Task ProcessarPerTipusMostraMMRAsync(
            Mostra mostra, 
            ResultatClassificacio classificacio, 
            ResultatProcessamentMixtMMRVR resultat)
        {
            switch (classificacio.TipusMostra)
            {
                case TipusMostra.UnSolResultatPositiu:
                    var resPositiu = await _processarPositivaMMR.ExecutarAsync(mostra, classificacio);
                    if (resPositiu.Exitosa)
                    {
                        resultat.ResultatsMMRPositius++;
                        resultat.PositiuAfegit = resPositiu.PositiuAfegit;
                        resultat.PositiusMMRIncorporats += resPositiu.PositiusIncorporats;
                        resultat.NegatiusMMRContrarestaPositiuIncorporats += resPositiu.NegatiusContrarestaPositiuIncorporats;
                    }
                    break;

                case TipusMostra.UnSolResultatNegatiu:
                    var resNegatiu = await _processarNegativaMMR.ExecutarAsync(mostra, classificacio);
                    if (resNegatiu.Exitosa)
                    {
                        resultat.ResultatsMMRNegatius++;
                        resultat.NegatiusMMRIncorporats += resNegatiu.NegatiusIncorporats;
                    }
                    break;

                case TipusMostra.MultiplesResultatsTotsPositius:
                    var resPositives = await _processarPositivesMMR.ExecutarAsync(mostra, classificacio);
                    if (resPositives.Exitosa)
                    {
                        resultat.ResultatsMMRPositius += classificacio.ResultatsPositius;
                        resultat.PositiuAfegit = resPositives.PositiuAfegit;
                        resultat.PositiusMMRIncorporats += resPositives.PositiusIncorporats;
                        resultat.NegatiusMMRContrarestaPositiuIncorporats += resPositives.NegatiusContrarestaPositiuIncorporats;
                    }
                    break;

                case TipusMostra.MultiplesResultatsTotsNegatius:
                    var resNegatives = await _processarNegativesMMR.ExecutarAsync(mostra, classificacio);
                    if (resNegatives.Exitosa)
                    {
                        resultat.ResultatsMMRNegatius += classificacio.ResultatsNegatius;
                        resultat.NegatiusMMRIncorporats += resNegatives.NegatiusIncorporats;
                    }
                    break;

                case TipusMostra.MultiplesResultatsPositiusINegatius:
                    var resMixta = await _processarMixtaMMR.ExecutarAsync(mostra, classificacio);
                    if (resMixta.Exitosa)
                    {
                        resultat.ResultatsMMRPositius += classificacio.ResultatsPositius;
                        resultat.ResultatsMMRNegatius += classificacio.ResultatsNegatius;
                        resultat.PositiuAfegit = resMixta.PositiuAfegit;
                        resultat.PositiusMMRIncorporats += resMixta.PositiusIncorporats;
                        resultat.NegatiusMMRIncorporats += resMixta.NegatiusIncorporats;
                        resultat.NegatiusMMRContrarestaPositiuIncorporats += resMixta.NegatiusContrarestaPositiuIncorporats;
                    }
                    break;
            }
        }
    }
}
