using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="pacientsAfiltrar">Llista opcional de NPATs de pacients per filtrar. Si està buida o null, processa tots</param>
        /// <param name="limitDiagnostics">Límit de diagnòstics a processar (0 = il·limitat)</param>
        /// <returns>Resum de la revisió executada</returns>
        public ResumRevisioVigenciaDto Executar(List<string> pacientsAfiltrar = null, int limitDiagnostics = 0)
        {
            var resum = new ResumRevisioVigenciaDto
            {
                DataRevisio = DateTime.Now
            };

            try
            {
                _logger.Info("🔍 Iniciant revisió de vigència de diagnòstics MR ...");
                
                // Informar si s'està aplicant un filtre de pacients
                if (pacientsAfiltrar != null && pacientsAfiltrar.Any())
                {
                    _logger.Info($"🔍 FILTRE ACTIU: Processant només {pacientsAfiltrar.Count} pacient(s) específic(s)");
                    _logger.Info($"    Pacients: {string.Join(", ", pacientsAfiltrar)}");
                }
                
                // Informar si s'està aplicant un límit de diagnòstics
                if (limitDiagnostics > 0)
                {
                    _logger.Info($"🔍 LÍMIT ACTIU: Processant màxim {limitDiagnostics} diagnòstic(s)");
                }
                
                _logger.Info("");

                // 1. Obtenir diagnòstics vigents que poden haver caducat
                _logger.Info("📋 Obtenint diagnòstics vigents per revisar...");
                var diagnosticsPerRevisar = _repository.ObtenirDiagnosticsVigentsPerRevisar();
                
                // Aplicar filtre de pacients si està informat
                if (pacientsAfiltrar != null && pacientsAfiltrar.Any())
                {
                    int totalOriginal = diagnosticsPerRevisar.Count;
                    diagnosticsPerRevisar = diagnosticsPerRevisar
                        .Where(d => pacientsAfiltrar.Contains(d.PacientSap, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                    
                    _logger.Info($"   Diagnòstics totals: {totalOriginal}");
                    _logger.Info($"   Diagnòstics després del filtre: {diagnosticsPerRevisar.Count}");
                }
                
                // Aplicar límit de diagnòstics si està informat
                if (limitDiagnostics > 0 && diagnosticsPerRevisar.Count > limitDiagnostics)
                {
                    int totalOriginal = diagnosticsPerRevisar.Count;
                    diagnosticsPerRevisar = diagnosticsPerRevisar.Take(limitDiagnostics).ToList();
                    _logger.Info($"   Diagnòstics limitats: de {totalOriginal} a {diagnosticsPerRevisar.Count}");
                }
                
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
                        _logger.Info($"_______________________________________________________________________________");
                        _logger.Info($"Processant diagnòstic ID {diagnostic.Id} - Pacient: {diagnostic.PacientSap}");
                        _logger.Info($"   Microorganisme: {diagnostic.Microorganisme} (Tipus: {diagnostic.TipusMicroorganisme})");
                        _logger.Info($"   Mecanisme: {diagnostic.Mecanisme}");
                        
                        string motiu = null;
                        bool hauDeMarcarNoVigent = false;
                        bool esPerExitus = false;

                        // COMPROVACIÓ 1: Pacient èxitus (global)
                        if (EsPacientExitus(diagnostic))
                        {
                            _logger.Info($"   ☠️ Pacient èxitus (data: {diagnostic.DataExitus:dd/MM/yyyy})");
                            hauDeMarcarNoVigent = true;
                            esPerExitus = true;
                            motiu = "E";
                        }
                        else
                        {
                            _logger.Info($"   ✓ Pacient NO és èxitus");
                        }

                        // COMPROVACIÓ 2: Ha superat la vigència (MultiResistent)
                        if (!hauDeMarcarNoVigent && HaSuperatVigencia(diagnostic))
                        {
                            _logger.Info($"   ⚠️ Diagnòstic ha superat la vigència");
                            hauDeMarcarNoVigent = true;
                            motiu = "V";
                        }
                        
                        if (!hauDeMarcarNoVigent)
                        {
                            _logger.Info($"   ✅ Diagnòstic encara vigent");
                        }

                        // Marcar com a no vigent si cal
                        if (hauDeMarcarNoVigent)
                        {
                            bool marcat = _repository.MarcarDiagnosticNoVigent(
                                diagnostic.Id,
                                "MULTIR_AUTOM",
                                motiu
                            );

                            if (marcat)
                            {
                                resum.MarcatsNoVigents++;
                                
                                if (esPerExitus)
                                {
                                    resum.MarcatsPerExitus++;
                                }
                                else
                                {
                                    resum.MarcatsPerVigencia++;
                                }
                                
                                resum.DiagnosticsMarcats.Add(new DiagnosticMarcat
                                {
                                    DiagnosticId = diagnostic.Id,
                                    PacientSap = diagnostic.PacientSap,
                                    Microorganisme = diagnostic.Microorganisme,
                                    Mecanisme = diagnostic.Mecanisme,
                                    DataUltimaMostra = diagnostic.DataUltimaMostra,
                                    DiesVigencia = diagnostic.DiesVigencia,
                                    Motiu = motiu
                                });

                                _logger.Info($"   ✅ Diagnòstic marcat com a no vigent correctament");
                            }
                            else
                            {
                                resum.Errors++;
                                _logger.Error($"   ❌ Error marcant diagnòstic com a no vigent");
                            }
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
            int diesVigenciaAplicar = 0;
            
            // Comprovar si el diagnòstic té mecanisme de resistència
            if (string.IsNullOrWhiteSpace(diagnostic.Mecanisme) || 
                diagnostic.Mecanisme.Equals("NOCOD", StringComparison.OrdinalIgnoreCase))
            {
                // Per diagnòstics sense mecanisme o amb NOCOD, aplicar vigència per defecte de 365 dies
                diesVigenciaAplicar = 365;
                
                if (string.IsNullOrWhiteSpace(diagnostic.Mecanisme))
                {
                    _logger.Info($"   ℹ️ Diagnòstic sense mecanisme de resistència - aplicant vigència per defecte: {diesVigenciaAplicar} dies");
                }
                else
                {
                    _logger.Info($"   ℹ️ Diagnòstic amb mecanisme NOCOD - aplicant vigència per defecte: {diesVigenciaAplicar} dies");
                }
            }
            else
            {
                // El diagnòstic té mecanisme: comprovar si té vigencia_inactiu configurat
                if (!diagnostic.VigenciaInactiu.HasValue || diagnostic.VigenciaInactiu.Value == 0)
                {
                    _logger.Info($"   ℹ️ Mecanisme '{diagnostic.Mecanisme}' sense vigència d'inactivitat configurada - no s'aplica vigència");
                    return false;
                }
                
                diesVigenciaAplicar = diagnostic.VigenciaInactiu.Value;
                _logger.Info($"   ℹ️ Mecanisme '{diagnostic.Mecanisme}' amb vigència configurada: {diesVigenciaAplicar} dies");
            }

            // Comprovar si existeix darrer positiu
            if (!diagnostic.DataDarrergPositiu.HasValue)
            {
                _logger.Info($"   ℹ️ No hi ha darrer positiu registrat (ni a mostres ni a data_diagnostic)");
                return false;
            }

            // Indicar l'origen de la data del darrer positiu
            if (diagnostic.DataDarrergPositiuEsDeDataDiagnostic)
            {
                _logger.Info($"   📌 Data darrer positiu obtinguda de 'data_diagnostic' (no hi ha mostres registrades)");
            }

            // Calcular la data límit (data darrer positiu + vigència)
            DateTime dataLimit = diagnostic.DataDarrergPositiu.Value.Date.AddDays(diesVigenciaAplicar);
            DateTime avui = DateTime.Now.Date;

            int diesTranscorreguts = (avui - diagnostic.DataDarrergPositiu.Value.Date).Days;

            _logger.Info($"   📅 Data darrer positiu: {diagnostic.DataDarrergPositiu.Value:dd/MM/yyyy}");
            _logger.Info($"   📅 Vigència aplicada: {diesVigenciaAplicar} dies");
            _logger.Info($"   📅 Data límit: {dataLimit:dd/MM/yyyy}");
            _logger.Info($"   📅 Dies transcorreguts: {diesTranscorreguts}");

            // Si la data d'avui és posterior a la data límit, ha superat la vigència
            bool haSuperatVigencia = avui > dataLimit;

            if (haSuperatVigencia)
            {
                _logger.Info($"   ⚠️ Ha superat la vigència ({diesTranscorreguts} dies > {diesVigenciaAplicar} dies)");
            }

            return haSuperatVigencia;
        }

        /// <summary>
        /// Comprova si un pacient està marcat com a èxitus
        /// </summary>
        private bool EsPacientExitus(DiagnosticPerRevisar diagnostic)
        {
            // Un pacient es considera èxitus si la data d'èxitus és válida i no és a la futura
            return diagnostic.DataExitus.HasValue && diagnostic.DataExitus.Value <= DateTime.Now;
        }
    }
}
