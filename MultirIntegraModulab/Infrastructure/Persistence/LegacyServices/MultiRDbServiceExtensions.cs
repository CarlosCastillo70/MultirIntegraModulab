using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Enums;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService per gestionar els tractaments de resultats
    /// </summary>
    public partial class MultiRDbService
    {
        #region Gestió de Mecanismes de Resistència

        /// <summary>
        /// Comprova l'estat d'un mecanisme de resistència
        /// </summary>
        public EstatMecanisme ComprovarExistenciaMecanisme(string mecanismeCodi)
        {
            var estat = new EstatMecanisme
            {
                MecanismeCodi = mecanismeCodi,
                Existeix = false,
                IncorporaModulab = null
            };

            if (string.IsNullOrWhiteSpace(mecanismeCodi))
            {
                return estat;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = @"SELECT codi, incorpora_modulab 
                                  FROM mecanismes 
                                  WHERE codi = @mecanismeCodi
                                  AND dt_delete IS NULL";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@mecanismeCodi", mecanismeCodi);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                estat.Existeix = true;
                                estat.IncorporaModulab = reader["incorpora_modulab"] != DBNull.Value ? 
                                    Convert.ToBoolean(reader["incorpora_modulab"]) : true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant estat del mecanisme {mecanismeCodi}: {ex.Message}", ex);
            }

            return estat;
        }

        /// <summary>
        /// Crea un nou mecanisme de resistència
        /// </summary>
        public bool CrearMecanisme(string mecanismeCodi, string mecanismeDescripcio)
        {
            if (string.IsNullOrWhiteSpace(mecanismeCodi))
            {
                Logger.Error("El codi del mecanisme no pot estar buit");
                return false;
            }

            if (string.IsNullOrWhiteSpace(mecanismeDescripcio))
            {
                Logger.Error("La descripció del mecanisme no pot estar buida");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sqlCheck = @"SELECT COUNT(*) 
                                       FROM mecanismes 
                                       WHERE codi = @mecanismeCodi
                                       AND dt_delete IS NULL";
                    
                    using (var cmdCheck = new MySqlCommand(sqlCheck, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@mecanismeCodi", mecanismeCodi);
                        int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        
                        if (count > 0)
                        {
                            Logger.Info($"Mecanisme '{mecanismeCodi}' ja existeix");
                            return true;
                        }
                    }
                    
                    string sqlInsert = @"INSERT INTO mecanismes 
                                        (codi, descripcio, tipus_mecanisme) 
                                        VALUES (@mecanismeCodi, @mecanismeDescripcio, @mecanismeTipus)";
                    
                    using (var cmdInsert = new MySqlCommand(sqlInsert, conn))
                    {
                        cmdInsert.Parameters.AddWithValue("@mecanismeCodi", mecanismeCodi);
                        cmdInsert.Parameters.AddWithValue("@mecanismeDescripcio", mecanismeDescripcio);
                        cmdInsert.Parameters.AddWithValue("@mecanismeTipus", mecanismeDescripcio);

                        int filsAfectades = cmdInsert.ExecuteNonQuery();
                        
                        if (filsAfectades > 0)
                        {
                            Logger.Info($"Mecanisme '{mecanismeCodi}' creat correctament");
                            return true;
                        }
                        else
                        {
                            Logger.Error($"No s'ha pogut crear el mecanisme '{mecanismeCodi}'");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creant mecanisme '{mecanismeCodi}': {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Classificació i actualització de resultats

        public int ComprovarResultatExisteix(string etiquetaId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT COUNT(*) 
                                  FROM pacients_diagnostics_mostra pdm 
                                  WHERE pdm.etiqueta = @etiqueta 
                                  AND pdm.dt_delete IS NULL";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiqueta", etiquetaId);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprobant existència de {etiquetaId}: {ex.Message}", ex);
                return 0;
            }
        }

        public EstatResultat ObtenirEstatResultat(string etiquetaId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT pdm.etiqueta, 
                                         pdm.data_resultat, 
                                         pdm.data_validacio
                                  FROM pacients_diagnostics_mostra pdm 
                                  WHERE pdm.etiqueta = @etiqueta 
                                  AND pdm.dt_delete IS NULL
                                  ORDER BY pdm.dt_create DESC
                                  LIMIT 1";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiqueta", etiquetaId);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new EstatResultat
                                {
                                    EtiquetaId = reader["etiqueta"]?.ToString(),
                                    DataResultat = reader["data_resultat"] != DBNull.Value 
                                        ? Convert.ToDateTime(reader["data_resultat"]) 
                                        : (DateTime?)null,
                                    DataValidacio = reader["data_validacio"] != DBNull.Value 
                                        ? Convert.ToDateTime(reader["data_validacio"]) 
                                        : (DateTime?)null
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint estat de {etiquetaId}: {ex.Message}", ex);
            }
            
            return null;
        }

        public TipusEstatResultat ClassificarEstatResultat(string etiquetaId, DateTime? dataResultatOracle, DateTime? dataValidacioOracle)
        {
            // Es comprova si la mostra (etiqueta) existeix a MultiR
            
            int count = ComprovarResultatExisteix(etiquetaId);
            
            if (count == 0)
            {
                return TipusEstatResultat.Nova;
            }

            var estatMySQL = ObtenirEstatResultat(etiquetaId);
            if (estatMySQL == null)
            {
                return TipusEstatResultat.Nova;
            }

            if (!estatMySQL.DataResultat.HasValue && !estatMySQL.DataValidacio.HasValue)
            {
                return TipusEstatResultat.Antiga;
            }

            if (estatMySQL.DataResultat == dataResultatOracle && 
                estatMySQL.DataValidacio == dataValidacioOracle)
            {
                return TipusEstatResultat.Repetida;
            }

            if (estatMySQL.DataResultat.HasValue && estatMySQL.DataValidacio.HasValue && 
                dataResultatOracle.HasValue && !dataValidacioOracle.HasValue)
            {
                return TipusEstatResultat.Desvalidada;
            }

            if (estatMySQL.DataResultat.HasValue && !estatMySQL.DataValidacio.HasValue && 
                dataValidacioOracle.HasValue)
            {
                return TipusEstatResultat.Validada;
            }

            if (estatMySQL.DataValidacio.HasValue && dataValidacioOracle.HasValue && 
                estatMySQL.DataValidacio != dataValidacioOracle)
            {
                return TipusEstatResultat.Revalidada;
            }

            return TipusEstatResultat.Canviada;
        }

        public bool ActualitzarResultatAmbNovesDates(string etiquetaId, DateTime? dataResultat, DateTime? dataValidacio)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    var updates = new List<string>();
                    if (dataResultat.HasValue)
                        updates.Add("data_resultat = @dataResultat");
                    if (dataValidacio.HasValue)
                        updates.Add("data_validacio = @dataValidacio");

                    if (!updates.Any())
                    {
                        Logger.Warning($"No hi ha cap camp per actualitzar per l'etiqueta {etiquetaId}");
                        return false;
                    }

                    if (dataValidacio.HasValue)
                    {
                        updates.Add(@"estat_integracio_m = CASE 
                                    WHEN @dataValidacio IS NOT NULL THEN 'V'
                                    ELSE 'P' 
                                END");
                    }

                    updates.Add("dt_update = NOW()");

                    string sql = $@"
                        UPDATE pacients_diagnostics_mostra
                        SET {string.Join(", ", updates)}
                        WHERE etiqueta = @etiqueta 
                        AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiqueta", etiquetaId);

                        if (dataResultat.HasValue)
                            cmd.Parameters.AddWithValue("@dataResultat", dataResultat.Value);

                        if (dataValidacio.HasValue)
                            cmd.Parameters.AddWithValue("@dataValidacio", dataValidacio.Value);

                        int filsAfectades = cmd.ExecuteNonQuery();

                        if (filsAfectades > 0)
                        {
                            Logger.Info($"Actualitzades {filsAfectades} files per l'etiqueta {etiquetaId}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"No s'han trobat files per actualitzar per l'etiqueta {etiquetaId}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error actualitzant resultat amb noves dates {etiquetaId}: {ex.Message}", ex);
                return false;
            }
        }

        public bool ActualitzarResultatAntic(string etiquetaId, DateTime dataResultat, DateTime? dataValidacio)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = @"UPDATE pacients_diagnostics_mostra 
                                  SET data_resultat = @dataResultat,
                                      data_validacio = @dataValidacio,
                                      estat_integracio_m = CASE 
                                          WHEN @dataValidacio IS NOT NULL THEN 'V'
                                          ELSE 'P' 
                                      END,
                                      dt_update = NOW()
                                  WHERE etiqueta = @etiqueta 
                                  AND dt_delete IS NULL";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiqueta", etiquetaId);
                        cmd.Parameters.AddWithValue("@dataResultat", dataResultat);
                        cmd.Parameters.AddWithValue("@dataValidacio", dataValidacio.HasValue ? (object)dataValidacio.Value : DBNull.Value);
                        
                        int filsAfectades = cmd.ExecuteNonQuery();
                        
                        if (filsAfectades > 0)
                        {
                            Logger.Info($"Actualitzades {filsAfectades} files per l'etiqueta {etiquetaId}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"No s'han trobat files per actualitzar per l'etiqueta {etiquetaId}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error actualitzant resultat antic {etiquetaId}: {ex.Message}", ex);
                return false;
            }
        }

        public bool ActualitzarDataValidacio(string etiquetaId, DateTime? dataValidacio)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = @"UPDATE pacients_diagnostics_mostra 
                                  SET data_validacio = @dataValidacio,
                                      estat_integracio_m = CASE 
                                          WHEN @dataValidacio IS NOT NULL THEN 'V'
                                          ELSE 'P' 
                                      END,
                                      dt_update = NOW()
                                  WHERE etiqueta = @etiqueta 
                                  AND dt_delete IS NULL";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiqueta", etiquetaId);
                        cmd.Parameters.AddWithValue("@dataValidacio", 
                            (object)dataValidacio ?? DBNull.Value);
                        
                        int filsAfectades = cmd.ExecuteNonQuery();
                        
                        if (filsAfectades > 0)
                        {
                            Logger.Info($"Actualitzada data validació per l'etiqueta {etiquetaId}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"No s'han trobat files per actualitzar validació per l'etiqueta {etiquetaId}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error actualitzant data validació {etiquetaId}: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Microorganismes

        public bool ComprovarICrearMicroorganisme(string microorganismeDescripcio)
        {
            if (string.IsNullOrWhiteSpace(microorganismeDescripcio))
            {
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sqlComprovar = @"SELECT COUNT(*) 
                                           FROM microorganismes 
                                           WHERE UPPER(descripcio) = UPPER(@descripcio)
                                           AND actiu = 1 
                                           AND dt_delete IS NULL";
                    
                    using (var cmd = new MySqlCommand(sqlComprovar, conn))
                    {
                        cmd.Parameters.AddWithValue("@descripcio", microorganismeDescripcio.Trim());
                        
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        if (count > 0)
                        {
                            return true;
                        }
                    }
                    
                    Logger.Info($"🎉 Creant nou microorganisme (no existia previament): {microorganismeDescripcio}");
                    
                    string sqlCrear = @"INSERT INTO microorganismes (codi, descripcio) 
                                       VALUES (@codi, @descripcio)";
                    
                    using (var cmd = new MySqlCommand(sqlCrear, conn))
                    {
                        cmd.Parameters.AddWithValue("@codi", microorganismeDescripcio.Trim());
                        cmd.Parameters.AddWithValue("@descripcio", microorganismeDescripcio.Trim());
                        
                        int filasAfectadas = cmd.ExecuteNonQuery();
                        
                        if (filasAfectadas > 0)
                        {
                            Logger.Info($"✔️ Microorganisme {microorganismeDescripcio} creat correctament");
                            return true;
                        }
                        else
                        {
                            Logger.Error($"❌ Error creant el nou microorganisme {microorganismeDescripcio}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprobant/creant microorganisme {microorganismeDescripcio}: {ex.Message}", ex);
                return false;
            }
        }

        public bool InserirMostraMicroorganisme(string etiquetaId, string microorganismeDescripcio)
        {
            if (string.IsNullOrWhiteSpace(etiquetaId) || string.IsNullOrWhiteSpace(microorganismeDescripcio))
            {
                Logger.Error("InserirMostraMicroorganisme: etiquetaId o microorganismeDescripcio buits");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Comprovar si ja existeix
                    string sqlCheck = @"SELECT COUNT(*) 
                                       FROM mostra_microorganisme 
                                       WHERE etiqueta = @etiqueta 
                                       AND microorganisme = @microorganisme
                                       AND dt_delete IS NULL";
                    
                    using (var cmdCheck = new MySqlCommand(sqlCheck, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@etiqueta", etiquetaId);
                        cmdCheck.Parameters.AddWithValue("@microorganisme", microorganismeDescripcio.Trim());
                        
                        int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        
                        if (count > 0)
                        {
                            return false; // Ja existeix
                        }
                    }
                    
                    // Inserir nova relació
                    string sqlInsert = @"INSERT INTO mostra_microorganisme 
                                        (etiqueta, microorganisme, dt_create, dt_update) 
                                        VALUES (@etiqueta, @microorganisme, NOW(), NOW())";
                    
                    using (var cmdInsert = new MySqlCommand(sqlInsert, conn))
                    {
                        cmdInsert.Parameters.AddWithValue("@etiqueta", etiquetaId);
                        cmdInsert.Parameters.AddWithValue("@microorganisme", microorganismeDescripcio.Trim());
                        
                        int filsAfectades = cmdInsert.ExecuteNonQuery();
                        return filsAfectades > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error inserint mostra_microorganisme {etiquetaId}-{microorganismeDescripcio}: {ex.Message}", ex);
                return false;
            }
        }

        public bool InserirIntegracioResultats(string etiquetaId, ResultatMostra registre, string mecanismeId, 
            string estat, string observacions, bool incorporaModulab)
        {
            if (string.IsNullOrWhiteSpace(etiquetaId) || registre == null)
            {
                Logger.Error("InserirIntegracioResultats: paràmetres invàlids");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = @"INSERT INTO integracio_resultats 
                                  (etiqueta, pacient_sap, microorganisme, mecanisme, 
                                   data_resultat, data_validacio, estat, observacions, 
                                   incorpora_modulab, dt_create, dt_update) 
                                  VALUES 
                                  (@etiqueta, @pacientSap, @microorganisme, @mecanisme, 
                                   @dataResultat, @dataValidacio, @estat, @observacions, 
                                   @incorporaModulab, NOW(), NOW())";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiqueta", etiquetaId);
                        cmd.Parameters.AddWithValue("@pacientSap", registre.PacientSap ?? "");
                        cmd.Parameters.AddWithValue("@microorganisme", registre.AillamentDescripcio ?? "");
                        cmd.Parameters.AddWithValue("@mecanisme", mecanismeId ?? "");
                        cmd.Parameters.AddWithValue("@dataResultat", registre.DataResultat);
                        cmd.Parameters.AddWithValue("@dataValidacio", 
                            registre.DataValidacio.HasValue ? (object)registre.DataValidacio.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@estat", estat ?? "");
                        cmd.Parameters.AddWithValue("@observacions", observacions ?? "");
                        cmd.Parameters.AddWithValue("@incorporaModulab", incorporaModulab);
                        
                        int filsAfectades = cmd.ExecuteNonQuery();
                        return filsAfectades > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error inserint integracio_resultats per {etiquetaId}: {ex.Message}", ex);
                return false;
            }
        }

        public bool EsCombinacioNoIncorporar(string microorganisme, string mecanisme)
        {
            if (string.IsNullOrWhiteSpace(microorganisme) || string.IsNullOrWhiteSpace(mecanisme))
            {
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = @"SELECT COUNT(*) 
                                  FROM microorganisme_mecanisme_no_incorporar 
                                  WHERE microorganisme = @microorganisme 
                                  AND mecanisme = @mecanisme
                                  AND dt_delete IS NULL";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@microorganisme", microorganisme.Trim());
                        cmd.Parameters.AddWithValue("@mecanisme", mecanisme.Trim());
                        
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant combinació no incorporar {microorganisme}-{mecanisme}: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Auditoria

        public bool InserirAuditoriaIntegracioModulab(Mostra mostra, string codiResultat, MecanismeResistenciaInfo mecanisme = null, ResultatMostra resultatMostra = null)
        {
            if (mostra == null)
            {
                Logger.Error("⚠ Mostra és null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(codiResultat))
            {
                Logger.Error("⚠ CodiResultat és buit");
                return false;
            }

            try
            {
                Logger.Info($"🔄 Inserint auditoria amb codi {codiResultat}");

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    bool resultat = InserirRegistreAuditoria(conn, mostra, codiResultat, mecanisme, resultatMostra);

                    if (resultat)
                    {
                        string infoMecanisme = mecanisme != null ? $" amb mecanisme {mecanisme.Id}" : " sense mecanisme";
                        Logger.Info($" ✔️ Inserit registre d'auditoria per mostra amb etiqueta {mostra.EtiquetaId} {infoMecanisme}, amb resultat {codiResultat}");
                    }
                    else
                    {
                        Logger.Warning($" ⚠ No s'ha pogut crear registre d'auditoria amb resultat {codiResultat}");
                    }

                    return resultat;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"⚠ Error inserint auditoria per {mostra.EtiquetaId}: {ex.Message}", ex);
                return false;
            }
        }

        private bool InserirRegistreAuditoria(MySqlConnection conn, Mostra mostra, 
            string codiResultat, MecanismeResistenciaInfo mecanisme, ResultatMostra resultatMostra)
        {
            try
            {
                // Si s'ha proporcionat un resultat específic, utilitzar-lo; si no, agafar el primer
                var resultatPerAuditoria = resultatMostra ?? mostra.Resultats.First();

                string sql = @"
                    INSERT INTO integracio_modulab (
                        etiqueta_id, 
                        pacient_sap, 
                        cip, 
                        colegiat_id, 
                        nom_metge, 
                        centre_descripcio, 
                        data_peticio_truc, 
                        aillament_descripcio, 
                        mecanisme_resistencia1_id, 
                        mecanisme_resistencia_descrip, 
                        servei_descripcio, 
                        prova_descripcio, 
                        mostra_descripcio, 
                        dt_create, 
                        data_resultat, 
                        data_validacio, 
                        resultat
                    ) VALUES (
                        @etiqueta_id, 
                        @pacient_sap, 
                        @cip, 
                        @colegiat_id, 
                        @nom_metge, 
                        @centre_descripcio, 
                        @data_peticio_truc, 
                        @aillament_descripcio, 
                        @mecanisme_resistencia1_id, 
                        @mecanisme_resistencia_descrip, 
                        @servei_descripcio, 
                        @prova_descripcio, 
                        @mostra_descripcio, 
                        NOW(), 
                        @data_resultat, 
                        @data_validacio, 
                        @resultat
                    )";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@etiqueta_id", mostra.EtiquetaId ?? "");
                    cmd.Parameters.AddWithValue("@pacient_sap", mostra.PacientSap ?? "");
                    cmd.Parameters.AddWithValue("@cip", mostra.Cip ?? "");
                    cmd.Parameters.AddWithValue("@colegiat_id", resultatPerAuditoria.ColegiatId ?? "");
                    cmd.Parameters.AddWithValue("@nom_metge", resultatPerAuditoria.NomMetge ?? "");
                    cmd.Parameters.AddWithValue("@centre_descripcio", resultatPerAuditoria.CentreDescripcio ?? "");
                    
                    cmd.Parameters.AddWithValue("@data_peticio_truc", 
                        resultatPerAuditoria.DataPeticioTrunc.HasValue ? (object)resultatPerAuditoria.DataPeticioTrunc.Value : DBNull.Value);
                    
                    // Utilitzar el microorganisme del resultat específic
                    cmd.Parameters.AddWithValue("@aillament_descripcio", resultatPerAuditoria.AillamentDescripcio ?? "");

                    cmd.Parameters.AddWithValue("@mecanisme_resistencia1_id", 
                        mecanisme?.Id ?? "");
                    cmd.Parameters.AddWithValue("@mecanisme_resistencia_descrip", 
                        mecanisme?.Descripcio ?? "");

                    cmd.Parameters.AddWithValue("@servei_descripcio", resultatPerAuditoria.ServeiDescripcio ?? "");
                    cmd.Parameters.AddWithValue("@prova_descripcio", resultatPerAuditoria.ProvaDescripcio ?? "");
                    cmd.Parameters.AddWithValue("@mostra_descripcio", resultatPerAuditoria.MostraDescripcio ?? "");

                    cmd.Parameters.AddWithValue("@data_resultat", 
                        resultatPerAuditoria.DataResultat);
                    
                    cmd.Parameters.AddWithValue("@data_validacio", 
                        resultatPerAuditoria.DataValidacio.HasValue ? (object)resultatPerAuditoria.DataValidacio.Value : DBNull.Value);

                    cmd.Parameters.AddWithValue("@resultat", codiResultat);

                    int filsAfectades = cmd.ExecuteNonQuery();
                    return filsAfectades > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error inserint registre d'auditoria: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Gestió de pacients

        public bool ValidarConnexio()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT 1", conn))
                    {
                        cmd.ExecuteScalar();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error de connexió MySQL: {ex.Message}", ex);
                return false;
            }
        }

        public bool ExisteixPacient(string pacientSap)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning(" ❌ ExisteixPacient: pacientSap és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = "SELECT COUNT(*) FROM pacients WHERE npat = @pacientSap AND dt_delete IS NULL";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        bool existeix = count > 0;
                        
                        // Logger.Info($"  Pacient {pacientSap}: {(existeix ? "existeix" : "no existeix")} a la taula pacients de MultiR");
                        return existeix;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant existència del pacient {pacientSap}: {ex.Message}", ex);
                return false;
            }
        }

        public DadesPacientMySQL ObtenirPacient(string pacientSap)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ObtenirPacient: pacientSap és null o buit");
                return null;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = @"SELECT id, npat, nom, cognom1, cognom2, dt_naixement, sexe, 
                                  dt_create, dt_update, cip, abs_referencia, consolidat, usuari
                                  FROM pacients 
                                  WHERE npat = @pacientSap AND dt_delete IS NULL";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new DadesPacientMySQL
                                {
                                    Id = reader.GetInt32("id"),
                                    Npat = reader["npat"]?.ToString(),
                                    Nom = reader["nom"]?.ToString(),
                                    Cognom1 = reader["cognom1"]?.ToString(),
                                    Cognom2 = reader["cognom2"]?.ToString(),
                                    DataNaixement = reader["dt_naixement"] as DateTime?,
                                    Sexe = reader["sexe"]?.ToString(),
                                    DataCreacio = reader["dt_create"] as DateTime?,
                                    DataActualitzacio = reader["dt_update"] as DateTime?,
                                    Cip = reader["cip"]?.ToString(),
                                    AbsReferencia = reader["abs_referencia"]?.ToString(),
                                    Consolidat = reader["consolidat"]?.ToString(),
                                    Usuari = reader["usuari"]?.ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint dades del pacient {pacientSap}: {ex.Message}", ex);
            }

            return null;
        }

        public bool CrearPacient(DadesPacientWebService dadesPacient)
        {
            if (dadesPacient == null)
            {
                Logger.Error("CrearPacient: dadesPacient és null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(dadesPacient.PacientSap))
            {
                Logger.Error("CrearPacient: PacientSap és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    string sql = @"INSERT INTO pacients 
                                  (npat, nom, cognom1, cognom2, dt_naixement, sexe, 
                                   dt_create, dt_update, fitxa, cip, abs_referencia, 
                                   consolidat, usuari)
                                  VALUES 
                                  (@npat, @nom, @cognom1, @cognom2, @dt_naixement, @sexe,
                                   NOW(), NOW(), 'I', @cip, @abs_referencia, 
                                   'N', 'MODULAB')";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@npat", dadesPacient.PacientSap);
                        cmd.Parameters.AddWithValue("@nom", dadesPacient.Nom ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@cognom1", dadesPacient.Cognom1 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@cognom2", dadesPacient.Cognom2 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@dt_naixement", dadesPacient.DataNaixement ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@sexe", dadesPacient.Sexe ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@cip", dadesPacient.Cip ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@abs_referencia", dadesPacient.Abs ?? (object)DBNull.Value);
                        
                        int filesAfectades = cmd.ExecuteNonQuery();
                        
                        if (filesAfectades > 0)
                        {
                            Logger.Info($"Pacient {dadesPacient.PacientSap} creat correctament");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"No s'han afectat files en crear el pacient {dadesPacient.PacientSap}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creant pacient {dadesPacient.PacientSap}: {ex.Message}", ex);
                return false;
            }
        }

        public int ComprovarDiagnosticExisteix(string pacientSap, string microorganisme, string mecanisme, string tipusMecanisme)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ComprovarDiagnosticExisteix: pacientSap és null o buit");
                return 0;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"🔎 Comprovant / creant diagnostic {microorganisme} + {microorganisme} [{tipusMecanisme}]");


                    conn.Open();
                    
                    string sql = @"SELECT id 
                                  FROM pacients_diagnostics 
                                  WHERE npat = @pacientSap 
                                  AND microorganisme = @microorganisme 
                                  AND mecanisme = @mecanisme 
                                  AND tipus_mecanisme = @tipusMecanisme 
                                  AND dt_delete IS NULL";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@microorganisme", microorganisme ?? "");
                        cmd.Parameters.AddWithValue("@mecanisme", mecanisme ?? "");
                        cmd.Parameters.AddWithValue("@tipusMecanisme", tipusMecanisme ?? "");
                        
                        var result = cmd.ExecuteScalar();
                        int diagnosticId = result != null ? Convert.ToInt32(result) : 0;
                        

                        Logger.Info($"  Diagnòstic del pacient {pacientSap} + {microorganisme} + {mecanisme}: {(diagnosticId > 0 ? $"ja existeix (ID: {diagnosticId})" : "no existeix")}");
                        return diagnosticId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant diagnòstic per pacient {pacientSap}: {ex.Message}", ex);
                return 0;
            }
        }

        public int CrearDiagnosticPacient(string pacientSap, string microorganisme, string mecanisme, string tipusMecanisme)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Error("CrearDiagnosticPacient: pacientSap és null o buit");
                return 0;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"  Es procedeix a crear el Diagnòstic: {microorganisme} + {mecanisme}");

                    conn.Open();
                    
                    string sql = @"INSERT INTO pacients_diagnostics 
                                  (npat, data_diagnostic, usuari, bitxo, dt_create, dt_update, 
                                   microorganisme, mecanisme, tipus_mecanisme, consolidat, 
                                   data_ingres, data_alta)
                                  VALUES 
                                  (@npat, NULL, 'MODULAB', '', NOW(), NOW(), 
                                   @microorganisme, @mecanisme, @tipusMecanisme, 'N', 
                                   '9999-12-31', '9999-12-31');
                                  SELECT LAST_INSERT_ID();";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@npat", pacientSap);
                        cmd.Parameters.AddWithValue("@microorganisme", microorganisme ?? "");
                        cmd.Parameters.AddWithValue("@mecanisme", mecanisme ?? "");
                        cmd.Parameters.AddWithValue("@tipusMecanisme", tipusMecanisme ?? "");
                        
                        var result = cmd.ExecuteScalar();
                        int nouDiagnosticId = result != null ? Convert.ToInt32(result) : 0;
                        
                        if (nouDiagnosticId > 0)
                        {
                            Logger.Info($" ✔️ Creat diagnòstic ID {nouDiagnosticId} per pacient {pacientSap}: {microorganisme} + {mecanisme}");
                            return nouDiagnosticId;
                        }
                        else
                        {
                            Logger.Error($" ⚠️ Error creant diagnòstic per pacient {pacientSap}: no s'ha retornat ID");
                            return 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creant diagnòstic per pacient {pacientSap}: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Comprova si existeix un registre de pacients_diagnostics_mostra
        /// </summary>
        /// <param name="pacientSap">SAP del pacient</param>
        /// <param name="dataMostra">Data de la mostra</param>
        /// <param name="tipusMostra">Tipus de mostra</param>
        /// <returns>ID del registre si existeix, 0 si no existeix</returns>
        public int ComprovarMostraDiagnosticExisteix(string pacientSap, DateTime? dataMostra, string tipusMostra)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ComprovarMostraDiagnosticExisteix: pacientSap és null o buit");
                return 0;
            }

            if (!dataMostra.HasValue)
            {
                Logger.Warning("ComprovarMostraDiagnostic Existeix: dataMostra és null");
                return 0;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"🔎 Comprovant / creant mostra diagnòstic per pacient {pacientSap} de tipus {tipusMostra}");

                    conn.Open();
                    
                    string sql = @"SELECT id 
                                  FROM pacients_diagnostics_mostra 
                                  WHERE npat = @pacientSap 
                                  AND data_mostra = @dataMostra 
                                  AND tipus_mostra_m = @tipusMostra 
                                  AND dt_delete IS NULL";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@dataMostra", dataMostra.Value);
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra ?? "");
                        
                        var result = cmd.ExecuteScalar();
                        int mostraId = result != null ? Convert.ToInt32(result) : 0;
                        
                        Logger.Info($"  Mostra diagnòstic per pacient {pacientSap}, data {dataMostra:dd/MM/yyyy}, tipus {tipusMostra}: {(mostraId > 0 ? $"ja existeix (ID: {mostraId})" : "no existeix")}");
                        return mostraId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant mostra diagnòstic per pacient {pacientSap}: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Crea un nou registre de pacients_diagnostics_mostra
        /// </summary>
        /// <param name="pacientSap">SAP del pacient</param>
        /// <param name="dataMostra">Data de la mostra</param>
        /// <param name="tipusMostra">Tipus de mostra</param>
        /// <param name="tipusProva">Tipus de prova</param>
        /// <param name="etiqueta">Etiqueta de la mostra</param>
        /// <param name="dataResultat">Data del resultat</param>
        /// <param name="dataValidacio">Data de validació (pot ser null)</param>
        /// <param name="mecanismeId">ID del mecanisme de resistència (pot ser null)</param>
        /// <param name="esMicroorganismeEspecial">Indica si el microorganisme és especial (pot ser null)</param>
        /// <returns>ID del nou registre si s'ha creat correctament, 0 si ha fallat</returns>
        public int CrearMostraDiagnostic(string pacientSap, DateTime? dataMostra, string tipusMostra, 
            string tipusProva, string etiqueta, DateTime? dataResultat, DateTime? dataValidacio, 
            string mecanismeId, bool? esMicroorganismeEspecial)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Error("CrearMostraDiagnostic: pacientSap és null o buit");
                return 0;
            }

            if (!dataMostra.HasValue)
            {
                Logger.Error("CrearMostraDiagnostic: dataMostra és null");
                return 0;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {

                    Logger.Info($"  Es procedeix a crear la Mostra diagnòstic");

                    conn.Open();
                    
                    // Determinar si la mostra és positiva: té mecanisme o el microorganisme és especial
                    bool esMostraPositiva = !string.IsNullOrWhiteSpace(mecanismeId) || 
                                           (esMicroorganismeEspecial.HasValue && esMicroorganismeEspecial.Value);
                    
                    // Determinar valoració segons si la mostra és positiva o negativa
                    string valoracio = esMostraPositiva ? "2" : "1";
                    
                    // Determinar estat d'integració segons si està validada o no
                    string estatIntegracio = dataValidacio.HasValue ? "V" : "P";
                    
                    string sql = @"INSERT INTO pacients_diagnostic
