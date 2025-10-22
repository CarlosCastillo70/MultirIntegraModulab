using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Enums;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService per gestionar l'historial de mostres
    /// </summary>
    public partial class MultiRDbService
    {
        /// <summary>
        /// Obté estadístiques generals de l'historial de mostres
        /// </summary>
        /// <returns>Estadístiques de l'historial</returns>
        public EstadistiquesHistorial ObtenirEstadistiquesHistorial()
        {
            var estadistiques = new EstadistiquesHistorial();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // Comptar registres totals
                    string sqlTotal = @"SELECT COUNT(*) 
                                       FROM pacients_diagnostics_mostra_historial 
                                       WHERE dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sqlTotal, conn))
                    {
                        estadistiques.TotalRegistresHistorial = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    if (estadistiques.TotalRegistresHistorial > 0)
                    {
                        // Obtenir dates extremes
                        string sqlDates = @"SELECT MIN(data_canvi) as primer, MAX(data_canvi) as ultim
                                           FROM pacients_diagnostics_mostra_historial 
                                           WHERE dt_delete IS NULL AND data_canvi IS NOT NULL";

                        using (var cmd = new MySqlCommand(sqlDates, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    estadistiques.PrimerRegistre = reader["primer"] as DateTime?;
                                    estadistiques.UltimRegistre = reader["ultim"] as DateTime?;
                                }
                            }
                        }

                        // Obtenir distribució per tipus de canvi
                        string sqlTipus = @"SELECT tipus_canvi, COUNT(*) as total
                                           FROM pacients_diagnostics_mostra_historial 
                                           WHERE dt_delete IS NULL 
                                           GROUP BY tipus_canvi
                                           ORDER BY total DESC";

                        using (var cmd = new MySqlCommand(sqlTipus, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string tipus = reader["tipus_canvi"]?.ToString() ?? "DESCONEGUT";
                                    int total = Convert.ToInt32(reader["total"]);
                                    estadistiques.RegistresPerTipus[tipus] = total;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint estadístiques d'historial: {ex.Message}", ex);
                // Retornar estadístiques buides en cas d'error
                estadistiques = new EstadistiquesHistorial();
            }

            return estadistiques;
        }

        /// <summary>
        /// Comprova si existeix historial per una mostra específica
        /// </summary>
        /// <param name="etiquetaId">Identificador de l'etiqueta</param>
        /// <returns>Nombre de registres d'historial per la mostra</returns>
        public int ComprovarHistorialExisteix(string etiquetaId)
        {
            if (string.IsNullOrWhiteSpace(etiquetaId))
            {
                Logger.Warning("ComprovarHistorialExisteix: etiquetaId és null o buit");
                return 0;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT COUNT(*) 
                                  FROM pacients_diagnostics_mostra_historial 
                                  WHERE etiqueta_id = @etiquetaId 
                                  AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiquetaId", etiquetaId);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant historial per {etiquetaId}: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Obté l'historial complet d'una mostra
        /// </summary>
        /// <param name="etiquetaId">Identificador de l'etiqueta</param>
        /// <returns>Llista ordenada de registres d'historial (més recent primer)</returns>
        public List<RegistreHistorialMostra> ObtenirHistorialMostra(string etiquetaId)
        {
            var historial = new List<RegistreHistorialMostra>();

            if (string.IsNullOrWhiteSpace(etiquetaId))
            {
                Logger.Warning("ObtenirHistorialMostra: etiquetaId és null o buit");
                return historial;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT id, etiqueta_id, pacient_sap, data_canvi, tipus_canvi, 
                                         estat_abans_canvi, estat_despres_canvi, microorganisme, 
                                         mecanisme_resistencia, observacions, dt_create
                                  FROM pacients_diagnostics_mostra_historial 
                                  WHERE etiqueta_id = @etiquetaId 
                                  AND dt_delete IS NULL
                                  ORDER BY data_canvi DESC, dt_create DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiquetaId", etiquetaId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var registre = new RegistreHistorialMostra
                                {
                                    Id = reader.GetInt32("id"),
                                    // Usar EtiquetaOriginal en lugar de EtiquetaId
                                    EtiquetaOriginal = reader["etiqueta_id"]?.ToString(),
                                    PacientSap = reader["pacient_sap"]?.ToString(),
                                    DataCanvi = reader["data_canvi"] as DateTime?,
                                    TipusCanvi = reader["tipus_canvi"]?.ToString(),
                                    EstatAbansCanvi = reader["estat_abans_canvi"]?.ToString(),
                                    // Usar una propiedad válida en lugar de EstatDespresCanvi
                                    Observacions = reader["estat_despres_canvi"]?.ToString(),
                                    Microorganisme = reader["microorganisme"]?.ToString(),
                                    MecanismeResistencia = reader["mecanisme_resistencia"]?.ToString(),
                                    // Concatenar observaciones si es necesario
                                    DataCreacio = reader["dt_create"] != DBNull.Value ? Convert.ToDateTime(reader["dt_create"]) : DateTime.MinValue
                                };

                                historial.Add(registre);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint historial per {etiquetaId}: {ex.Message}", ex);
            }

            return historial;
        }

        /// <summary>
        /// Guarda un registre d'historial per una mostra
        /// </summary>
        /// <param name="etiquetaId">Identificador de l'etiqueta</param>
        /// <param name="tipusCanvi">Tipus de canvi realitzat</param>
        /// <param name="observacions">Observacions opcionals</param>
        /// <param name="microorganisme">Microorganisme afectat (opcional)</param>
        /// <param name="mecanisme">Mecanisme de resistència afectat (opcional)</param>
        /// <param name="estatAbans">Estat abans del canvi (opcional)</param>
        /// <param name="estatDespres">Estat després del canvi (opcional)</param>
        /// <returns>True si s'ha guardat correctament</returns>
        public bool GuardarHistorialMostra(string etiquetaId, string tipusCanvi, string observacions = null,
            string microorganisme = null, string mecanisme = null, string estatAbans = null, string estatDespres = null)
        {
            if (string.IsNullOrWhiteSpace(etiquetaId))
            {
                Logger.Error("GuardarHistorialMostra: etiquetaId és null o buit");
                return false;
            }

            if (string.IsNullOrWhiteSpace(tipusCanvi))
            {
                Logger.Error("GuardarHistorialMostra: tipusCanvi és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // Obtenir pacient_sap de la mostra actual si cal
                    string pacientSap = null;
                    if (string.IsNullOrEmpty(pacientSap))
                    {
                        string sqlPacient = @"SELECT DISTINCT npat 
                                             FROM pacients_diagnostics_mostra 
                                             WHERE etiqueta = @etiquetaId 
                                             AND dt_delete IS NULL 
                                             LIMIT 1";

                        using (var cmdPacient = new MySqlCommand(sqlPacient, conn))
                        {
                            cmdPacient.Parameters.AddWithValue("@etiquetaId", etiquetaId);
                            pacientSap = cmdPacient.ExecuteScalar()?.ToString();
                        }
                    }

                    string sql = @"INSERT INTO pacients_diagnostics_mostra_historial 
                                  (etiqueta_id, pacient_sap, data_canvi, tipus_canvi, estat_abans_canvi, 
                                   estat_despres_canvi, microorganisme, mecanisme_resistencia, observacions, 
                                   dt_create, dt_update)
                                  VALUES 
                                  (@etiquetaId, @pacientSap, NOW(), @tipusCanvi, @estatAbans, 
                                   @estatDespres, @microorganisme, @mecanisme, @observacions, 
                                   NOW(), NOW())";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiquetaId", etiquetaId);
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@tipusCanvi", tipusCanvi);
                        cmd.Parameters.AddWithValue("@estatAbans", estatAbans ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@estatDespres", estatDespres ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@microorganisme", microorganisme ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@mecanisme", mecanisme ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@observacions", observacions ?? (object)DBNull.Value);

                        int filesAfectades = cmd.ExecuteNonQuery();

                        if (filesAfectades > 0)
                        {
                            Logger.Info($"Registre d'historial guardat per {etiquetaId}: {tipusCanvi}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"No s'han afectat files guardant historial per {etiquetaId}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error guardant historial per {etiquetaId}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Guarda historial automàticament quan es detecten canvis en mostres validades/revalidades/desvalidades
        /// </summary>
        /// <param name="mostra">Mostra que ha canviat</param>
        /// <param name="tipusIncorporacio">Tipus d'incorporació detectat</param>
        /// <param name="observacions">Observacions opcionals sobre el canvi</param>
        /// <returns>True si s'ha guardat correctament</returns>
        public bool GuardarHistorialAutomaticMostra(Mostra mostra, TipusIncorporacio tipusIncorporacio, string observacions = null)
        {
            if (mostra == null)
            {
                Logger.Error("GuardarHistorialAutomaticMostra: mostra és null");
                return false;
            }

            // Només guardar historial per canvis significatius
            string tipusCanvi = null;
            switch (tipusIncorporacio)
            {
                case TipusIncorporacio.Desvalidada:
                    tipusCanvi = "DESVALIDADA_CANVI";
                    break;
                case TipusIncorporacio.Validada:
                    tipusCanvi = "VALIDADA_CANVI";
                    break;
                case TipusIncorporacio.Revalidada:
                    tipusCanvi = "REVALIDADA_CANVI";
                    break;
                default:
                    // No guardar historial per altres tipus
                    return true;
            }

            // Obtenir informació dels microorganismes i mecanismes de la mostra
            var microorganismes = mostra.Microorganismes;
            var mecanismes = mostra.MecanismesResistencia;

            string infoMicroorganismes = microorganismes.Any() ? string.Join(", ", microorganismes) : null;
            string infoMecanismes = mecanismes.Any() ? string.Join(", ", mecanismes) : null;

            string observacionsCompletes = observacions;
            if (!string.IsNullOrEmpty(infoMicroorganismes) || !string.IsNullOrEmpty(infoMecanismes))
            {
                var detalls = new List<string>();
                if (!string.IsNullOrEmpty(infoMicroorganismes))
                    detalls.Add($"Microorganismes: {infoMicroorganismes}");
                if (!string.IsNullOrEmpty(infoMecanismes))
                    detalls.Add($"Mecanismes: {infoMecanismes}");

                string detallsText = string.Join("; ", detalls);
                observacionsCompletes = string.IsNullOrEmpty(observacions) ? detallsText : $"{observacions}. {detallsText}";
            }

            return GuardarHistorialMostra(
                mostra.EtiquetaId,
                tipusCanvi,
                observacionsCompletes,
                infoMicroorganismes,
                infoMecanismes
            );
        }

        /// <summary>
        /// Neteja registres d'historial més antics de X dies (per manteniment)
        /// </summary>
        /// <param name="diesRetencio">Dies de retenció (per defecte 90 dies)</param>
        /// <returns>Nombre de registres eliminats</returns>
        public int NetejarHistorialAntic(int diesRetencio = 90)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"UPDATE pacients_diagnostics_mostra_historial 
                                  SET dt_delete = NOW()
                                  WHERE data_canvi < DATE_SUB(NOW(), INTERVAL @diesRetencio DAY)
                                  AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diesRetencio", diesRetencio);
                        int registresNetejats = cmd.ExecuteNonQuery();

                        if (registresNetejats > 0)
                        {
                            Logger.Info($"Netejats {registresNetejats} registres d'historial anteriors a {diesRetencio} dies");
                        }

                        return registresNetejats;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error netejant historial antic: {ex.Message}", ex);
                return 0;
            }
        }
    }
}