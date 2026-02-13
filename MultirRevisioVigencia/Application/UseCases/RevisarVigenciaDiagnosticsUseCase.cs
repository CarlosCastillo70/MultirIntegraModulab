using System;
using MultirRevisioVigencia.Application.DTOs;
using MultirRevisioVigencia.Domain.Interfaces;

namespace MultirRevisioVigencia.Application.UseCases
{
    /// <summary>
    /// Cas d'ús per revisar la vigència dels diagnòstics i marcar com a no vigents els que han caducat
    /// </summary>
    public class RevisarVigenciaDiagnosticsUseCase
    {
        private readonly IMultiRRepository _repository;
        private readonly ILoggerService _logger;

        public RevisarVigenciaDiagnosticsUseCase(IMultiRRepository repository, ILoggerService logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executa la revisió de vigència de diagnòstics
        /// </summary>
        /// <returns>Resum de la revisió executada</returns>
        public ResumRevisioVigenciaDto Executar()
        {
            var resum = new ResumRevisioVigenciaDto
            {
                DataRevisio = DateTime.Now
            };

            try
            {
                _logger.Info("🔍 Iniciant revisió de vigència de diagnòstics...");
                _logger.Info("");

                // 1. Obtenir diagnòstics vigents que poden haver caducat
                _logger.Info("📋 Obtenint diagnòstics vigents per revisar...");
                var diagnosticsPerRevisar = _repository.ObtenirDiagnosticsVigentsPerRevisar();
                resum.TotalRevisats = diagnosticsPerRevisar.Count;

                _logger.Info($"   Trobats {diagnosticsPerRevisar.Count} diagnòstic(s) vigent(s) per revisar");
                _logger.Info("");

                if (diagnosticsPerRevisar.Count == 0)
                {
                    _logger.Info("✅ No hi ha diagnòstics per revisar");
                    return resum;
                }

                // 2. Revisar cada diagnòstic
                foreach (var diagnostic in diagnosticsPerRevisar)
                {
                    try
                    {
                        _logger.Info($"🔎 Revisant diagnòstic ID {diagnostic.Id} - Pacient: {diagnostic.PacientSap}");
                        _logger.Info($"   Microorganisme: {diagnostic.Microorganisme}");
                        _logger.Info($"   Mecanisme: {diagnostic.Mecanisme}");

                        // Comprovar si ha superat la vigència
                        if (HaSuperatVigencia(diagnostic))
                        {
                            _logger.Info($"   ⚠️ Diagnòstic ha superat la vigència");

                            // Marcar com a no vigent
                            bool marcat = _repository.MarcarDiagnosticNoVigent(
                                diagnostic.Id,
                                "SISTEMA_AUTO",
                                $"Caducat automàticament per superar {diagnostic.DiesVigencia} dies"
                            );

                            if (marcat)
                            {
                                resum.MarcatsNoVigents++;
                                resum.DiagnosticsMarcats.Add(new DiagnosticMarcat
                                {
                                    DiagnosticId = diagnostic.Id,
                                    PacientSap = diagnostic.PacientSap,
                                    Microorganisme = diagnostic.Microorganisme,
                                    Mecanisme = diagnostic.Mecanisme,
                                    DataUltimaMostra = diagnostic.DataUltimaMostra,
                                    DiesVigencia = diagnostic.DiesVigencia,
                                    Motiu = $"Superat {diagnostic.DiesVigencia} dies de vigència"
                                });

                                _logger.Info($"   ✅ Diagnòstic marcat com a no vigent correctament");
                            }
                            else
                            {
                                resum.Errors++;
                                _logger.Error($"   ❌ Error marcant diagnòstic com a no vigent");
                            }
                        }
                        else
                        {
                            _logger.Info($"   ✓ Diagnòstic encara vigent");
                        }

                        _logger.Info("");
                    }
                    catch (Exception ex)
                    {
                        resum.Errors++;
                        _logger.Error($"❌ Error processant diagnòstic {diagnostic.Id}: {ex.Message}", ex);
                        _logger.Info("");
                    }
                }

                _logger.Info("✅ Revisió de vigència finalitzada");
                return resum;
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Error executant revisió de vigència: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Comprova si un diagnòstic ha superat el seu període de vigència
        /// </summary>
        private bool HaSuperatVigencia(DiagnosticPerRevisar diagnostic)
        {
            if (!diagnostic.DataUltimaMostra.HasValue || !diagnostic.DiesVigencia.HasValue)
            {
                return false;
            }

            // Calcular els dies transcorreguts des de l'última mostra
            var diesTranscorreguts = (DateTime.Now.Date - diagnostic.DataUltimaMostra.Value.Date).Days;

            _logger.Info($"   Dies transcorreguts: {diesTranscorreguts}, Dies vigència configurats: {diagnostic.DiesVigencia}");

            // Si han passat més dies dels configurats, ha caducat
            return diesTranscorreguts > diagnostic.DiesVigencia.Value;
        }
    }
}
