using MultirIntegraModulab.Application.Helpers;
using MultirIntegraModulab.Domain.Entities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService per gestionar la vigència dels diagnòstics
    /// </summary>
    public partial class MultiRDbService
    {
        #region Vigència de Diagnòstics

        /// <summary>
        /// Marca un diagnòstic com a no vigent
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <param name="responsable">Usuari que marca com a no vigent</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        public bool MarcarDiagnosticNoVigent(int diagnosticId, string responsable)
        {
            if (diagnosticId <= 0)
            {
                Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}MarcarDiagnosticNoVigent: diagnosticId invàlid ({diagnosticId})");
                return false;
            }

            if (string.IsNullOrWhiteSpace(responsable))
            {
                Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}MarcarDiagnosticNoVigent: responsable és null o buit");
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
                            responsable_no_vigent = @responsable,
                            data_no_vigent = NOW(),
                            dt_update = NOW()
                        WHERE id = @diagnosticId
                          AND dt_delete IS NULL
                          AND vigent = 'S'";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);
                        cmd.Parameters.AddWithValue("@responsable", responsable);

                        int filesAfectades = cmd.ExecuteNonQuery();

                        if (filesAfectades > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Diagnòstic {diagnosticId} marcat com a NO vigent per {responsable}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut marcar diagnòstic {diagnosticId} com a no vigent (ja era no vigent o no existeix)");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marcant diagnòstic {diagnosticId} com a no vigent: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Reactiva un diagnòstic (el torna a marcar com a vigent)
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <param name="responsable">Usuari que reactiva</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        public bool ReactivarDiagnostic(int diagnosticId, string responsable)
        {
            if (diagnosticId <= 0)
            {
                Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ReactivarDiagnostic: diagnosticId invàlid ({diagnosticId})");
                return false;
            }

            if (string.IsNullOrWhiteSpace(responsable))
            {
                Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ReactivarDiagnostic: responsable és null o buit");
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
                            responsable_no_vigent = NULL,
                            data_no_vigent = NULL,
                            dt_update = NOW()
                        WHERE id = @diagnosticId
                          AND dt_delete IS NULL
                          AND vigent = 'N'";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);

                        int filesAfectades = cmd.ExecuteNonQuery();

                        if (filesAfectades > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Diagnòstic {diagnosticId} reactivat per {responsable}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut reactivar diagnòstic {diagnosticId} (ja era vigent o no existeix)");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reactivant diagnòstic {diagnosticId}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Obté els diagnòstics actius (vigents) d'un pacient amb el darrer positiu associat
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient (npat)</param>
        /// <returns>Llista de diagnòstics actius amb informació del darrer positiu</returns>
        public List<DiagnosticActiuPacient> ObtenirDiagnosticsActiusPacient(string pacientSap)
        {
            var diagnostics = new List<DiagnosticActiuPacient>();

            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ObtenirDiagnosticsActiusPacient: pacientSap és null o buit");
                return diagnostics;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}🔎 Obtenint diagnòstics actius del pacient {pacientSap}");

                    conn.Open();

                    // Query per obtenir els diagnòstics vigents amb el darrer positiu associat
                    string sql = @"
                        SELECT DISTINCT
                            pd.id AS diagnostic_id,
                            pd.npat,
                            pd.microorganisme,
                            pd.mecanisme,
                            pd.tipus_mecanisme,
                            pd.data_diagnostic,
                            -- Darrer positiu associat
                            MAX(pdm.data_mostra) AS data_darrer_positiu,
                            pdm_darrer.tipus_mostra_m AS tipus_mostra,
                            tm.descripcio AS descripcio_tipus_mostra,
                            -- Camps nota_curs_clinic
                            mec.nota_curs_clinic AS mecanisme_nota_curs_clinic,
                            micro.nota_curs_clinic AS microorganisme_nota_curs_clinic
                        FROM pacients_diagnostics pd
                            -- Relació amb mostres a través de mostra_microorganisme
                            INNER JOIN mostra_microorganisme mm ON pd.id = mm.pacient_diagnostic_id
                            INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                            -- Obtenir el darrer positiu
                            LEFT JOIN pacients_diagnostics_mostra pdm_darrer ON pdm_darrer.id = (
                                SELECT pdm_sub.id
                                FROM mostra_microorganisme mm_sub
                                    INNER JOIN pacients_diagnostics_mostra pdm_sub ON mm_sub.pacient_diagnostic_mostra_id = pdm_sub.id
                                WHERE mm_sub.pacient_diagnostic_id = pd.id
                                  AND pdm_sub.valoracio = '2'
                                  AND pdm_sub.dt_delete IS NULL
                                ORDER BY pdm_sub.data_mostra DESC
                                LIMIT 1
                            )
                            -- Tipus de mostra
                            LEFT JOIN tipusmostra_m tm ON pdm_darrer.tipus_mostra_m = tm.codi
                            -- Mecanisme
                            LEFT JOIN mecanismes mec ON pd.mecanisme = mec.codi AND mec.dt_delete IS NULL
                            -- Microorganisme
                            LEFT JOIN microorganismes micro ON pd.microorganisme = micro.codi AND micro.dt_delete IS NULL
                        WHERE pd.npat = @pacientSap
                          AND pd.vigent = 'S'
                          AND pd.dt_delete IS NULL
                          AND pdm.valoracio = '2'
                          AND pdm.dt_delete IS NULL
                        GROUP BY pd.id, pd.npat, pd.microorganisme, pd.mecanisme, pd.tipus_mecanisme, 
                                 pd.data_diagnostic, pdm_darrer.tipus_mostra_m, tm.descripcio,
                                 mec.nota_curs_clinic, micro.nota_curs_clinic
                        ORDER BY MAX(pdm.data_mostra) DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnostics.Add(new DiagnosticActiuPacient
                                {
                                    DiagnosticId = reader.GetInt32("diagnostic_id"),
                                    PacientSap = reader["npat"]?.ToString(),
                                    Microorganisme = reader["microorganisme"]?.ToString(),
                                    Mecanisme = reader["mecanisme"] != DBNull.Value ? reader["mecanisme"]?.ToString() : null,
                                    TipusMecanisme = reader["tipus_mecanisme"] != DBNull.Value ? reader["tipus_mecanisme"]?.ToString() : null,
                                    DataDiagnostic = reader["data_diagnostic"] as DateTime?,
                                    DataDarrerPositiu = reader["data_darrer_positiu"] as DateTime?,
                                    TipusMostra = reader["tipus_mostra"]?.ToString(),
                                    DescripcioTipusMostra = reader["descripcio_tipus_mostra"]?.ToString(),
                                    MecanismeNotaCursClinic = reader["mecanisme_nota_curs_clinic"] != DBNull.Value 
                                        ? Convert.ToBoolean(reader["mecanisme_nota_curs_clinic"]) 
                                        : (bool?)null,
                                    MicroorganismeNotaCursClinic = reader["microorganisme_nota_curs_clinic"] != DBNull.Value 
                                        ? Convert.ToBoolean(reader["microorganisme_nota_curs_clinic"]) 
                                        : (bool?)null
                                });
                            }
                        }
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Trobats {diagnostics.Count} diagnòstic(s) actiu(s) per al pacient {pacientSap}");

                    // Mostrar detall de cada diagnòstic
                    if (diagnostics.Count > 0)
                    {
                        foreach (var diag in diagnostics)
                        {
                            string infoMecanisme = !string.IsNullOrWhiteSpace(diag.Mecanisme) 
                                ? $" + {diag.Mecanisme}" 
                                : "";

                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}  • Diagnòstic {diag.DiagnosticId}: {diag.Microorganisme}{infoMecanisme} " +
                                       $"(Darrer positiu: {diag.DataDarrerPositiu:dd/MM/yyyy}, Tipus: {diag.TipusMostra})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint diagnòstics actius del pacient {pacientSap}: {ex.Message}", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// Confecciona la nota del curs clínic amb la llista de diagnòstics actius del pacient
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <returns>Nota formattejada amb els diagnòstics actius</returns>
        public string ConfeccionarNotaCursClinic(string pacientSap)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ConfeccionarNotaCursClinic: pacientSap és null o buit");
                return string.Empty;
            }

            try
            {
                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Confeccionant nota curs clínic per pacient {pacientSap}");

                // Obtenir els diagnòstics actius del pacient
                var diagnostics = ObtenirDiagnosticsActiusPacient(pacientSap);

                if (diagnostics == null || diagnostics.Count == 0)
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ℹ️ El pacient no té diagnòstics actius");
                    return string.Empty;
                }

                // Confeccionar la nota
                var nota = new StringBuilder();
                nota.AppendLine("DIAGNÒSTICS ACTIUS:");
                nota.AppendLine();

                foreach (var diagnostic in diagnostics)
                {
                    // Tipus de mostra
                    string tipusMostra = !string.IsNullOrWhiteSpace(diagnostic.DescripcioTipusMostra)
                        ? diagnostic.DescripcioTipusMostra
                        : diagnostic.TipusMostra ?? "N/D";

                    // Microorganisme
                    string microorganisme = diagnostic.Microorganisme ?? "N/D";

                    // Mecanisme de resistència (opcional)
                    string mecanisme = !string.IsNullOrWhiteSpace(diagnostic.Mecanisme)
                        ? $" + {diagnostic.Mecanisme}"
                        : "";

                    // Data del darrer positiu
                    string dataPositiu = diagnostic.DataDarrerPositiu.HasValue
                        ? diagnostic.DataDarrerPositiu.Value.ToString("dd/MM/yyyy")
                        : "N/D";

                    // Afegir línia del diagnòstic
                    nota.AppendLine($"- {microorganisme}{mecanisme}");
                    nota.AppendLine($"  Tipus mostra: {tipusMostra}");
                    nota.AppendLine($"  Darrer positiu: {dataPositiu}");
                    
                    // Afegir notes si cal
                    bool teNotaClinica = (diagnostic.MecanismeNotaCursClinic.HasValue && diagnostic.MecanismeNotaCursClinic.Value) ||
                                        (diagnostic.MicroorganismeNotaCursClinic.HasValue && diagnostic.MicroorganismeNotaCursClinic.Value);

                    if (teNotaClinica)
                    {
                        nota.AppendLine($"  ⚠️ Requereix seguiment clínic");
                    }

                    nota.AppendLine();
                }

                string notaFinal = nota.ToString().TrimEnd();

                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Nota confeccionada: {diagnostics.Count} diagnòstic(s)");

                return notaFinal;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error confeccionant nota curs clínic per pacient {pacientSap}: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Afegeix una nota al curs clínic del pacient si s'han creat nous diagnòstics positius
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="sShanAfegitPositius">Indica si s'han afegit positius en el processament</param>
        /// <returns>True si s'ha inserit la nota, false en cas contrari</returns>
        public bool AfegirNotaCursClinicSiCal(string pacientSap, bool sShanAfegitPositius)
        {
            if (!sShanAfegitPositius)
            {
                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ℹ️ No s'han afegit positius. No cal afegir nota al curs clínic");
                return false;
            }

            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("AfegirNotaCursClinicSiCal: pacientSap és null o buit");
                return false;
            }

            try
            {
                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📋 S'han afegit positius. Procedint a afegir nota al curs clínic...");

                // Confeccionar la nota
                string nota = ConfeccionarNotaCursClinic(pacientSap);

                if (string.IsNullOrWhiteSpace(nota))
                {
                    Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ La nota està buida. No s'afegirà al curs clínic");
                    return false;
                }

                // Inserir la nota - cridar el mètode local
                bool inserit = this.InserirNotaCursClinic(pacientSap, nota);

                if (inserit)
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Nota afegida correctament al curs clínic del pacient {pacientSap}");
                }

                return inserit;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error afegint nota curs clínic per pacient {pacientSap}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Insereix una nota al curs clínic d'un pacient
        /// </summary>
        /// <param name="npat">Identificador del pacient</param>
        /// <param name="nota">Contingut de la nota</param>
        /// <returns>True si s'ha inserit correctament</returns>
        public bool InserirNotaCursClinic(string npat, string nota)
        {
            if (string.IsNullOrWhiteSpace(npat))
            {
                Logger.Warning("InserirNotaCursClinic: npat és null o buit");
                return false;
            }

            if (string.IsNullOrWhiteSpace(nota))
            {
                Logger.Warning("InserirNotaCursClinic: nota és null o buida");
                return false;
            }

            try
            {
                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Inserint nota curs clínic per pacient {npat}");

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        INSERT INTO notes_curs_clinic 
                            (npat, nota, dt_create, dt_update, enviada) 
                        VALUES 
                            (@npat, @nota, NOW(), NOW(), 1)";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@npat", npat);
                        cmd.Parameters.AddWithValue("@nota", nota);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Nota curs clínic inserida correctament ({rowsAffected} registre)");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha inserit cap nota curs clínic");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error inserint nota curs clínic per pacient {npat}: {ex.Message}", ex);
                return false;
            }
        }

        #endregion
    }
}
