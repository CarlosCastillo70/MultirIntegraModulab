using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Enums;
using MultirIntegraModulab.Application.Helpers;

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
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Mecanisme '{mecanismeCodi}' JA existeix");
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
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Mecanisme '{mecanismeCodi}' creat correctament");
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
                        Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}No hi ha cap camp per actualitzar per l'etiqueta {etiquetaId}");
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
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Actualitzades {filsAfectades} files per l'etiqueta {etiquetaId}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}No s'han trobat files per actualitzar per l'etiqueta {etiquetaId}");
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
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Actualitzades {filsAfectades} files per l'etiqueta {etiquetaId}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}No s'han trobat files per actualitzar per l'etiqueta {etiquetaId}");
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
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Actualitzada data validació per l'etiqueta {etiquetaId}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}No s'han trobat files per actualitzar validació per l'etiqueta {etiquetaId}");
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
                    
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}🎉 Creant nou microorganisme (no existia previament): {microorganismeDescripcio}");
                    
                    string sqlCrear = @"INSERT INTO microorganismes (codi, descripcio) 
                                       VALUES (@codi, @descripcio)";
                    
                    using (var cmd = new MySqlCommand(sqlCrear, conn))
                    {
                        cmd.Parameters.AddWithValue("@codi", microorganismeDescripcio.Trim());
                        cmd.Parameters.AddWithValue("@descripcio", microorganismeDescripcio.Trim());
                        
                        int filasAfectades = cmd.ExecuteNonQuery();
                        
                        if (filasAfectades > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Microorganisme {microorganismeDescripcio} creat correctament");
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

        /// <summary>
        /// Obté la descripció d'un resultat d'integració a partir del seu codi
        /// </summary>
        /// <param name="codiResultat">Codi del resultat (ex: OKP, OKN, NPWS, DMM, etc.)</param>
        /// <returns>Descripció del resultat o null si no es troba</returns>
        private string ObtenirDescripcioResultatIntegracio(string codiResultat)
        {
            if (string.IsNullOrWhiteSpace(codiResultat))
                return null;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT descripcio 
                                  FROM integracio_modulab_resultats 
                                  WHERE codi = @codiResultat 
                                  LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiResultat", codiResultat);

                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint descripció del resultat {codiResultat}: {ex.Message}", ex);
                return null;
            }
        }

        public bool InserirAuditoriaIntegracioModulab(Mostra mostra, string codiResultat, ResultatMostra resultatMostra = null, MecanismeResistenciaInfo mecanisme = null)
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
                // Obtenir la descripció del resultat
                string descripcioResultat = ObtenirDescripcioResultatIntegracio(codiResultat);
                string textDescripcio = !string.IsNullOrWhiteSpace(descripcioResultat) 
                    ? $" ({descripcioResultat})" 
                    : "";

                Logger.Info($"🔄 Inserint auditoria amb codi '{codiResultat}'{textDescripcio}");

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    bool resultat = InserirRegistreAuditoria(conn, mostra, codiResultat, resultatMostra, mecanisme);

                    if (resultat)
                    {
                        string infoMecanisme = mecanisme != null ? $" amb mecanisme {mecanisme.Id}" : " sense mecanisme";
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Inserit registre d'auditoria per mostra amb etiqueta {mostra.EtiquetaId} {infoMecanisme}, amb resultat {codiResultat}{textDescripcio}");
                    }
                    else
                    {
                        Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠ No s'ha pogut crear registre d'auditoria amb resultat {codiResultat}{textDescripcio}");
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
            string codiResultat, ResultatMostra resultatMostra, MecanismeResistenciaInfo mecanisme)
        {
            try
            {
                // Si no s'ha proporcionat un resultat específic, utilitzar el primer
                var resultatUtilitzar = resultatMostra ?? mostra.Resultats.First();

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
                    cmd.Parameters.AddWithValue("@colegiat_id", resultatUtilitzar.ColegiatId ?? "");
                    cmd.Parameters.AddWithValue("@nom_metge", resultatUtilitzar.NomMetge ?? "");
                    cmd.Parameters.AddWithValue("@centre_descripcio", resultatUtilitzar.CentreDescripcio ?? "");
                    
                    cmd.Parameters.AddWithValue("@data_peticio_truc", 
                        resultatUtilitzar.DataPeticioTrunc.HasValue ? (object)resultatUtilitzar.DataPeticioTrunc.Value : DBNull.Value);
                    
                    cmd.Parameters.AddWithValue("@aillament_descripcio", resultatUtilitzar.AillamentDescripcio ?? "");

                    cmd.Parameters.AddWithValue("@mecanisme_resistencia1_id", 
                        mecanisme?.Id ?? "");
                    cmd.Parameters.AddWithValue("@mecanisme_resistencia_descrip", 
                        mecanisme?.Descripcio ?? "");

                    cmd.Parameters.AddWithValue("@servei_descripcio", resultatUtilitzar.ServeiDescripcio ?? "");
                    cmd.Parameters.AddWithValue("@prova_descripcio", resultatUtilitzar.ProvaDescripcio ?? "");
                    cmd.Parameters.AddWithValue("@mostra_descripcio", resultatUtilitzar.MostraDescripcio ?? "");

                    cmd.Parameters.AddWithValue("@data_resultat", 
                        resultatUtilitzar.DataResultat != default(DateTime) ? (object)resultatUtilitzar.DataResultat : DBNull.Value);
                    
                    cmd.Parameters.AddWithValue("@data_validacio", 
                        resultatUtilitzar.DataValidacio.HasValue ? (object)resultatUtilitzar.DataValidacio.Value : DBNull.Value);

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
                Logger.Error($"Error comprobant existència del pacient {pacientSap}: {ex.Message}", ex);
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
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Pacient {dadesPacient.PacientSap} creat correctament");
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
                    Logger.Info($"🔎 Comprovant / creant diagnostic {microorganisme} [{mecanisme} - {tipusMecanisme}]");


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
                        

                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Diagnòstic del pacient {pacientSap} + {microorganisme} + {mecanisme}: {(diagnosticId > 0 ? $"JA existeix (ID: {diagnosticId})" : "NO existeix")}");
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
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Es procedeix a crear el Diagnòstic: {microorganisme} + {mecanisme}");

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
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Creat diagnòstic ID {nouDiagnosticId} per pacient {pacientSap}: {microorganisme} + {mecanisme}");
                            return nouDiagnosticId;
                        }
                        else
                        {
                            Logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Error creant diagnòstic per pacient {pacientSap}: no s'ha retornat ID");
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
                    Logger.Info($"🔎 Comprovant / creant mostra diagnòstic de tipus '{tipusMostra}'");

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
                        
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Mostra del pacient {pacientSap} + data {dataMostra:dd/MM/yyyy} + tipus '{tipusMostra}': {(mostraId > 0 ? $"JA existeix (ID: {mostraId})" : "NO existeix")}");
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

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Es procedeix a crear la Mostra diagnòstic");

                    conn.Open();
                    
                    // Determinar si la mostra és positiva: té mecanisme o el microorganisme és especial
                    bool esMostraPositiva = !string.IsNullOrWhiteSpace(mecanismeId) || 
                                           (esMicroorganismeEspecial.HasValue && esMicroorganismeEspecial.Value);
                    
                    // Determinar valoració segons si la mostra és positiva o negativa
                    string valoracio = esMostraPositiva ? "2" : "1";
                    
                    // Determinar estat d'integració segons si està validada o no
                    string estatIntegracio = dataValidacio.HasValue ? "V" : "P";
                    
                    string sql = @"INSERT INTO pacients_diagnostics_mostra 
                                  (npat, data_diagnostic, data_mostra, tipus_mostra_m, usuari, 
                                   dt_create, dt_update, consolidat, valoracio, tipus_prova, 
                                   etiqueta, data_resultat, data_validacio, estat_integracio_m)
                                  VALUES 
                                  (@npat, NULL, @dataMostra, @tipusMostra, 'MODULAB', 
                                   NOW(), NOW(), 'N', @valoracio, @tipusProva, 
                                   @etiqueta, @dataResultat, @dataValidacio, @estatIntegracio);
                                  SELECT LAST_INSERT_ID();";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@npat", pacientSap);
                        cmd.Parameters.AddWithValue("@dataMostra", dataMostra.Value);
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra ?? "");
                        cmd.Parameters.AddWithValue("@valoracio", valoracio);
                        cmd.Parameters.AddWithValue("@tipusProva", tipusProva ?? "");
                        cmd.Parameters.AddWithValue("@etiqueta", etiqueta ?? "");
                        cmd.Parameters.AddWithValue("@dataResultat", dataResultat.HasValue ? (object)dataResultat.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@dataValidacio", dataValidacio.HasValue ? (object)dataValidacio.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@estatIntegracio", estatIntegracio);
                        
                        var result = cmd.ExecuteScalar();
                        int nouMostraId = result != null ? Convert.ToInt32(result) : 0;
                        
                        if (nouMostraId > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Creada mostra diagnòstic ID {nouMostraId} per pacient {pacientSap}: data {dataMostra:dd/MM/yyyy}, tipus {tipusMostra}, valoració {valoracio}");
                            return nouMostraId;
                        }
                        else
                        {
                            Logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Error creant mostra diagnòstic per pacient {pacientSap}: no s'ha retornat ID");
                            return 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creant mostra diagnòstic per pacient {pacientSap}: {ex.Message}", ex);
                return 0;
            }
        }

        public class DadesPacientMySQL
        {
            public int Id { get; set; }
            public string Npat { get; set; }
            public string Nom { get; set; }
            public string Cognom1 { get; set; }
            public string Cognom2 { get; set; }
            public DateTime? DataNaixement { get; set; }
            public string Sexe { get; set; }
            public DateTime? DataCreacio { get; set; }
            public DateTime? DataActualitzacio { get; set; }
            public string Cip { get; set; }
            public string AbsReferencia { get; set; }
            public string Consolidat { get; set; }
            public string Usuari { get; set; }

            public override string ToString()
            {
                return $"Pacient {Npat}: {Nom} {Cognom1} {Cognom2} (ID: {Id})";
            }
        }

        #endregion

        #region Comparació de mostres

        /// <summary>
        /// Obté les dades completes d'una mostra diagnòstic existent
        /// </summary>
        /// <param name="etiquetaId">Etiqueta de la muestra</param>
        /// <returns>Dades de la mostra diagnòstic o null si no existeix</returns>
        public MostraDiagnosticExistent ObtenirMostraDiagnostic(string etiquetaId)
        {
            if (string.IsNullOrWhiteSpace(etiquetaId))
            {
                Logger.Warning("ObtenirMostraDiagnostic: etiquetaId és null o buit");
                return null;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT id, npat, data_mostra, tipus_mostra_m, tipus_prova, 
                                         etiqueta, data_resultat, data_validacio, valoracio, 
                                         estat_integracio_m, dt_create, dt_update
                                  FROM pacients_diagnostics_mostra 
                                  WHERE etiqueta = @etiqueta 
                                  AND dt_delete IS NULL
                                  ORDER BY dt_create DESC
                                  LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiqueta", etiquetaId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new MostraDiagnosticExistent
                                {
                                    Id = reader.getInt32("id"),
                                    PacientSap = reader["npat"]?.ToString(),
                                    DataMostra = reader["data_mostra"] as DateTime?,
                                    TipusMostra = reader["tipus_mostra_m"]?.ToString(),
                                    TipusProva = reader["tipus_prova"]?.ToString(),
                                    Etiqueta = reader["etiqueta"]?.ToString(),
                                    DataResultat = reader["data_resultat"] as DateTime?,
                                    DataValidacio = reader["data_validacio"] as DateTime?,
                                    Valoracio = reader["valoracio"]?.ToString(),
                                    EstatIntegracio = reader["estat_integracio_m"]?.ToString(),
                                    DataCreacio = reader["dt_create"] as DateTime?,
                                    DataActualitzacio = reader["dt_update"] as DateTime?
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint mostra diagnòstic {etiquetaId}: {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// Compara una mostra entrant amb una mostra existent per detectar canvis
        /// </summary>
        /// <param name="mostraExistent">Mostra existent a la base de dades</param>
        /// <param name="mostraEntrant">Mostra que està entrant</param>
        /// <returns>Resultat de la comparació amb detall dels canvis</returns>
        public ResultatComparacioMostres CompararMostres(MostraDiagnosticExistent mostraExistent, Mostra mostraEntrant)
        {
            var resultat = new ResultatComparacioMostres
            {
                HiHaCanvis = false,
                CanvisDetectats = new List<string>()
            };

            if (mostraExistent == null || mostraEntrant == null)
            {
                resultat.HiHaCanvis = true;
                resultat.CanvisDetectats.Add("Una de les mostres és null");
                return resultat;
            }

            // 1. COMPARAR DATA DE RESULTAT
            var dataResultatEntrant = mostraEntrant.DataUltimResultat;
            if (mostraExistent.DataResultat != dataResultatEntrant)
            {
                resultat.HiHaCanvis = true;
                resultat.CanvisDetectats.Add($"Data resultat: {mostraExistent.DataResultat:dd/MM/yyyy HH:mm} -> {dataResultatEntrant:dd/MM/yyyy HH:mm}");
            }

            // 2. COMPARAR TIPUS DE MOSTRA
            var tipusMostraEntrant = mostraEntrant.Resultats.FirstOrDefault()?.MostraDescripcio;
            if (!string.Equals(mostraExistent.TipusMostra, tipusMostraEntrant, StringComparison.OrdinalIgnoreCase))
            {
                resultat.HiHaCanvis = true;
                resultat.CanvisDetectats.Add($"Tipus mostra: '{mostraExistent.TipusMostra}' -> '{tipusMostraEntrant}'");
            }

            // 3. COMPARAR TIPUS DE PROVA
            var tipusProvaEntrant = mostraEntrant.Resultats.FirstOrDefault()?.ProvaDescripcio;
            if (!string.Equals(mostraExistent.TipusProva, tipusProvaEntrant, StringComparison.OrdinalIgnoreCase))
            {
                resultat.HiHaCanvis = true;
                resultat.CanvisDetectats.Add($"Tipus prova: '{mostraExistent.TipusProva}' -> '{tipusProvaEntrant}'");
            }

            // 4. COMPARAR COMBINACIONS MICROORGANISME + MECANISMES DE RESISTÈNCIA
            // Obtenir les combinacions de la base de dades i de la mostra entrant
            var combinacionsExistents = ObtenirCombinacionsMicroorganismeMecanisme(mostraExistent.Etiqueta);
            var combinacionsEntrants = ObtenirCombinacionsMostraEntrant(mostraEntrant);

            // Convertir a conjunts (HashSet) per comparar sense tenir en compte l'ordre
            var conjuntExistent = new HashSet<string>(
                combinacionsExistents.Select(c => c.ToString()), 
                StringComparer.OrdinalIgnoreCase);
            
            var conjuntEntrant = new HashSet<string>(
                combinacionsEntrants.Select(c => c.ToString()), 
                StringComparer.OrdinalIgnoreCase);

            // Comprovar si hi ha diferències en les combinacions
            if (!conjuntExistent.SetEquals(conjuntEntrant))
            {
                resultat.HiHaCanvis = true;
                
                // Identificar combinacions noves (que estan a l'entrant però no a l'existent)
                var combinacionsNoves = conjuntEntrant.Except(conjuntExistent).ToList();
                if (combinacionsNoves.Any())
                {
                    resultat.CanvisDetectats.Add($"Combinacions noves: [{string.Join(", ", combinacionsNoves)}]");
                }

                // Identificar combinacions eliminades (que estan a l'existent però no a l'entrant)
                var combinacionsEliminades = conjuntExistent.Except(conjuntEntrant).ToList();
                if (combinacionsEliminades.Any())
                {
                    resultat.CanvisDetectats.Add($"Combinacions eliminades: [{string.Join(", ", combinacionsEliminades)}]");
                }
            }

            return resultat;
        }

        /// <summary>
        /// Obté les combinacions de microorganisme + mecanismes d'una mostra existent a la BD
        /// NOMÉS obtenim diagnòstics positius: amb mecanisme de resistència
        /// </summary>
        public List<CombinacioMicroorganismeMecanisme> ObtenirCombinacionsMicroorganismeMecanisme(string etiquetaId)
        {
            var combinacions = new List<CombinacioMicroorganismeMecanisme>();

            if (string.IsNullOrWhiteSpace(etiquetaId))
            {
                return combinacions;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // Obtenir les combinacions de mostra_microorganisme i els seus mecanismes
                    // NOMÉS obtenim diagnòstics positius: amb mecanisme de resistència
                    string sql = @"
                        SELECT DISTINCT
                            pd.microorganisme,
                            pd.mecanisme AS mecanisme1,
                            pd.tipus_mecanisme
                        FROM mostra_microorganisme mm
                            INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                            INNER JOIN pacients_diagnostics pd ON mm.pacient_diagnostic_id = pd.id
                        WHERE pdm.etiqueta = @etiqueta
                            AND pdm.dt_delete IS NULL
                            AND pd.dt_delete IS NULL
                            AND (
                                pd.mecanisme IS NOT NULL AND pd.mecanisme != ''
                            )
                        ORDER BY pd.microorganisme, pd.mecanisme";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiqueta", etiquetaId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var microorganisme = reader["microorganisme"]?.ToString() ?? "";
                                var mecanisme1 = reader["mecanisme1"]?.ToString() ?? "";

                                if (!string.IsNullOrWhiteSpace(microorganisme) && !string.IsNullOrWhiteSpace(mecanisme1))
                                {
                                    // Crear una combinació individual per cada microorganisme + mecanisme
                                    // Això permet comparar correctament independentment de l'ordre
                                    combinacions.Add(new CombinacioMicroorganismeMecanisme
                                    {
                                        Microorganisme = microorganisme.Trim(),
                                        Mecanismes = new List<string> { mecanisme1.Trim() }
                                    });
                                }
                            }
                        }
                    }

                    // Obtenir microorganismes especials sense mecanismes (si n'hi ha)
                    // Això són casos on valoracio = '2' però no tenen mecanisme
                    string sqlEspecials = @"
                        SELECT DISTINCT
                            pd.microorganisme
                        FROM mostra_microorganisme mm
                            INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                            INNER JOIN pacients_diagnostics pd ON mm.pacient_diagnostic_id = pd.id
                        WHERE pdm.etiqueta = @etiqueta
                            AND pdm.dt_delete IS NULL
                            AND pd.dt_delete IS NULL
                            AND pdm.valoracio = '2'
                            AND (pd.mecanisme IS NULL OR pd.mecanisme = '')
                        ORDER BY pd.microorganisme";

                    using (var cmd = new MySqlCommand(sqlEspecials, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiqueta", etiquetaId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var microorganisme = reader["microorganisme"]?.ToString() ?? "";

                                if (!string.IsNullOrWhiteSpace(microorganisme))
                                {
                                    combinacions.Add(new CombinacioMicroorganismeMecanisme
                                    {
                                        Microorganisme = microorganisme.Trim(),
                                        Mecanismes = new List<string>()
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint combinacions de microorganisme/mecanismes per {etiquetaId}: {ex.Message}", ex);
            }

            return combinacions;
        }

        /// <summary>
        /// Obté les combinacions de microorganisme + mecanismes d'una mostra entrant
        /// NOMÉS retorna les combinacions POSITIVES: amb mecanisme de resistència o microorganisme especial
        /// </summary>
        public List<CombinacioMicroorganismeMecanisme> ObtenirCombinacionsMostraEntrant(Mostra mostra)
        {
            var combinacions = new List<CombinacioMicroorganismeMecanisme>();

            if (mostra == null || !mostra.Resultats.Any())
            {
                return combinacions;
            }

            foreach (var resultat in mostra.Resultats)
            {
                if (string.IsNullOrWhiteSpace(resultat.AillamentDescripcio))
                {
                    continue;
                }

                var mecanismes = new List<string>();

                // Recollir tots els mecanismes no nuls del resultat
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia1Id))
                    mecanismes.Add(resultat.MecanismeResistencia1Id.Trim());
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia2Id))
                    mecanismes.Add(resultat.MecanismeResistencia2Id.Trim());
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia3Id))
                    mecanismes.Add(resultat.MecanismeResistencia3Id.Trim());
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia4Id))
                    mecanismes.Add(resultat.MecanismeResistencia4Id.Trim());
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia5Id))
                    mecanismes.Add(resultat.MecanismeResistencia5Id.Trim());

                // FILTRE: només considerem combinacions POSITIVES
                // Una combinació és positiva si:
                // 1. Té mecanismes de resistència, o
                // 2. El microorganisme és especial (encara que no tingui mecanismes)
                
                bool esMicroorganismeEspecial = resultat.EsMicroorganismeEspecial ?? false;
                bool teMecanismes = mecanismes.Any();

                // Si no és positiva (ni especial ni té mecanismes), saltar aquest resultat
                if (!esMicroorganismeEspecial && !teMecanismes)
                {
                    continue;
                }

                // Obtenir el codi del microorganisme (si existeix a la taula microorganismes)
                string microorganismeCodi = resultat.AillamentDescripcio.Trim();
                try
                {
                    var microorganismeEntitat = ObtenirMicroorganisme(resultat.AillamentDescripcio);
                    if (microorganismeEntitat != null && !string.IsNullOrWhiteSpace(microorganismeEntitat.Codi))
                    {
                        microorganismeCodi = microorganismeEntitat.Codi.Trim();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"No s'ha pogut obtenir el codi del microorganisme '{resultat.AillamentDescripcio}': {ex.Message}");
                }

                // Si té mecanismes, crear una combinació individual per cada microorganisme + mecanisme
                // Això permet comparar correctament independentment de l'ordre
                if (teMecanismes)
                {
                    foreach (var mecanisme in mecanismes)
                    {
                        combinacions.Add(new CombinacioMicroorganismeMecanisme
                        {
                            Microorganisme = microorganismeCodi,
                            Mecanismes = new List<string> { mecanisme }
                        });
                    }
                }
                else if (esMicroorganismeEspecial)
                {
                    // Si no té mecanismes però és especial, crear una combinació només amb el microorganisme
                    combinacions.Add(new CombinacioMicroorganismeMecanisme
                    {
                        Microorganisme = microorganismeCodi,
                        Mecanismes = new List<string>()
                    });
                }
            }

            return combinacions;
        }

        /// <summary>
        /// Classe auxiliar per representar una combinació microorganisme + mecanismes
        /// Cada combinació representa un parell únic de microorganisme + 1 mecanisme (o sense mecanisme si és especial)
        /// </summary>
        public class CombinacioMicroorganismeMecanisme
        {
            public string Microorganisme { get; set; }
            public List<string> Mecanismes { get; set; }

            public CombinacioMicroorganismeMecanisme()
            {
                Mecanismes = new List<string>();
            }

            /// <summary>
            /// Representació en text de la combinació.
            /// Format: "MICROORGANISME+[MECANISME]" o "MICROORGANISME" si no té mecanisme
            /// </summary>
            public override string ToString()
            {
                // Normalitzar microorganisme: trim i majúscules
                string microNormalitzat = (Microorganisme ?? "").Trim().ToUpperInvariant();

                if (Mecanismes != null && Mecanismes.Any())
                {
                    // Si té mecanismes, ordenar-los i normalitzar-los
                    var mecanismesNormalitzats = Mecanismes
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Select(m => m.Trim().ToUpperInvariant())
                        .OrderBy(m => m)
                        .ToList();

                    if (mecanismesNormalitzats.Any())
                    {
                        return $"{microNormalitzat}+[{string.Join(",", mecanismesNormalitzats)}]";
                    }
                }
                
                // Si no té mecanismes
                return microNormalitzat;
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is CombinacioMicroorganismeMecanisme other)
                {
                    return string.Equals(ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }
        }

        #endregion

        #region Diagnòstics positius

        /// <summary>
        /// Obté informació d'un diagnòstic concret
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <returns>Informació del diagnòstic o null si no existeix</returns>
        public DiagnosticInfo ObtenirInformDiagnostic(int diagnosticId)
        {
            if (diagnosticId <= 0)
            {
                Logger.Warning($"ObtenirInformDiagnostic: diagnosticId invàlid ({diagnosticId})");
                return null;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT id, npat, microorganisme, mecanisme, tipus_mecanisme, data_diagnostic
                        FROM pacients_diagnostics 
                        WHERE id = @diagnosticId 
                        AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool teMemanisme = reader["mecanisme"] != DBNull.Value &&
                                                  !string.IsNullOrWhiteSpace(reader["mecanisme"].ToString());

                                return new DiagnosticInfo
                                {
                                    Id = reader.GetInt32("id"),
                                    PacientSap = reader["npat"]?.ToString(),
                                    MicroorganismeCodi = reader["microorganisme"]?.ToString(),
                                    MecanismeId = reader["mecanisme"]?.ToString(),
                                    MecanismeDescrip = reader["tipus_mecanisme"]?.ToString(),
                                    DataDiagnostic = reader["data_diagnostic"] as DateTime?,
                                    EsPositiu = teMemanisme
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint informació del diagnòstic {diagnosticId}: {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// Obté els IDs dels diagnòstics positius d'un pacient per qualsevol tipus de mostra
        /// Un diagnòstic positiu és aquell que té valoració = '2'
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <returns>Llista d'IDs de diagnòstics positius. Retorna llista buida si no n'hi ha</returns>
        public List<int> ObtenirDiagnosticsPositiusPacientAlgunTipusMostra(string pacientSap)
        {
            var diagnostics = new List<int>();

            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ObtenirDiagnosticsPositiusPacientAlgunTipusMostra: pacientSap és null o buit");
                return diagnostics;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔍 Recuperant diagnòstics positius del pacient {pacientSap} per qualsevol tipus de mostra");

                    conn.Open();

                    string sql = @"
                        SELECT DISTINCT pd.id
                        FROM pacients_diagnostics_mostra pdm 
                            INNER JOIN mostra_microorganisme mm ON pdm.id = mm.pacient_diagnostic_mostra_id
                            INNER JOIN pacients_diagnostics pd ON mm.pacient_diagnostic_id = pd.id
                            INNER JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.codi
                        WHERE pdm.npat = @pacientSap 
                            AND pdm.valoracio = '2'
                            AND pdm.dt_delete IS NULL
                            AND pd.dt_delete IS NULL  
                            AND tm.dt_delete IS NULL
                        ORDER BY pd.id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnostics.Add(reader.GetInt32("id"));
                            }
                        }
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Trobats {diagnostics.Count} diagnòstics positius per pacient {pacientSap}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint diagnòstics positius per pacient {pacientSap}: {ex.Message}", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// Obté els IDs dels diagnòstics positius vigents d'un pacient per un tipus de mostra 
        /// i els seus tipus de mostra equivalents.
        /// Un diagnòstic és positiu si té valoració = '2' i és vigent si no ha superat els dies de vigència.
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra (MOSTRA_DESCRIPCIO)</param>
        /// <returns>Llista d'IDs de diagnòstics positius vigents. Retorna llista buida si no n'hi ha</returns>
        public List<int> ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents(string pacientSap, string tipusMostra)
        {
            var diagnostics = new List<int>();

            if (string.IsNullOrWhiteSpace(pacientSap) || string.IsNullOrWhiteSpace(tipusMostra))
            {
                Logger.Warning("ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents: pacientSap o tipusMostra és null o buit");
                return diagnostics;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}🔍 Recuperant diagnòstics positius vigents del pacient {pacientSap} per tipus mostra '{tipusMostra}' o equivalents (Comprovació 2)");
                    conn.Open();

                    string sql = @"
                        SELECT DISTINCT pd.id
                        FROM pacients_diagnostics_mostra pdm	 
                            INNER JOIN mostra_microorganisme mm ON pdm.id = mm.pacient_diagnostic_mostra_id
                            INNER JOIN pacients_diagnostics pd ON mm.pacient_diagnostic_id = pd.id
                            INNER JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.codi
                        WHERE pdm.npat = @pacientSap 
                            AND pdm.valoracio = '2'
                            AND pdm.dt_delete IS NULL
                            AND pd.dt_delete IS NULL  
                            AND tm.dt_delete IS NULL 	

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
                            AND ( 
                                tm.dies_vigencia_positiu IS NULL 
                                OR pdm.data_mostra >= DATE_SUB(CURRENT_DATE, INTERVAL tm.dies_vigencia_positiu DAY) 
                            )
                        ORDER BY pd.id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnostics.Add(reader.GetInt32("id"));
                            }
                        }
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Trobats {diagnostics.Count} diagnòstics positius vigents per pacient {pacientSap} i tipus mostra '{tipusMostra}' o equivalents");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint diagnòstics positius vigents per pacient {pacientSap} i tipus mostra '{tipusMostra}': {ex.Message}", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// Obté els IDs dels diagnòstics positius d'un pacient per un tipus de mostra específic,
        /// excloent opcionalment una etiqueta concreta
        /// Un diagnòstic positiu és aquell que té mecanisme de resistència (no null/buit)
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra (MOSTRA_DESCRIPCIO)</param>
        /// <param name="etiquetaExcloure">Etiqueta a excloure de la cerca (opcional)</param>
        /// <returns>Llista d'IDs de diagnòstics positius. Retorna llista buida si no n'hi ha</returns>
        public List<int> ObtenirDiagnosticsPositiusPacientPerTipusMostra(string pacientSap, string tipusMostra, string etiquetaExcloure = null)
        {
            var diagnostics = new List<int>();

            if (string.IsNullOrWhiteSpace(pacientSap) || string.IsNullOrWhiteSpace(tipusMostra))
            {
                Logger.Warning("ObtenirDiagnosticsPositiusPacientPerTipusMostra: pacientSap o tipusMostra és null o buit");
                return diagnostics;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    string infoEtiqueta = string.IsNullOrWhiteSpace(etiquetaExcloure)
                        ? ""
                        : $" (excloent etiqueta '{etiquetaExcloure}')";

                    Logger.Info($"🔎 Buscant altres diagnòstics positius per tipus mostra '{tipusMostra}'{infoEtiqueta}");

                    conn.Open();

                    // Query per obtenir diagnòstics positius:
                    // - Del mateix pacient
                    // - Amb mecanisme de resistència (no null ni buit)
                    // - Que tenen mostres del mateix tipus de mostra
                    // - Excloent l'etiqueta especificada si s'ha proporcionat
                    string sql = @"
                        SELECT DISTINCT pd.id
                        FROM pacients_diagnostics pd
                            INNER JOIN mostra_microorganisme mm ON pd.id = mm.pacient_diagnostic_id 
                            INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id 
                        WHERE pd.npat = @pacientSap 
                            AND pd.mecanisme IS NOT NULL 
                            AND pd.mecanisme != ''
                            AND pdm.tipus_mostra_m = @tipusMostra
                            AND pd.dt_delete IS NULL
                            AND pdm.dt_delete IS NULL";

                    // Afegir condició per excloure etiqueta si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(etiquetaExcloure))
                    {
                        sql += @"
                            AND pdm.etiqueta != @etiquetaExcloure";
                    }

                    sql += @"
                        ORDER BY pd.id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra);

                        if (!string.IsNullOrWhiteSpace(etiquetaExcloure))
                        {
                            cmd.Parameters.AddWithValue("@etiquetaExcloure", etiquetaExcloure);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnostics.Add(reader.GetInt32("id"));
                            }
                        }
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Trobats {diagnostics.Count} diagnòstics positius per pacient {pacientSap} i tipus mostra '{tipusMostra}'{infoEtiqueta}");
                }

            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint diagnòstics positius per pacient {pacientSap} i tipus mostra '{tipusMostra}': {ex.Message}", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// Esborra les dades d'una mostra desvalidada (soft delete)
        /// </summary>
        /// <param name="etiquetaId">Etiqueta de la mostra a esborrar</param>
        /// <returns>True si s'ha esborrat correctament</returns>
        public bool EsborrarDadesMostra(string etiquetaId)
        {
            if (string.IsNullOrWhiteSpace(etiquetaId))
            {
                Logger.Error("EsborrarDadesMostra: etiquetaId és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Soft delete de pacients_diagnostics_mostra
                            string sqlMostra = @"UPDATE pacients_diagnostics_mostra 
                                                SET dt_delete = NOW(), dt_update = NOW()
                                                WHERE etiqueta = @etiqueta 
                                                AND dt_delete IS NULL";

                            using (var cmdMostra = new MySqlCommand(sqlMostra, conn, transaction))
                            {
                                cmdMostra.Parameters.AddWithValue("@etiqueta", etiquetaId);
                                int filesAfectades = cmdMostra.ExecuteNonQuery();
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Esborrades {filesAfectades} files de pacients_diagnostics_mostra per etiqueta {etiquetaId}");
                            }

                            // 2. Soft delete de mostra_microorganisme (utilitzant etiqueta indirectament)
                            string sqlMostraMicro = @"UPDATE mostra_microorganisme mm
                                                     INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                                                     SET mm.dt_delete = NOW(), mm.dt_update = NOW()
                                                     WHERE pdm.etiqueta = @etiqueta
                                                     AND mm.dt_delete IS NULL";

                            using (var cmdMicro = new MySqlCommand(sqlMostraMicro, conn, transaction))
                            {
                                cmdMicro.Parameters.AddWithValue("@etiqueta", etiquetaId);
                                int filesAfectades = cmdMicro.ExecuteNonQuery();
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Esborrades {filesAfectades} files de mostra_microorganisme per etiqueta {etiquetaId}");
                            }

                            transaction.Commit();
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Dades de mostra {etiquetaId} esborrades correctament");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Logger.Error($"Error en transacció d'esborrat de mostra {etiquetaId}: {ex.Message}", ex);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error esborrant dades de mostra {etiquetaId}: {ex.Message}", ex);
                return false;
            }
        }

        #endregion
    }
}
