using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Enums;
using MultirIntegraModulab.Application.Helpers;
using MySql.Data.MySqlClient;
using System;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService per gestionar la taula mostra_microorganisme
    /// </summary>
    public partial class MultiRDbService
    {
        /// <summary>
        /// Comprova si existeix un registre a la taula mostra_microorganisme
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic (pacient_diagnostic_id)</param>
        /// <param name="mostraDiagnosticId">ID de la mostra diagnòstic (pacient_diagnostic_mostra_id)</param>
        /// <returns>True si existeix, False en cas contrari</returns>
        public bool ComprovarMostraMicroorganismeExisteix(int diagnosticId, int mostraDiagnosticId)
        {
            string sql = @"
                SELECT COUNT(*) 
                FROM mostra_microorganisme 
                WHERE pacient_diagnostic_id = @diagnosticId 
                  AND pacient_diagnostic_mostra_id = @mostraDiagnosticId";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"🔎 Comprovant / creant registre mostra_microorganisme {diagnosticId} {mostraDiagnosticId}");

                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);
                        cmd.Parameters.AddWithValue("@mostraDiagnosticId", mostraDiagnosticId);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Registre de mostra microorganisme {diagnosticId}, {mostraDiagnosticId} : {(count > 0 ? $"JA existeix" : "NO existeix")}");

                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant existència mostra_microorganisme " +
                    $"(diagnostic: {diagnosticId}, mostra: {mostraDiagnosticId})", ex);
                return false; // En cas d'error, assumim que no existeix
            }
        }

        /// <summary>
        /// Crea un nou registre a la taula mostra_microorganisme
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic (pacient_diagnostic_id)</param>
        /// <param name="mostraDiagnosticId">ID de la mostra diagnòstic (pacient_diagnostic_mostra_id)</param>
        /// <returns>True si s'ha creat correctament, False en cas contrari</returns>
        public bool CrearMostraMicroorganisme(int diagnosticId, int mostraDiagnosticId)
        {
            string sql = @"
                INSERT INTO mostra_microorganisme 
                    (pacient_diagnostic_id, pacient_diagnostic_mostra_id) 
                VALUES 
                    (@diagnosticId, @mostraDiagnosticId)";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Es procedeix a crear el registre mostra_microorganisme");

                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);
                        cmd.Parameters.AddWithValue("@mostraDiagnosticId", mostraDiagnosticId);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Creat registre a mostra_microorganisme : diagnostic_id = {diagnosticId}, mostra_diagnostic_id = {mostraDiagnosticId}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠ No s'ha pogut crear el registre mostra_microorganisme");
                        }


                        return false;
                    }
                }
            }
            catch (MySqlException ex) when (ex.Number == 1062) // Duplicate entry
            {
                Logger.Warning($"Registre mostra_microorganisme duplicat detectat: " +
                    $"diagnostic_id={diagnosticId}, mostra_diagnostic_id={mostraDiagnosticId}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creant registre mostra_microorganisme " +
                    $"(diagnostic: {diagnosticId}, mostra: {mostraDiagnosticId})", ex);
                return false;
            }
        }

        /// <summary>
        /// Actualitza la data_diagnostic amb la data de mostra més antiga per aquest diagnòstic
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient (NPAT)</param>
        /// <param name="microorganismeCodi">Codi del microorganisme</param>
        /// <param name="mecanismeId">ID del mecanisme de resistència</param>
        /// <param name="tipusMecanisme">Tipus/descripció del mecanisme</param>
        /// <returns>True si s'ha actualitzat correctament, False en cas contrari</returns>
        public bool ActualitzarDataDiagnosticPacientsDiagnostics(
            string pacientSap, 
            string microorganismeCodi, 
            string mecanismeId, 
            string tipusMecanisme)
        {
            // Determinar si hem de comparar amb NULL o amb valor
            // Si mecanismeId o tipusMecanisme són null, a la BD estan emmagatzemats com a ''
            bool mecanismeEsNullOBuit = string.IsNullOrWhiteSpace(mecanismeId);
            bool tipusMecanismeEsNullOBuit = string.IsNullOrWhiteSpace(tipusMecanisme);

            string sql = @"
                UPDATE pacients_diagnostics  
                SET data_diagnostic = (  
                    SELECT MIN(pdm.data_mostra)  
                    FROM mostra_microorganisme mm  
                    JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id 	 
                    WHERE mm.pacient_diagnostic_id = pacients_diagnostics.id  
                )  
                WHERE npat = @pacientSap
                  AND microorganisme = @microorganismeCodi
                  AND (mecanisme = @mecanismeId OR (mecanisme = '' AND @mecanismeEsNullOBuit = 1))
                  AND (tipus_mecanisme = @tipusMecanisme OR (tipus_mecanisme = '' AND @tipusMecanismeEsNullOBuit = 1))
                  AND dt_delete IS NULL";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"🔄 Actualitzant data_diagnostic (de pacients_diagnostics) per pacient {pacientSap}");

                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@microorganismeCodi", microorganismeCodi);
                        cmd.Parameters.AddWithValue("@mecanismeId", mecanismeEsNullOBuit ? "" : mecanismeId);
                        cmd.Parameters.AddWithValue("@tipusMecanisme", tipusMecanismeEsNullOBuit ? "" : tipusMecanisme);
                        cmd.Parameters.AddWithValue("@mecanismeEsNullOBuit", mecanismeEsNullOBuit ? 1 : 0);
                        cmd.Parameters.AddWithValue("@tipusMecanismeEsNullOBuit", tipusMecanismeEsNullOBuit ? 1 : 0);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Data diagnòstic (pacients_diagnostics) actualitzada a {rowsAffected} registre(s), per pacient = {pacientSap}, microorganisme = '{microorganismeCodi}', mecanisme = '{mecanismeId ?? "(buit)"}'");
                            return true;
                        }
                        
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha actualitzat cap registre per pacient = {pacientSap}, microorganisme = {microorganismeCodi}, mecanisme = {mecanismeId ?? "(buit)"}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error actualitzant data diagnòstic (pacient: {pacientSap}, micro: {microorganismeCodi}, mec: {mecanismeId ?? "(buit)"})", ex);
                return false;
            }
        }

        /// <summary>
        /// Actualitza la data_diagnostic de pacients_diagnostics_mostra amb la data de mostra més antiga
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient (NPAT)</param>
        /// <param name="microorganismeCodi">Codi del microorganisme</param>
        /// <param name="mecanismeId">ID del mecanisme de resistència</param>
        /// <param name="tipusMecanisme">Tipus/descripció del mecanisme</param>
        /// <returns>True si s'ha actualitzat correctament, False en cas contrari</returns>
        public bool ActualitzarDataDiagnosticPacientsDiagnosticsMostra(
            string pacientSap, 
            string microorganismeCodi, 
            string mecanismeId, 
            string tipusMecanisme)
        {
            // Determinar si hem de comparar amb NULL o amb valor
            // Si mecanismeId o tipusMecanisme són null, a la BD estan emmagatzemats com a ''
            bool mecanismeEsNullOBuit = string.IsNullOrWhiteSpace(mecanismeId);
            bool tipusMecanismeEsNullOBuit = string.IsNullOrWhiteSpace(tipusMecanisme);

            string sql = @"
                UPDATE pacients_diagnostics_mostra pdm  
                SET data_diagnostic = (  
                    SELECT MIN(pdm_sub.data_mostra)  
                    FROM pacients_diagnostics_mostra pdm_sub  
                    JOIN mostra_microorganisme mm ON mm.pacient_diagnostic_mostra_id = pdm_sub.id 		
                    WHERE mm.pacient_diagnostic_id IN (  
                        SELECT id  
                        FROM pacients_diagnostics  
                        WHERE npat = @pacientSap  
                          AND microorganisme = @microorganismeCodi 
                          AND (mecanisme = @mecanismeId OR (mecanisme = '' AND @mecanismeEsNullOBuit = 1))
                          AND (tipus_mecanisme = @tipusMecanisme OR (tipus_mecanisme = '' AND @tipusMecanismeEsNullOBuit = 1))
                          AND dt_delete IS NULL 
                    )  
                )  
                WHERE pdm.id IN (  
                    SELECT pdm_sub.id  
                    FROM pacients_diagnostics_mostra pdm_sub  
                    JOIN mostra_microorganisme mm ON mm.pacient_diagnostic_mostra_id = pdm_sub.id 		
                    WHERE mm.pacient_diagnostic_id IN (  
                        SELECT id  
                        FROM pacients_diagnostics  
                        WHERE npat = @pacientSap2  
                          AND microorganisme = @microorganismeCodi2 
                          AND (mecanisme = @mecanismeId2 OR (mecanisme = '' AND @mecanismeEsNullOBuit2 = 1))
                          AND (tipus_mecanisme = @tipusMecanisme2 OR (tipus_mecanisme = '' AND @tipusMecanismeEsNullOBuit2 = 1))
                          AND dt_delete IS NULL  
                    )  
                )";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"🔄 Actualitzant data_diagnostic (de pacients_diagnostics_mostra) per pacient {pacientSap}");

                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        // Paràmetres per la subconsulta del SET
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@microorganismeCodi", microorganismeCodi);
                        cmd.Parameters.AddWithValue("@mecanismeId", mecanismeEsNullOBuit ? "" : mecanismeId);
                        cmd.Parameters.AddWithValue("@tipusMecanisme", tipusMecanismeEsNullOBuit ? "" : tipusMecanisme);
                        cmd.Parameters.AddWithValue("@mecanismeEsNullOBuit", mecanismeEsNullOBuit ? 1 : 0);
                        cmd.Parameters.AddWithValue("@tipusMecanismeEsNullOBuit", tipusMecanismeEsNullOBuit ? 1 : 0);
                        
                        // Paràmetres per la subconsulta del WHERE (MySQL no permet reutilitzar paràmetres)
                        cmd.Parameters.AddWithValue("@pacientSap2", pacientSap);
                        cmd.Parameters.AddWithValue("@microorganismeCodi2", microorganismeCodi);
                        cmd.Parameters.AddWithValue("@mecanismeId2", mecanismeEsNullOBuit ? "" : mecanismeId);
                        cmd.Parameters.AddWithValue("@tipusMecanisme2", tipusMecanismeEsNullOBuit ? "" : tipusMecanisme);
                        cmd.Parameters.AddWithValue("@mecanismeEsNullOBuit2", mecanismeEsNullOBuit ? 1 : 0);
                        cmd.Parameters.AddWithValue("@tipusMecanismeEsNullOBuit2", tipusMecanismeEsNullOBuit ? 1 : 0);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Data diagnòstic (pacients_diagnostics_mostra) actualitzada a {rowsAffected} registre(s), per pacient = {pacientSap}, microorganisme = {microorganismeCodi}, mecanisme = {mecanismeId ?? "(buit)"}");
                            return true;
                        }
                        
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha actualitzat cap registre de pacients_diagnostics_mostra per pacient={pacientSap}, micro={microorganismeCodi}, mec={mecanismeId ?? "(buit)"}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error actualitzant data diagnòstic de pacients_diagnostics_mostra (pacient: {pacientSap}, micro: {microorganismeCodi}, mec: {mecanismeId ?? "(buit)"})", ex);
                return false;
            }
        }
    }
}