using MultirRevisioVigencia.Application.DTOs;
using MultirRevisioVigencia.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultirRevisioVigencia.Application.Services
{
    /// <summary>
    /// Servei per calcular si un diagnòstic ha assolit el nombre necessari de mostres negatives consecutives
    /// </summary>
    public class MostresNegativesService
    {
        private readonly IMultiRRepository _repository;
        private readonly ILoggerService _logger;

        public MostresNegativesService(IMultiRRepository repository, ILoggerService logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Calcula els tipus de mostra i les quantitats necessàries per desactivar un diagnòstic
        /// Implementa el sistema de 3 fonts d'informació amb acumulació
        /// </summary>
        public Dictionary<string, int> CalcularTipusMostraQuantitats(DiagnosticPerRevisar diagnostic)
        {
            var tipusMostraAcumulat = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                _logger.Info($"     📊 Calculant els tipus de mostra i quantitats necessàries de negatius, per poder desactivar ...");

                // 1. TAULA DE REGLES: tipusmostra_referencia
                var regla = _repository.ObtenirReglaTipusMostra(diagnostic.Microorganisme, diagnostic.Mecanisme);

                if (regla != null && !string.IsNullOrWhiteSpace(regla.Resultat))
                {
                    _logger.Info($"     ℹ️ Per tipus de mostra de referència - Regla trobada : '{regla.Resultat}'");
                    ProcessarResultatRegla(regla.Resultat, tipusMostraAcumulat);
                }
                else
                {
                    _logger.Info($"     ℹ️ Per tipus de mostra de referència - No s'ha trobat cap regla específica a tipusmostra_referencia");
                }

                // 2. MOSTRES POSITIVES DEL DIAGNÒSTIC
                var mostresPositives = _repository.ObtenirMostresPositivesDiagnostic(diagnostic.Id);

                if (mostresPositives.Any())
                {
                    _logger.Info($"     📋 Tipus de mostra dels diferents positius. Mostres positives trobades: {mostresPositives.Count}");
                    ProcessarMostresPositives(mostresPositives, tipusMostraAcumulat);
                }
                else
                {
                    _logger.Info($"     ℹ️ Tipus de mostra dels diferents positius. No hi ha mostres positives registrades per aquest diagnòstic");
                }

                // Mostrar resultat final
                if (tipusMostraAcumulat.Any())
                {
                    _logger.Info($"     📋 Tipus de mostra i quantitats necessàries:");
                    foreach (var kvp in tipusMostraAcumulat.OrderBy(k => k.Key))
                    {
                        _logger.Info($"        - {kvp.Key}: {kvp.Value} mostres negatives");
                    }
                }
                else
                {
                    _logger.Info($"     ⚠️ No s'han pogut determinar tipus de mostra i quantitats");
                }

                return tipusMostraAcumulat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error calculant tipus de mostra i quantitats: {ex.Message}", ex);
                return tipusMostraAcumulat;
            }
        }

        /// <summary>
        /// Comprova si un diagnòstic compleix els requisits de mostres negatives consecutives per ser desactivat
        /// </summary>
        public bool CompleixRequisitsMostresNegatives(DiagnosticPerRevisar diagnostic, out string detalls)
        {
            detalls = string.Empty;

            try
            {
                // 1. Obtenir les quantitats necessàries per cada tipus de mostra
                var quantitatsNecessaries = CalcularTipusMostraQuantitats(diagnostic);

                if (!quantitatsNecessaries.Any())
                {
                    detalls = "No s'han pogut determinar les quantitats necessàries";
                    _logger.Info($"   ℹ️ {detalls}");
                    return false;
                }

                // 2. Obtenir totes les mostres del diagnòstic posteriors a la data de diagnòstic
                if (!diagnostic.DataDiagnostic.HasValue)
                {
                    detalls = "El diagnòstic no té data_diagnostic";
                    _logger.Warning($"   ⚠️ {detalls}");
                    return false;
                }

                var mostres = _repository.ObtenirMostresDiagnostic(diagnostic.Id, diagnostic.DataDiagnostic.Value);

                if (!mostres.Any())
                {
                    detalls = "No hi ha mostres registrades posteriors a la data de diagnòstic";
                    _logger.Info($"   ℹ️ {detalls}");
                    return false;
                }

                // Normalitzar tipus de mostra eliminant espais en blanc
                foreach (var mostra in mostres)
                {
                    if (mostra.TipusMostraM != null)
                    {
                        mostra.TipusMostraM = mostra.TipusMostraM.Trim();
                    }
                }

                _logger.Info($"     📊 Mostres trobades posteriors a la data de diagnòstic {diagnostic.DataDiagnostic.Value:dd/MM/yyyy}: {mostres.Count}");

                // 3. Agrupar per tipus de mostra i comprovar si hi ha suficients negatives consecutives
                var comptadors = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var compleixTots = true;
                var detallsLlista = new List<string>();

                foreach (var tipusMostra in quantitatsNecessaries.Keys)
                {
                    int quantitatNecessaria = quantitatsNecessaries[tipusMostra];
                    var mostresAquestTipus = mostres
                        .Where(m => m.TipusMostraM.Equals(tipusMostra, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(m => m.DataMostra)
                        .ToList();

                    if (!mostresAquestTipus.Any())
                    {
                        detallsLlista.Add($"{tipusMostra}: No hi ha mostres d'aquest tipus");
                        _logger.Info($"        ⚠️ {tipusMostra}: No hi ha mostres d'aquest tipus de mostra");
                        compleixTots = false;
                        continue;
                    }

                    // Comptar negatives consecutives des de l'últim positiu (o des del principi)
                    int negativesConsecutives = ComptarNegativesConsecutives(mostresAquestTipus);

                    // Generar seqüència de mostres per debug (N=negatiu, P=positiu)
                    string sequencia = string.Join("", mostresAquestTipus.Select(m => m.EsPositiva ? "P" : "N"));

                    // Comprovar si hi ha algun positiu
                    bool hiHaPositius = mostresAquestTipus.Any(m => m.EsPositiva);

                    bool compleix = negativesConsecutives >= quantitatNecessaria;
                    string simbolCompleix = compleix ? "✅" : "❌";

                    detallsLlista.Add($"{tipusMostra}: {negativesConsecutives}/{quantitatNecessaria} negatives consecutives");

                    if (hiHaPositius)
                    {
                        _logger.Info($"        {simbolCompleix} {tipusMostra}: {negativesConsecutives}/{quantitatNecessaria} negatives consecutives després del darrer positiu (Seqüència: {sequencia})");
                    }
                    else
                    {
                        _logger.Info($"        {simbolCompleix} {tipusMostra}: {negativesConsecutives}/{quantitatNecessaria} negatives consecutives (Seqüència: {sequencia})");
                    }

                    if (!compleix)
                    {
                        compleixTots = false;
                    }
                }

                detalls = string.Join("; ", detallsLlista);

                if (compleixTots)
                {
                    _logger.Info($"   ✅ El diagnòstic compleix els requisits de mostres negatives consecutives");
                }
                else
                {
                    _logger.Info($"   ❌ El diagnòstic NO compleix els requisits de mostres negatives consecutives");
                }

                return compleixTots;
            }
            catch (Exception ex)
            {
                detalls = $"Error: {ex.Message}";
                _logger.Error($"Error comprovant requisits mostres negatives: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Processa el resultat d'una regla (format: "Frotis rectal|5|Orina|6")
        /// i l'acumula al diccionari amb les quantitats màximes
        /// </summary>
        private void ProcessarResultatRegla(string resultat, Dictionary<string, int> tipusMostraAcumulat)
        {
            if (string.IsNullOrWhiteSpace(resultat))
                return;

            var parts = resultat.Split('|');

            for (int i = 0; i < parts.Length - 1; i += 2)
            {
                string nomTipus = parts[i].Trim();

                if (i + 1 >= parts.Length)
                    break;

                if (int.TryParse(parts[i + 1].Trim(), out int quantitat))
                {
                    // Si no existeix o la quantitat és superior, actualitzar
                    if (!tipusMostraAcumulat.ContainsKey(nomTipus) || tipusMostraAcumulat[nomTipus] < quantitat)
                    {
                        tipusMostraAcumulat[nomTipus] = quantitat;
                    }
                }
            }
        }

        /// <summary>
        /// Processa les mostres positives d'un diagnòstic
        /// Afegeix cada tipus de mostra amb quantitat = 3 si no existeix o és inferior
        /// </summary>
        private void ProcessarMostresPositives(List<MostraPositivaDiagnostic> mostresPositives, Dictionary<string, int> tipusMostraAcumulat)
        {
            foreach (var mostra in mostresPositives)
            {
                string tipusTrim = mostra.TipusMostraM?.Trim();

                if (string.IsNullOrWhiteSpace(tipusTrim))
                    continue;

                // Si no existeix o la quantitat és inferior a 3, actualitzar a 3
                if (!tipusMostraAcumulat.ContainsKey(tipusTrim) || tipusMostraAcumulat[tipusTrim] < 3)
                {
                    tipusMostraAcumulat[tipusTrim] = 3;
                }
            }
        }

        /// <summary>
        /// Compta les mostres negatives consecutives des de l'últim positiu
        /// Recorre les mostres des de la més recent cap enrere i para quan troba un positiu
        /// </summary>
        private int ComptarNegativesConsecutives(List<MostraDiagnostic> mostresOrdenades)
        {
            int comptador = 0;

            // Recórrer des de la més recent cap enrere
            for (int i = mostresOrdenades.Count - 1; i >= 0; i--)
            {
                if (mostresOrdenades[i].EsPositiva)
                {
                    // Positiu trobat, PARAR de comptar
                    // Només comptem les negatives DESPRÉS d'aquest positiu
                    break;
                }
                else
                {
                    // Negatiu, incrementar comptador
                    comptador++;
                }
            }

            return comptador;
        }
    }
}
