using MultirIntegraModulab.Application.Helpers;
using MultirIntegraModulab.Domain.Entities;
using MySql.Data.MySqlClient;
using System;

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

        #endregion
    }
}
