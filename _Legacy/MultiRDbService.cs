using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace MultirIntegraModulab
{
    public partial class MultiRDbService : IDbService
    {
        private readonly string _connectionString;
        private static Dictionary<string, bool> _cacheMicroorganismesEspecials = new Dictionary<string, bool>();
        private static DateTime _ultimaCarregaCache = DateTime.MinValue;
        private static readonly int MINUTS_VIGENCIA_CACHE = 30; // Caché vàlida per 30 minuts

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
            return "MySQL";
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
        /// Carrega tots els microorganismes especials a la caché en memòria
        /// </summary>
        public void CarregarMicroorganismesEspecials()
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
                            _cacheMicroorganismesEspecials[descripcio.ToLower().Trim()] = true;
                        }
                    }
                }
                
                _ultimaCarregaCache = DateTime.Now;
                Console.WriteLine($"?? Carregats {_cacheMicroorganismesEspecials.Count} microorganismes especials a la caché");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error carregant microorganismes especials: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Comprova si un microorganisme és especial utilitzant la caché
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
            if (_cacheMicroorganismesEspecials.ContainsKey(descripcioNormalitzada))
            {
                return _cacheMicroorganismesEspecials[descripcioNormalitzada];
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

            return null;
        }

        /// <summary>
        /// Obté tots els microorganismes especials
        /// </summary>
        /// <returns>Llista de microorganismes especials</returns>
        public List<Microorganisme> ObtenirMicroorganismesEspecials()
        {
            var microorganismes = new List<Microorganisme>();

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
        /// </summary>
        public void NetejarCacheMicroorganismes()
        {
            _cacheMicroorganismesEspecials.Clear();
            _ultimaCarregaCache = DateTime.MinValue;
            Console.WriteLine("?? Caché de microorganismes netejada");
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