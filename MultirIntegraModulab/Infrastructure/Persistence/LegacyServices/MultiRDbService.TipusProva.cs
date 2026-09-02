using MultirIntegraModulab.Application.Helpers;
using MultirIntegraModulab.Domain.Entities;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using System;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService per gestionar la taula tipusprova
    /// </summary>
    public partial class MultiRDbService
    {
        /// <summary>
        /// Comprova si existeix un tipus de prova a la taula tipusprova
        /// </summary>
        /// <param name="codiProva">Codi de la prova (PROVA_DESCRIPCIO de Modulab)</param>
        /// <returns>True si existeix i està actiu, False en cas contrari</returns>
        public bool ExisteixTipusProvaActiu(string codiProva)
        {
            if (string.IsNullOrWhiteSpace(codiProva))
            {
                Logger.Warning("Intentant comprovar tipus prova amb codi null o buit");
                return false;
            }

            string sql = @"
                SELECT COUNT(*) 
                FROM tipusprova 
                WHERE UPPER(codi) = UPPER(@codiProva) 
                  AND actiu = 1";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔎 Comprovant / creant tipus prova a taula tipusprova: '{codiProva}'");

                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiProva", codiProva);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✓ Tipus prova '{codiProva}' JA existeix a tipusprova");
                        }
                        else
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Tipus prova '{codiProva}' NO existeix a tipusprova, es procedeix a crear-lo");
                        }

                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant existència tipus prova: {codiProva}", ex);
                return false;
            }
        }

        /// <summary>
        /// Crea un nou tipus de prova a la taula tipusprova
        /// </summary>
        /// <param name="codiProva">Codi de la prova (PROVA_DESCRIPCIO de Modulab)</param>
        /// <returns>True si s'ha creat correctament, False en cas contrari</returns>
        public bool CrearTipusProva(string codiProva)
        {
            if (string.IsNullOrWhiteSpace(codiProva))
            {
                Logger.Warning("Intentant crear tipus prova amb codi null o buit");
                return false;
            }

            string sql = @"
                INSERT INTO tipusprova 
                    (codi, descripcio, comportament, dt_create, dt_update, actiu) 
                VALUES 
                    (@codiProva, @codiProva, 0, NOW(), NOW(), 1)";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}El tipus de prova {codiProva} no existeix, es procedeix a crear-lo");

                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiProva", codiProva);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Tipus prova {codiProva} creat correctament");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut crear el tipus prova a tipusprova");
                        }

                        return false;
                    }
                }
            }
            catch (MySqlException ex) when (ex.Number == 1062) // Duplicate entry
            {
                Logger.Warning($"Tipus prova duplicat detectat: {codiProva}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creant tipus prova: {codiProva}", ex);
                return false;
            }
        }

        /// <summary>
        /// Comprova si un tipus de prova permet incorporar virus respiratoris
        /// </summary>
        /// <param name="codiProva">Codi de la prova (PROVA_DESCRIPCIO de Modulab)</param>
        /// <returns>True si incorpora_virus_respiratori = 1, False en cas contrari o si no existeix</returns>
        public bool TipusProvaPermitIncorporarVirusRespiratori(string codiProva)
        {
            if (string.IsNullOrWhiteSpace(codiProva))
            {
                Logger.Warning("Intentant comprovar tipus prova VR amb codi null o buit");
                return false;
            }

            string sql = @"
                SELECT incorpora_virus_respiratori 
                FROM tipusprova 
                WHERE UPPER(codi) = UPPER(@codiProva) 
                  AND actiu = 1";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiProva", codiProva);

                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            int incorporaVR = Convert.ToInt32(result);
                            return incorporaVR == 1;
                        }

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant incorporació VR per tipus prova: {codiProva}", ex);
                return false;
            }
        }

        /// <summary>
        /// Comprova si un tipus de prova és MDO (Malaltia de Declaració Obligatòria)
        /// </summary>
        /// <param name="codiProva">Codi de la prova (PROVA_DESCRIPCIO de Modulab)</param>
        /// <param name="shortDescription1">Valor de SHORTDESCRIPTION1 del resultat ('P' = Positiu)</param>
        /// <returns>True si és MDO (incorpora_mdo = 1 i resultat positiu, o incorpora_mdo = 2), False en cas contrari</returns>
        public bool TipusProvaEsMDO(string codiProva, string shortDescription1)
        {
            if (string.IsNullOrWhiteSpace(codiProva))
            {
                Logger.Warning("Intentant comprovar tipus prova MDO amb codi null o buit");
                return false;
            }

            string sql = @"
                SELECT incorpora_mdo 
                FROM tipusprova 
                WHERE UPPER(codi) = UPPER(@codiProva) 
                  AND actiu = 1";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiProva", codiProva);

                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            int incorporaMdo = Convert.ToInt32(result);
                            
                            // incorpora_mdo = 0 -> NO és MDO
                            if (incorporaMdo == 0)
                            {
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)} Tipus prova: '{codiProva}' Incorpora_mdo: '0 - NO és MDO'");
                                return false;
                            }

                            // incorpora_mdo = 1 -> És MDO només si el resultat és positiu
                            if (incorporaMdo == 1)
                            {
                                bool esPositiu = !string.IsNullOrWhiteSpace(shortDescription1) &&
                                                 shortDescription1.Trim().ToUpper() == "P";

                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)} Tipus prova: '{codiProva}' Incorpora_mdo: '1 - Incorpora si resultat {shortDescription1} és positiu'");
                                return esPositiu;
                            }

                            // incorpora_mdo = 2 -> SEMPRE és MDO (independentment del resultat)
                            if (incorporaMdo == 2)
                            {
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)} Tipus prova: '{codiProva}' Incorpora_mdo: '2 - Incorpora sempre'. Resultat: {shortDescription1}");
                                return true;
                            }

                        }

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant MDO per tipus prova: {codiProva}", ex);
                return false;
            }
        }
    }
}
