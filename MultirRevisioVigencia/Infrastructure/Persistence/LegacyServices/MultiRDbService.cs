using MultirRevisioVigencia.Application.DTOs;
using MultirRevisioVigencia.Domain.Interfaces;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace MultirRevisioVigencia.Infrastructure.Persistence.LegacyServices
{
    /// <summary>
    /// Servei per accedir a la base de dades MySQL de MultiR
    /// </summary>
    public class MultiRDbService : IMultiRRepository
    {
        private readonly string _connectionString;
        private readonly ILoggerService _logger;

        public MultiRDbService(string connectionString, ILoggerService logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Valida la connexió amb la base de dades
        /// </summary>
        public bool ValidarConnexio()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT 1", conn))
                    {
                        cmd.ExecuteScalar();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error de connexió MySQL: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Obté els diagnòstics vigents per revisar
        /// IMPORTANT: Només processa diagnòstics de MULTIRESISTENTS (tipus='M')
        /// Els virus respiratoris (tipus='R') segueixen uns altres criteris i es tractaran més endavant
        /// </summary>
        public List<DiagnosticPerRevisar> ObtenirDiagnosticsVigentsPerRevisar()
        {
            var diagnostics = new List<DiagnosticPerRevisar>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // Query per obtenir diagnòstics vigents amb informació de vigència, èxitus i darrer positiu
                    // FILTRE: Només microorganismes multiresistents (tipus='M')
                    string sql = @"
                        SELECT pd.id,
                            pd.npat,
                            pd.microorganisme,
                            pd.mecanisme,
                            m.dies_vigencia,
                            p.dt_exitus,
                            (SELECT MAX(pdm.data_mostra)
                             FROM pacients_diagnostics_mostra pdm
                             INNER JOIN mostra_microorganisme mm ON pdm.id = mm.pacient_diagnostic_mostra_id
                             WHERE mm.pacient_diagnostic_id = pd.id
                               AND pdm.valoracio = '2'
                               AND pdm.dt_delete IS NULL) AS data_darrer_positiu,
                            mec.vigencia_inactiu
                        FROM pacients_diagnostics pd
                        INNER JOIN microorganismes m ON pd.microorganisme = m.codi
                        LEFT JOIN pacients p ON pd.npat = p.npat
                        LEFT JOIN mecanismes mec ON pd.mecanisme = mec.codi AND mec.dt_delete IS NULL
                        WHERE pd.vigent = 'S'
                            AND pd.dt_delete IS NULL
                            AND m.tipus = 'M'
                        ORDER BY pd.id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnostics.Add(new DiagnosticPerRevisar
                                {
                                    Id = reader.GetInt32("id"),
                                    PacientSap = reader["npat"]?.ToString(),
                                    Microorganisme = reader["microorganisme"]?.ToString(),
                                    Mecanisme = reader["mecanisme"]?.ToString(),
                                    DataUltimaMostra = null,
                                    DiesVigencia = reader.IsDBNull(reader.GetOrdinal("dies_vigencia"))
                                        ? (int?)null
                                        : reader.GetInt32("dies_vigencia"),
                                    DataExitus = reader.IsDBNull(reader.GetOrdinal("dt_exitus"))
                                        ? (DateTime?)null
                                        : reader.GetDateTime("dt_exitus"),
                                    DataDarrergPositiu = reader.IsDBNull(reader.GetOrdinal("data_darrer_positiu"))
                                        ? (DateTime?)null
                                        : reader.GetDateTime("data_darrer_positiu"),
                                    VigenciaInactiu = reader.IsDBNull(reader.GetOrdinal("vigencia_inactiu"))
                                        ? (int?)null
                                        : reader.GetInt32("vigencia_inactiu")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error obtenint diagnòstics vigents per revisar: {ex.Message}", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// Marca un diagnòstic com a no vigent
        /// </summary>
        public bool MarcarDiagnosticNoVigent(int diagnosticId, string responsable, string motiu = null)
        {
            if (diagnosticId <= 0)
            {
                _logger.Error("MarcarDiagnosticNoVigent: diagnosticId invàlid");
                return false;
            }

            if (string.IsNullOrWhiteSpace(responsable))
            {
                _logger.Error("MarcarDiagnosticNoVigent: responsable és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        UPDATE pacients_diagnostics
                        SET vigent = 'N',
                            data_no_vigent = NOW(),
                            responsable_no_vigent = @responsable,
                            motiu_no_vigent = @motiu,
                            dt_update = NOW()
                        WHERE id = @diagnosticId
                          AND vigent = 'S'
                          AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);
                        cmd.Parameters.AddWithValue("@responsable", responsable);
                        cmd.Parameters.AddWithValue("@motiu", motiu ?? (object)DBNull.Value);

                        int filesAfectades = cmd.ExecuteNonQuery();

                        if (filesAfectades > 0)
                        {
                            _logger.Info($"   ✅ Diagnòstic {diagnosticId} marcat com a no vigent");
                            return true;
                        }
                        else
                        {
                            _logger.Warning($"   ⚠️ No s'han afectat files per diagnòstic {diagnosticId}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error marcant diagnòstic {diagnosticId} com a no vigent: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Reactiva un diagnòstic marcant-lo com a vigent
        /// </summary>
        public bool ReactivarDiagnostic(int diagnosticId, string responsable, string motiu = null)
        {
            if (diagnosticId <= 0)
            {
                _logger.Error("ReactivarDiagnostic: diagnosticId invàlid");
                return false;
            }

            if (string.IsNullOrWhiteSpace(responsable))
            {
                _logger.Error("ReactivarDiagnostic: responsable és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        UPDATE pacients_diagnostics
                        SET vigent = 'S',
                            data_no_vigent = NULL,
                            responsable_no_vigent = NULL,
                            motiu_no_vigent = NULL,
                            dt_update = NOW()
                        WHERE id = @diagnosticId
                          AND vigent = 'N'
                          AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);

                        int filesAfectades = cmd.ExecuteNonQuery();

                        if (filesAfectades > 0)
                        {
                            _logger.Info($"   ✅ Diagnòstic {diagnosticId} reactivat com a vigent");
                            return true;
                        }
                        else
                        {
                            _logger.Warning($"   ⚠️ No s'han afectat files per diagnòstic {diagnosticId}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error reactivant diagnòstic {diagnosticId}: {ex.Message}", ex);
                return false;
            }
        }
    }
}
