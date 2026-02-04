using MultirIntegraModulab.Application.Helpers;
using MySql.Data.MySqlClient;
using System;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService per gestionar la taula pacients_diagnostics_mostra
    /// </summary>
    public partial class MultiRDbService
    {
        /// <summary>
        /// Obté l'ID de la mostra diagnòstic associada a un diagnòstic amb una etiqueta i tipus específics
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <param name="etiqueta">Etiqueta de la mostra</param>
        /// <param name="tipusMostra">Tipus de mostra</param>
        /// <returns>ID de la mostra diagnòstic, o 0 si no existeix</returns>
        public int ObtenirIdMostraDiagnosticPerDiagnosticIEtiqueta(int diagnosticId, string etiqueta, string tipusMostra)
        {
            if (diagnosticId <= 0)
            {
                Logger.Warning($"ObtenirIdMostraDiagnosticPerDiagnosticIEtiqueta: diagnosticId invàlid ({diagnosticId})");
                return 0;
            }

            if (string.IsNullOrWhiteSpace(etiqueta))
            {
                Logger.Warning("ObtenirIdMostraDiagnosticPerDiagnosticIEtiqueta: etiqueta és null o buida");
                return 0;
            }

            if (string.IsNullOrWhiteSpace(tipusMostra))
            {
                Logger.Warning("ObtenirIdMostraDiagnosticPerDiagnosticIEtiqueta: tipusMostra és null o buit");
                return 0;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT pdm.id 
                        FROM mostra_microorganisme mm
                        INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                        WHERE mm.pacient_diagnostic_id = @diagnosticId
                          AND pdm.etiqueta = @etiqueta
                          AND pdm.tipus_mostra_m = @tipusMostra
                          AND pdm.dt_delete IS NULL
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);
                        cmd.Parameters.AddWithValue("@etiqueta", etiqueta);
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra);

                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint ID mostra diagnòstic per diagnòstic {diagnosticId}, etiqueta {etiqueta} i tipus {tipusMostra}: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Actualitza el camp microorganisme_mecanisme_captat d'una mostra diagnòstic existent.
        /// Si el camp ja té valor, concatena el nou valor amb una coma.
        /// </summary>
        /// <param name="mostraDiagnosticId">ID de la mostra diagnòstic</param>
        /// <param name="nouMicroorganismeMecanisme">Nou valor a afegir</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        public bool ActualitzarMicroorganismeMecanismeCaptat(int mostraDiagnosticId, string nouMicroorganismeMecanisme)
        {
            if (mostraDiagnosticId <= 0)
            {
                Logger.Error($"ActualitzarMicroorganismeMecanismeCaptat: mostraDiagnosticId invàlid ({mostraDiagnosticId})");
                return false;
            }

            if (string.IsNullOrWhiteSpace(nouMicroorganismeMecanisme))
            {
                Logger.Warning("ActualitzarMicroorganismeMecanismeCaptat: nouMicroorganismeMecanisme és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Obtenir el valor actual del camp microorganisme_mecanisme_captat
                    string valorActual = null;
                    
                    string sqlSelect = @"
                        SELECT microorganisme_mecanisme_captat 
                        FROM pacients_diagnostics_mostra 
                        WHERE id = @mostraDiagnosticId 
                          AND dt_delete IS NULL";

                    using (var cmdSelect = new MySqlCommand(sqlSelect, conn))
                    {
                        cmdSelect.Parameters.AddWithValue("@mostraDiagnosticId", mostraDiagnosticId);
                        
                        var result = cmdSelect.ExecuteScalar();
                        valorActual = result?.ToString();
                    }

                    // 2. Determinar el nou valor a guardar
                    string nouValor;
                    
                    if (string.IsNullOrWhiteSpace(valorActual))
                    {
                        // Si el camp està buit, només guardar el nou valor
                        nouValor = nouMicroorganismeMecanisme.Trim();
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Camp 'microorganisme_mecanisme_captat' buit. Guardant: '{nouValor}'");
                    }
                    else
                    {
                        // Si ja té valor, concatenar amb una coma
                        // Comprovar primer si el nou valor ja està present per evitar duplicats
                        if (valorActual.Contains(nouMicroorganismeMecanisme.Trim()))
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}El valor '{nouMicroorganismeMecanisme}' ja està present al camp. No es concatena.");
                            return true; // No cal actualitzar, però retornem true perquè no és un error
                        }
                        
                        nouValor = $"{valorActual}, {nouMicroorganismeMecanisme.Trim()}";
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Camp 'microorganisme_mecanisme_captat' existent: '{valorActual}'. Concatenant: '{nouValor}'");
                    }

                    // 3. Actualitzar el camp
                    string sqlUpdate = @"
                        UPDATE pacients_diagnostics_mostra 
                        SET microorganisme_mecanisme_captat = @nouValor,
                            dt_update = NOW()
                        WHERE id = @mostraDiagnosticId 
                          AND dt_delete IS NULL";

                    using (var cmdUpdate = new MySqlCommand(sqlUpdate, conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@mostraDiagnosticId", mostraDiagnosticId);
                        cmdUpdate.Parameters.AddWithValue("@nouValor", nouValor);

                        int rowsAffected = cmdUpdate.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Actualitzat 'microorganisme_mecanisme_captat' per mostra diagnòstic ID {mostraDiagnosticId}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ No s'ha actualitzat cap registre per mostra diagnòstic ID {mostraDiagnosticId}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error actualitzant microorganisme_mecanisme_captat per mostra diagnòstic ID {mostraDiagnosticId}: {ex.Message}", ex);
                return false;
            }
        }
    }
}
