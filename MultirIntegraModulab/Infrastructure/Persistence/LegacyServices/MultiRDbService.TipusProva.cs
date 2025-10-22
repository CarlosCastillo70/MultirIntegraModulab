using MultirIntegraModulab.Domain.Entities;
using MySql.Data.MySqlClient;
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

                    Logger.Info($"🔎 Comprovant / creant tipus prova a taula tipusprova: {codiProva}");

                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiProva", codiProva);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count > 0)
                        {
                            Logger.Info($"  Tipus prova {codiProva} ja existeix a tipusprova");
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

                    Logger.Info($" El tipus de prova {codiProva} no existeix, es procedeix a crear-lo");

                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiProva", codiProva);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            Logger.Info($" ✔️ Tipus prova {codiProva} creat correctament");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($" ⚠️ No s'ha pogut crear el tipus prova a tipusprova");
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
    }
}
