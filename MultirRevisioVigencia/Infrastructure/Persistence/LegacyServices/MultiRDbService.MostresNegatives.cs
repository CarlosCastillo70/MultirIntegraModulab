using MultirRevisioVigencia.Application.DTOs;
using MultirRevisioVigencia.Domain.Interfaces;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace MultirRevisioVigencia.Infrastructure.Persistence.LegacyServices
{
    /// <summary>
    /// Extensions parcials per MultiRDbService - Gestió de mostres negatives
    /// </summary>
    public partial class MultiRDbService
    {
        /// <summary>
        /// Obté la regla de tipus de mostra per un microorganisme i mecanisme
        /// Busca la regla amb més prioritat que coincideixi amb els patrons
        /// </summary>
        public ReglaTipusMostra ObtenirReglaTipusMostra(string microorganisme, string mecanisme)
        {
            if (string.IsNullOrWhiteSpace(microorganisme))
            {
                _logger.Warning("ObtenirReglaTipusMostra: microorganisme és null o buit");
                return null;
            }

            // Si el mecanisme és buit o null, buscar com 'SENSE'
            string mecanismeABuscar = string.IsNullOrWhiteSpace(mecanisme) ? "SENSE" : mecanisme;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT id, 
                               microorganisme_patro, 
                               mecanisme_patro, 
                               resultat, 
                               prioritat,
                               actiu
                        FROM tipusmostra_referencia
                        WHERE @microorganisme LIKE microorganisme_patro
                          AND @mecanisme LIKE mecanisme_patro
                          AND (actiu = 1 OR actiu IS NULL)
                        ORDER BY prioritat ASC
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@microorganisme", microorganisme);
                        cmd.Parameters.AddWithValue("@mecanisme", mecanismeABuscar);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ReglaTipusMostra
                                {
                                    Id = reader.GetInt32("id"),
                                    MicroorganismePatro = reader["microorganisme_patro"]?.ToString(),
                                    MecanismePatro = reader["mecanisme_patro"]?.ToString(),
                                    Resultat = reader["resultat"]?.ToString(),
                                    Prioritat = reader.GetInt32("prioritat"),
                                    Actiu = reader.IsDBNull(reader.GetOrdinal("actiu")) || reader.GetInt32("actiu") == 1
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error obtenint regla tipus mostra per '{microorganisme}' / '{mecanismeABuscar}': {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// Obté les mostres positives d'un diagnòstic
        /// </summary>
        public List<MostraPositivaDiagnostic> ObtenirMostresPositivesDiagnostic(int diagnosticId)
        {
            var mostres = new List<MostraPositivaDiagnostic>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT pdm.id, pdm.tipus_mostra_m, pdm.data_mostra
                        FROM mostra_microorganisme mm
                        INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                        WHERE mm.pacient_diagnostic_id = @diagnosticId
                          AND pdm.valoracio = '2'
                          AND pdm.dt_delete IS NULL
                        ORDER BY pdm.data_mostra DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                mostres.Add(new MostraPositivaDiagnostic
                                {
                                    Id = reader.GetInt32("id"),
                                    TipusMostraM = reader["tipus_mostra_m"]?.ToString(),
                                    DataMostra = reader.GetDateTime("data_mostra")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error obtenint mostres positives del diagnòstic {diagnosticId}: {ex.Message}", ex);
            }

            return mostres;
        }

        /// <summary>
        /// Obté totes les mostres (positives i negatives) d'un diagnòstic posteriors a la data de diagnòstic
        /// </summary>
        public List<MostraDiagnostic> ObtenirMostresDiagnostic(int diagnosticId, DateTime dataDiagnostic)
        {
            var mostres = new List<MostraDiagnostic>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT pdm.id, pdm.tipus_mostra_m, pdm.data_mostra, pdm.valoracio
                        FROM mostra_microorganisme mm
                        INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                        WHERE mm.pacient_diagnostic_id = @diagnosticId
                          AND pdm.data_mostra >= @dataDiagnostic
                          AND pdm.dt_delete IS NULL
                        ORDER BY pdm.data_mostra ASC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);
                        cmd.Parameters.AddWithValue("@dataDiagnostic", dataDiagnostic);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                mostres.Add(new MostraDiagnostic
                                {
                                    Id = reader.GetInt32("id"),
                                    TipusMostraM = reader["tipus_mostra_m"]?.ToString(),
                                    DataMostra = reader.GetDateTime("data_mostra"),
                                    Valoracio = reader["valoracio"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error obtenint mostres del diagnòstic {diagnosticId}: {ex.Message}", ex);
            }

            return mostres;
        }
    }
}
