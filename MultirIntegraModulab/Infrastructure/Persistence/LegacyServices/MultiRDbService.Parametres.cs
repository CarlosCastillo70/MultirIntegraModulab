using MultirIntegraModulab.Application.Helpers;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService per gestionar paràmetres de l'aplicació
    /// </summary>
    public partial class MultiRDbService
    {
        /// <summary>
        /// Comprova si un valor està a la llista de paràmetres actius d'una categoria
        /// </summary>
        /// <param name="categoria">Categoria del paràmetre (ex: VR_CENTRES)</param>
        /// <param name="valor">Valor a comprovar (ex: nom del centre)</param>
        /// <returns>True si el valor està a la llista i està actiu</returns>
        public bool ExisteixParametre(string categoria, string valor)
        {
            if (string.IsNullOrWhiteSpace(categoria) || string.IsNullOrWhiteSpace(valor))
            {
                Logger.Warning("Intentant comprovar paràmetre amb categoria o valor buit");
                return false;
            }

            string sql = @"
                SELECT COUNT(*) 
                FROM parametres_aplicacio 
                WHERE categoria = @categoria
                  AND UPPER(clau) = UPPER(@valor)
                  AND actiu = 1
                  AND dt_delete IS NULL";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@categoria", categoria);
                        cmd.Parameters.AddWithValue("@valor", valor);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant paràmetre {categoria}.{valor}", ex);
                return false; // Per defecte, si hi ha error, NO existeix (conservador)
            }
        }

        /// <summary>
        /// Obté el valor d'un paràmetre de l'aplicació
        /// </summary>
        /// <param name="categoria">Categoria del paràmetre</param>
        /// <param name="clau">Clau del paràmetre</param>
        /// <returns>Valor del paràmetre o null si no existeix</returns>
        public string ObtenirParametre(string categoria, string clau)
        {
            if (string.IsNullOrWhiteSpace(categoria) || string.IsNullOrWhiteSpace(clau))
            {
                Logger.Warning("Intentant obtenir paràmetre amb categoria o clau buida");
                return null;
            }

            string sql = @"
                SELECT valor 
                FROM parametres_aplicacio 
                WHERE categoria = @categoria
                  AND clau = @clau
                  AND actiu = 1
                  AND dt_delete IS NULL
                LIMIT 1";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@categoria", categoria);
                        cmd.Parameters.AddWithValue("@clau", clau);

                        object result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint paràmetre {categoria}.{clau}", ex);
                return null;
            }
        }

        /// <summary>
        /// Obté tots els paràmetres actius d'una categoria (retorna les claus)
        /// </summary>
        /// <param name="categoria">Categoria dels paràmetres</param>
        /// <returns>Llista de claus dels paràmetres actius</returns>
        public List<string> ObtenirParametresPerCategoria(string categoria)
        {
            var parametres = new List<string>();

            if (string.IsNullOrWhiteSpace(categoria))
            {
                Logger.Warning("Intentant obtenir paràmetres amb categoria buida");
                return parametres;
            }

            string sql = @"
                SELECT clau 
                FROM parametres_aplicacio 
                WHERE categoria = @categoria
                  AND actiu = 1
                  AND dt_delete IS NULL
                ORDER BY clau";

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@categoria", categoria);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                parametres.Add(reader.GetString("clau"));
                            }
                        }
                    }
                }

                Logger.Info($"Carregats {parametres.Count} paràmetres de la categoria '{categoria}'");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint paràmetres de categoria {categoria}", ex);
            }

            return parametres;
        }
    }
}
