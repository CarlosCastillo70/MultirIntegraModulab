using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab
{
    public partial class MultiRDbService : IDbService
    {
        private readonly string _connectionString;
        
        // Utilitzar ConcurrentDictionary per thread-safety
        private static ConcurrentDictionary<string, bool> _cacheMicroorganismesEspecials = new ConcurrentDictionary<string, bool>();
        private static DateTime _ultimaCarregaCache = DateTime.MinValue;
        private static readonly object _lockCache = new object();
        
        // Caché vàlida per 30 minuts (pot venir de configuració)
        private static readonly int MINUTS_VIGENCIA_CACHE = 30;

        public MultiRDbService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public object GetCurrentDate()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT NOW()";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    return cmd.ExecuteScalar();
                }
            }
        }

        public string GetDatabaseType()
        {
            return "MultirR (MySQL)";
        }

        public int GetTableRecordCount(string tableName)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string sql = $"SELECT COUNT(*) FROM {tableName}";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        /// <summary>
        /// Insereix un nou pacient a la base de dades
        /// </summary>
        /// <param name="dadesPacient">Dades del pacient a inserir</param>
        /// <returns>True si s'ha inserit correctament, False en cas contrari</returns>
        public bool InserirPacient(DadesPacient dadesPacient)
        {
            if (dadesPacient == null || string.IsNullOrWhiteSpace(dadesPacient.PacientSap))
                return false;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = @"
                        INSERT INTO pacients 
                        (npat, nom, cognom1, cognom2, dt_naixement, sexe, dt_create, dt_update, fitxa, cip, abs_referencia, consolidat, usuari)
                        VALUES 
                        (@npat, @nom, @cognom1, @cognom2, @dtNaixement, @sexe, NOW(), NOW(), 'I', @cip, @abs, 'N', 'MODULAB')";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@npat", dadesPacient.PacientSap.Trim());
                        cmd.Parameters.AddWithValue("@nom", dadesPacient.Nom ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@cognom1", dadesPacient.Cognom1 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@cognom2", dadesPacient.Cognom2 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@dtNaixement", dadesPacient.DataNaixement ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@sexe", dadesPacient.Sexe ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@cip", dadesPacient.Cip ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@abs", dadesPacient.Abs ?? (object)DBNull.Value);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Pacient {dadesPacient.PacientSap} inserit correctament a pacients de MultiR");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"No s'han afectat files en inserir el pacient {dadesPacient.PacientSap}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error inserint pacient {dadesPacient.PacientSap}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Carrega tots els microorganismes especials a la caché en memòria
        /// Thread-safe amb lock
        /// </summary>
        public void CarregarMicroorganismesEspecials()
        {
            lock (_lockCache)
            {
                try
                {
                    using (var conn = new MySqlConnection(_connectionString))
                    {
                        conn.Open();
                        
                        string sql = @"
                            SELECT CODI, DESCRIPCIO, ESPECIAL 
                            FROM microorganismes 
                            WHERE DT_DELETE IS NULL 
                            AND ACTIU = 1 
                            AND ESPECIAL = 1
                            ORDER BY DESCRIPCIO";

                        using (var cmd = new MySqlCommand(sql, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            _cacheMicroorganismesEspecials.Clear();
                            
                            while (reader.Read())
                            {
                                string descripcio = reader["DESCRIPCIO"].ToString();
                                string clau = descripcio.ToLower().Trim();
                                _cacheMicroorganismesEspecials.TryAdd(clau, true);
                            }
                        }
                    }
                    
                    _ultimaCarregaCache = DateTime.Now;
                    Console.WriteLine($"📋 Carregats {_cacheMicroorganismesEspecials.Count} microorganismes especials a la caché");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error carregant microorganismes especials: {ex.Message}");
                    Logger.Error($"Error carregant microorganismes especials: {ex.Message}", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Comprova si un microorganisme és especial utilitzant la caché
        /// Thread-safe
        /// </summary>
        /// <param name="microorganismeDescripcio">Descripció del microorganisme</param>
        /// <returns>True si és especial, False si no ho és, null si no es troba</returns>
        public bool? EsMicroorganismeEspecial(string microorganismeDescripcio)
        {
            // Verificar si cal recarregar la caché
            if (CacheCaducada())
            {
                CarregarMicroorganismesEspecials();
            }

            if (string.IsNullOrWhiteSpace(microorganismeDescripcio))
                return null;

            string descripcioNormalitzada = microorganismeDescripcio.ToLower().Trim();
            
            // Buscar coincidència exacta
            if (_cacheMicroorganismesEspecials.TryGetValue(descripcioNormalitzada, out bool esEspecial))
            {
                return esEspecial;
            }

            // Buscar coincidència parcial (conté)
            foreach (var microorganisme in _cacheMicroorganismesEspecials.Keys)
            {
                if (descripcioNormalitzada.Contains(microorganisme) || 
                    microorganisme.Contains(descripcioNormalitzada))
                {
                    return _cacheMicroorganismesEspecials[microorganisme];
                }
            }

            return false; // No es troba a la llista d'especials
        }

        /// <summary>
        /// Obté un microorganisme específic per descripció
        /// </summary>
        /// <param name="descripcio">Descripció del microorganisme</param>
        /// <returns>Microorganisme trobat o null</returns>
        public Microorganisme ObtenirMicroorganisme(string descripcio)
        {
            if (string.IsNullOrWhiteSpace(descripcio))
                return null;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = @"
                        SELECT ID, CODI, DESCRIPCIO, DT_DELETE, ACTIU, DIES_VIGENCIA, ESPECIAL
                        FROM microorganismes 
                        WHERE DESCRIPCIO = @descripcio 
                        AND DT_DELETE IS NULL 
                        AND ACTIU = 1
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@descripcio", descripcio.Trim());
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Microorganisme
                                {
                                    Id = Convert.ToInt32(reader["ID"]),
                                    Codi = reader["CODI"].ToString(),
                                    Descripcio = reader["DESCRIPCIO"].ToString(),
                                    DtDelete = reader["DT_DELETE"] != DBNull.Value 
                                        ? Convert.ToDateTime(reader["DT_DELETE"]) 
                                        : (DateTime?)null,
                                    Actiu = Convert.ToInt32(reader["ACTIU"]),
                                    DiesVigencia = Convert.ToInt32(reader["DIES_VIGENCIA"]),
                                    Especial = Convert.ToBoolean(reader["ESPECIAL"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint microorganisme '{descripcio}': {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// Obté tots els microorganismes especials
        /// </summary>
        /// <returns>Llista de microorganismes especials</returns>
        public List<Microorganisme> ObtenirMicroorganismesEspecials()
        {
            var microorganismes = new List<Microorganisme>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = @"
                        SELECT ID, CODI, DESCRIPCIO, DT_DELETE, ACTIU, DIES_VIGENCIA, ESPECIAL
                        FROM microorganismes 
                        WHERE DT_DELETE IS NULL 
                        AND ACTIU = 1 
                        AND ESPECIAL = 1
                        ORDER BY DESCRIPCIO";

                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            microorganismes.Add(new Microorganisme
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                Codi = reader["CODI"].ToString(),
                                Descripcio = reader["DESCRIPCIO"].ToString(),
                                DtDelete = reader["DT_DELETE"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["DT_DELETE"]) 
                                    : (DateTime?)null,
                                Actiu = Convert.ToInt32(reader["ACTIU"]),
                                DiesVigencia = Convert.ToInt32(reader["DIES_VIGENCIA"]),
                                Especial = Convert.ToBoolean(reader["ESPECIAL"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint microorganismes especials: {ex.Message}", ex);
            }

            return microorganismes;
        }

        /// <summary>
        /// Comprova si la caché ha caducat
        /// </summary>
        private bool CacheCaducada()
        {
            return _ultimaCarregaCache == DateTime.MinValue || 
                   DateTime.Now.Subtract(_ultimaCarregaCache).TotalMinutes > MINUTS_VIGENCIA_CACHE;
        }

        /// <summary>
        /// Neteja la caché de microorganismes
        /// Thread-safe
        /// </summary>
        public void NetejarCacheMicroorganismes()
        {
            lock (_lockCache)
            {
                _cacheMicroorganismesEspecials.Clear();
                _ultimaCarregaCache = DateTime.MinValue;
                Console.WriteLine("🧹 Caché de microorganismes netejada");
                Logger.Info("Caché de microorganismes netejada");
            }
        }

        /// <summary>
        /// Obté estadístiques de la caché
        /// </summary>
        /// <returns>Informació sobre l'estat de la caché</returns>
        public string ObtenirEstadistiquesCache()
        {
            var minutsTranscorreguts = _ultimaCarregaCache != DateTime.MinValue 
                ? DateTime.Now.Subtract(_ultimaCarregaCache).TotalMinutes 
                : -1;

            return $"Caché: {_cacheMicroorganismesEspecials.Count} microorganismes, " +
                   $"darrera càrrega: {(_ultimaCarregaCache != DateTime.MinValue ? _ultimaCarregaCache.ToString("dd/MM/yyyy HH:mm") : "mai")}, " +
                   $"vigència: {(CacheCaducada() ? "caducada" : $"{(MINUTS_VIGENCIA_CACHE - minutsTranscorreguts):F0} min restants")}";
        }
    }
}