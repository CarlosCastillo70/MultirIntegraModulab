using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Application.Helpers;
using MySql.Data.MySqlClient;
using System;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService per gestionar la taula tipusmostra_m
    /// </summary>
    public partial class MultiRDbService
    {
        /// <summary>
        /// Comprova si existeix un tipus de mostra a la taula tipusmostra_m
        /// </summary>
        /// <param name="codiMostra">Codi de la mostra (MOSTRA_DESCRIPCIO de Modulab)</param>
        /// <returns>True si existeix i està actiu, False en cas contrari</returns>
        public bool ExisteixTipusMostraMactiu(string codiMostra)
        {
            if (string.IsNullOrWhiteSpace(codiMostra))
            {
                Logger.Warning("Intentant comprovar tipus mostra amb codi null o buit");
                return false;
            }

            string sql = @"
                SELECT COUNT(*) 
                FROM tipusmostra_m 
                WHERE UPPER(codi) = UPPER(@codiMostra) 
                  AND actiu = 1";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"🔎 Comprovant / creant tipus mostra a tipusmostra_m: '{codiMostra}'");

                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiMostra", codiMostra);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Tipus mostra '{codiMostra}' JA existeix a tipusmostra_m");
                        }
                        else
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}El tipus de mostra '{codiMostra}' NO existeix, es procedeix a crear-lo");
                        }

                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Error comprovant existència tipus mostra_m: '{codiMostra}'", ex);
                return false;
            }
        }

        /// <summary>
        /// Crea un nou tipus de mostra a la taula tipusmostra_m
        /// </summary>
        /// <param name="codiMostra">Codi de la mostra (MOSTRA_DESCRIPCIO de Modulab)</param>
        /// <returns>True si s'ha creat correctament, False en cas contrari</returns>
        public bool CrearTipusMostraM(string codiMostra)
        {
            if (string.IsNullOrWhiteSpace(codiMostra))
            {
                Logger.Warning("Intentant crear tipus mostra amb codi null o buit");
                return false;
            }

            string sql = @"
                INSERT INTO tipusmostra_m 
                    (codi, descripcio, dt_create, dt_update, actiu, comportament, dies_vigencia_positiu) 
                VALUES 
                    (@codiMostra, @codiMostra, NOW(), NOW(), 1, 0, 455)";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiMostra", codiMostra);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Tipus mostra_m {codiMostra} creat correctament");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠ No s'ha pogut crear el tipus mostra a tipusmostra_m");
                        }

                        return false;
                    }
                }
            }
            catch (MySqlException ex) when (ex.Number == 1062) // Duplicate entry
            {
                Logger.Warning($"Tipus mostra_m duplicat detectat: {codiMostra}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creant tipus mostra_m: {codiMostra}", ex);
                return false;
            }
        }

        /// <summary>
        /// Obté el comportament d'un tipus de mostra
        /// </summary>
        /// <param name="codiMostra">Codi de la mostra (MOSTRA_DESCRIPCIO de Modulab)</param>
        /// <returns>El valor de comportament (0, 1, etc.) o null si no existeix o no està actiu</returns>
        public int? ObtenirComportamentTipusMostra(string codiMostra)
        {
            if (string.IsNullOrWhiteSpace(codiMostra))
            {
                Logger.Warning("Intentant obtenir comportament amb codi mostra null o buit");
                return null;
            }

            string sql = @"
                SELECT comportament  
                FROM tipusmostra_m 
                WHERE UPPER(codi) = UPPER(@codiMostra) 
                  AND dt_delete IS NULL 
                  AND actiu = 1";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiMostra", codiMostra);

                        var result = cmd.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            int comportament = Convert.ToInt32(result);
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Tipus mostra '{codiMostra}' té comportament {comportament}");
                            return comportament;
                        }
                        else
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Tipus mostra '{codiMostra}' no trobat o no actiu");
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Error obtenint comportament tipus mostra: {codiMostra}", ex);
                return null;
            }
        }

        /// <summary>
        /// Comprova si el pacient té algun diagnòstic positiu (per qualsevol tipus de mostra)
        /// Un diagnòstic és positiu si té valoració = '2'
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <returns>True si el pacient té almenys un diagnòstic positiu, False en cas contrari</returns>
        public bool PacientTePositiusAlgunTipusMostra(string pacientSap)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("Intentant comprovar positius amb pacientSap null o buit");
                return false;
            }

            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ Tipus de mostra amb comportament 1 (incorporar si el pacient té positius)");

            string sql = @"
                        SELECT COUNT(DISTINCT pd.id)  AS positius_algun_tipus_mostra 
                        FROM pacients_diagnostics_mostra pdm 
                            INNER JOIN mostra_microorganisme mm ON pdm.id = mm.pacient_diagnostic_mostra_id
                            INNER JOIN pacients_diagnostics pd ON mm.pacient_diagnostic_id = pd.id
                            INNER JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.codi
                        WHERE pdm.npat = @pacientSap 
                            AND pdm.valoracio = '2'
                            AND pdm.dt_delete IS NULL
                            AND pd.dt_delete IS NULL  
                            AND tm.dt_delete IS NULL";


            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        bool tePositius = count > 0;
                        
                        if (tePositius)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✓ Comprovació 1 COMPLERTA: Pacient {pacientSap} té {count} diagnòstics positius prèvis. SI cal incorporar el negatiu");
                        }
                        else
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Comprovació 1: Pacient {pacientSap} NO té positius previs → Continuar amb comprovació 2");
                        }
                        
                        return tePositius;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Error comprovant positius del pacient {pacientSap}", ex);
                return false;
            }
        }

        /// <summary>
        /// Comprova si el pacient té algun diagnòstic positiu vigent per un tipus de mostra específic
        /// i els seus tipus de mostra equivalents.
        /// Un positiu és vigent si no ha superat els dies_vigencia_positiu del tipus de mostra.
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra (MOSTRA_DESCRIPCIO)</param>
        /// <param name="etiquetaExcloure">Etiqueta a excloure de la cerca (opcional)</param>
        /// <returns>True si el pacient té almenys un positiu vigent per aquest tipus o equivalents</returns>
        public bool PacientTePositiusVigentsTipusMostraIEquivalents(string pacientSap, string tipusMostra, string etiquetaExcloure = null)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("Intentant comprovar positius vigents amb pacientSap null o buit");
                return false;
            }

            if (string.IsNullOrWhiteSpace(tipusMostra))
            {
                Logger.Warning("Intentant comprovar positius vigents amb tipusMostra null o buit");
                return false;
            }

            string sql = @"
                SELECT COUNT(*) AS positius_vigents_tipus_mostra_i_equivalents 
                FROM pacients_diagnostics_mostra pdm	 
                JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.descripcio 		 
                WHERE pdm.npat = @pacientSap
                  AND ( 
                    UPPER(tm.descripcio) = UPPER(@tipusMostra) 
                    OR tm.id IN ( 
                        SELECT tipusmostra_id_equivalent 
                        FROM tipusmostra_equivalents 
                        WHERE tipusmostra_id = ( 
                            SELECT id  
                            FROM tipusmostra_m tmm  
                            WHERE UPPER(tmm.descripcio) = UPPER(@tipusMostra) 
                        ) 
                    ) 
                  ) 
                  AND pdm.valoracio = '2' 
                  AND ( 
                    tm.dies_vigencia_positiu IS NULL 
                    OR pdm.data_mostra >= DATE_SUB(CURRENT_DATE, INTERVAL tm.dies_vigencia_positiu DAY) 
                  ) 
                  AND pdm.dt_delete IS NULL 
                  AND tm.dt_delete IS NULL";

            // Si s'especifica una etiqueta a excloure, afegir la condició
            if (!string.IsNullOrWhiteSpace(etiquetaExcloure))
            {
                sql += " AND (pdm.etiqueta <> @etiquetaExcloure OR pdm.etiqueta IS NULL)";
            }

            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Aplicant Comprovació 2: Positius vigents per aquest tipus de mostra '{tipusMostra}' o equivalents, i amb diferent etiqueta");

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra);
                        
                        if (!string.IsNullOrWhiteSpace(etiquetaExcloure))
                        {
                            cmd.Parameters.AddWithValue("@etiquetaExcloure", etiquetaExcloure);
                        }

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        bool tePositiusVigents = count > 0;
                        
                        if (tePositiusVigents)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Pacient {pacientSap} té {count} mostra(es) positiva(es) vigent(s) per tipus mostra '{tipusMostra}' o equivalents (Poden estar en un mateix diagnòstic)");
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚡ Pacient té positius vigents. SI cal incorporar el negatiu");
                        }
                        else
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Pacient NO té positius vigents per aquest tipus de mostra '{tipusMostra}' o equivalents, i amb diferent etiqueta");
                        }
                        
                        return tePositiusVigents;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠️ Error comprovant positius vigents del pacient {pacientSap} per tipus mostra {tipusMostra}", ex);
                return false;
            }
        }
    }
}
