using MultirIntegraModulab.Application.Helpers;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Enums;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

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
                Logger.Error($"Error comprobant estat del mecanisme {mecanismeCodi}: {ex.Message}", ex);
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
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Actualitzada data validació per mostra amb etiqueta {etiquetaId}");
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
                                   data_resultat, data_validacio,estat, observacions, 
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

                    // 1. Intentar amb el codi del mecanisme (tal com arriba)
                    string sql = @"SELECT COUNT(*) 
                                  FROM microorganisme_mecanisme_no_incorporar 
                                  WHERE UPPER(TRIM(microorganisme)) = UPPER(TRIM(@microorganisme))
                                  AND UPPER(TRIM(mecanisme)) = UPPER(TRIM(@mecanisme))
                                  AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@microorganisme", microorganisme);
                        cmd.Parameters.AddWithValue("@mecanisme", mecanisme);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        if (count > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Combinació '{microorganisme}' + '{mecanisme}' (codi) trobada a la taula NO INCORPORAR");
                            return true;
                        }
                    }

                    // 2. Si no es troba amb el codi, intentar amb la descripció del mecanisme
                    // Obtenir la descripció del mecanisme des de la taula mecanismes
                    string sqlDescripcio = @"SELECT descripcio 
                                            FROM mecanismes 
                                            WHERE codi = @mecanismeCodi
                                            AND dt_delete IS NULL
                                            LIMIT 1";

                    string descripcioMecanisme = null;
                    using (var cmdDesc = new MySqlCommand(sqlDescripcio, conn))
                    {
                        cmdDesc.Parameters.AddWithValue("@mecanismeCodi", mecanisme);
                        var result = cmdDesc.ExecuteScalar();
                        descripcioMecanisme = result?.ToString();
                    }

                    if (!string.IsNullOrWhiteSpace(descripcioMecanisme))
                    {
                        // Intentar amb la descripció del mecanisme
                        string sqlAmbDescripcio = @"SELECT COUNT(*) 
                                                   FROM microorganisme_mecanisme_no_incorporar 
                                                   WHERE UPPER(TRIM(microorganisme)) = UPPER(TRIM(@microorganisme))
                                                   AND UPPER(TRIM(mecanisme)) = UPPER(TRIM(@descripcioMecanisme))
                                                   AND dt_delete IS NULL";

                        using (var cmdDescripcio = new MySqlCommand(sqlAmbDescripcio, conn))
                        {
                            cmdDescripcio.Parameters.AddWithValue("@microorganisme", microorganisme);
                            cmdDescripcio.Parameters.AddWithValue("@descripcioMecanisme", descripcioMecanisme);

                            int countDescripcio = Convert.ToInt32(cmdDescripcio.ExecuteScalar());
                            
                            if (countDescripcio > 0)
                            {
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Combinació '{microorganisme}' + '{descripcioMecanisme}' (descripció) trobada a la taula NO INCORPORAR");
                                return true;
                            }
                        }
                    }

                    // 3. Si encara no es troba, intentar amb el codi del microorganisme (per si a la taula està el codi)
                    var microorganismeEntitat = ObtenirMicroorganisme(microorganisme);
                    if (microorganismeEntitat != null && !string.IsNullOrWhiteSpace(microorganismeEntitat.Codi))
                    {
                        string codiMicroorganisme = microorganismeEntitat.Codi;
                        
                        // Intentar amb codi micro + codi mecanisme
                        string sqlCodi = @"SELECT COUNT(*) 
                                          FROM microorganisme_mecanisme_no_incorporar 
                                          WHERE UPPER(TRIM(microorganisme)) = UPPER(TRIM(@codiMicroorganisme))
                                          AND UPPER(TRIM(mecanisme)) = UPPER(TRIM(@mecanisme))
                                          AND dt_delete IS NULL";

                        using (var cmdCodi = new MySqlCommand(sqlCodi, conn))
                        {
                            cmdCodi.Parameters.AddWithValue("@codiMicroorganisme", codiMicroorganisme);
                            cmdCodi.Parameters.AddWithValue("@mecanisme", mecanisme);

                            int countCodi = Convert.ToInt32(cmdCodi.ExecuteScalar());
                            
                            if (countCodi > 0)
                            {
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Combinació '{codiMicroorganisme}' (codi micro) + '{mecanisme}' (codi) trobada a la taula NO INCORPORAR");
                                return true;
                            }
                        }

                        // Intentar amb codi micro + descripció mecanisme
                        if (!string.IsNullOrWhiteSpace(descripcioMecanisme))
                        {
                            string sqlCodiDesc = @"SELECT COUNT(*) 
                                                  FROM microorganisme_mecanisme_no_incorporar 
                                                  WHERE UPPER(TRIM(microorganisme)) = UPPER(TRIM(@codiMicroorganisme))
                                                  AND UPPER(TRIM(mecanisme)) = UPPER(TRIM(@descripcioMecanisme))
                                                  AND dt_delete IS NULL";

                            using (var cmdCodiDesc = new MySqlCommand(sqlCodiDesc, conn))
                            {
                                cmdCodiDesc.Parameters.AddWithValue("@codiMicroorganisme", codiMicroorganisme);
                                cmdCodiDesc.Parameters.AddWithValue("@descripcioMecanisme", descripcioMecanisme);

                                int countCodiDesc = Convert.ToInt32(cmdCodiDesc.ExecuteScalar());
                                
                                if (countCodiDesc > 0)
                                {
                                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Combinació '{codiMicroorganisme}' (codi micro) + '{descripcioMecanisme}' (descripció) trobada a la taula NO INCORPORAR");
                                    return true;
                                }
                            }
                        }
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✓ Combinació '{microorganisme}' + '{mecanisme}' NO està a la taula NO INCORPORAR");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprobant combinació no incorporar {microorganisme}-{mecanisme}: {ex.Message}", ex);
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

                Logger.Info($"💥 Inserint auditoria amb codi '{codiResultat}'{textDescripcio}");

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
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔎 Comprovant / creant diagnostic '{microorganisme}' ['{mecanisme}' - '{tipusMecanisme}']");

                    conn.Open();

                    string sql = @"SELECT MAX(id) 
                                  FROM pacients_diagnostics 
                                  WHERE npat = @pacientSap 
                                  AND microorganisme = @microorganisme 
                                  AND mecanisme = @mecanisme 
                                  AND vigent = 'S'
                                  AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@microorganisme", microorganisme ?? "");
                        cmd.Parameters.AddWithValue("@mecanisme", mecanisme ?? "");

                        var result = cmd.ExecuteScalar();
                        int diagnosticId = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;

                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Diagnòstic del pacient {pacientSap} + '{microorganisme}' + '{mecanisme}': {(diagnosticId > 0 ? $"JA existeix (ID: {diagnosticId})" : "NO existeix")}");

                        return diagnosticId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprobant diagnòstic per pacient {pacientSap}: {ex.Message}", ex);
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
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Es procedeix a crear el diagnòstic '{microorganisme}' + '{mecanisme}'");

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
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Creat diagnòstic amb id {nouDiagnosticId} per pacient {pacientSap} microorganisme '{microorganisme}' mecanisme '{mecanisme}'");
                            return nouDiagnosticId;
                        }
                        else
                        {
                            Logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Error creant diagnòstic per pacient {pacientSap}: no s'ha retornat id");
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
        /// <param name="valoracio">Valoració de la mostra (opcional). Si té valor, es filtra per aquesta valoració</param>
        /// <param name="etiqueta">Etiqueta de la mostra (opcional). Si té valor, es filtra per aquesta etiqueta</param>
        /// <returns>ID del registre si existeix, 0 si no existeix</returns>
        public int ComprovarMostraDiagnosticExisteix(string pacientSap, DateTime? dataMostra, string tipusMostra, string valoracio = null, string etiqueta = null)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ComprovarMostraDiagnosticExisteix: pacientSap és null o buit");
                return 0;
            }

            if (!dataMostra.HasValue)
            {
                Logger.Warning("ComprovarMostraDiagnosticExists: dataMostra és null");
                return 0;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}🔎 Comprovant / creant mostra del pacient '{pacientSap}' data '{dataMostra:dd/MM/yyyy}' tipus '{tipusMostra}'");
                    conn.Open();

                    // Obtenir les dades de la mostra existent
                    string sql = @"SELECT id 
                                  FROM pacients_diagnostics_mostra 
                                  WHERE npat = @pacientSap 
                                  AND data_mostra = @dataMostra
                                  AND tipus_mostra_m = @tipusMostra
                                  AND dt_delete IS NULL";

                    // Afegir filtre per valoració si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(valoracio))
                    {
                        sql += " AND valoracio = @valoracio";
                    }

                    // Afegir filtre per etiqueta si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(etiqueta))
                    {
                        sql += " AND etiqueta = @etiqueta";
                    }

                    sql += " ORDER BY dt_create DESC LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@dataMostra", dataMostra.Value);
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra ?? "");

                        if (!string.IsNullOrWhiteSpace(valoracio))
                        {
                            cmd.Parameters.AddWithValue("@valoracio", valoracio);
                        }

                        if (!string.IsNullOrWhiteSpace(etiqueta))
                        {
                            cmd.Parameters.AddWithValue("@etiqueta", etiqueta);
                        }

                        var result = cmd.ExecuteScalar();
                        int mostraId = result != null ? Convert.ToInt32(result) : 0;

                        string infoValoracio = !string.IsNullOrWhiteSpace(valoracio) ? $" + valoració '{valoracio}'" : "";
                        string infoEtiqueta = !string.IsNullOrWhiteSpace(etiqueta) ? $" + etiqueta '{etiqueta}'" : "";
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Mostra del pacient {pacientSap} + data {dataMostra:dd/MM/yyyy} + tipus '{tipusMostra}'{infoValoracio}{infoEtiqueta}: {(mostraId > 0 ? $"JA existeix (ID: {mostraId})" : "NO existeix")}");

                        return mostraId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprobant mostra diagnòstic per pacient {pacientSap}: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Comprova si existeix una mostra diagnòstic amb una etiqueta específica
        /// </summary>
        /// <param name="pacientSap">SAP del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra</param>
        /// <param name="valoracio">Valoració de la mostra (1=negatiu, 2=positiu)</param>
        /// <param name="etiqueta">Etiqueta específica de la mostra</param>
        /// <returns>ID de la mostra si existeix, 0 si no existeix</returns>
        public int ComprovarMostraDiagnosticPerEtiqueta(string pacientSap, string tipusMostra, string valoracio, string etiqueta)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ComprovarMostraDiagnosticPerEtiqueta: pacientSap és null o buit");
                return 0;
            }

            if (string.IsNullOrWhiteSpace(tipusMostra))
            {
                Logger.Warning("ComprovarMostraDiagnosticPerEtiqueta: tipusMostra és null o buida");
                return 0;
            }

            if (string.IsNullOrWhiteSpace(valoracio))
            {
                Logger.Warning("ComprovarMostraDiagnosticPerEtiqueta: valoracio és null o buida");
                return 0;
            }

            if (string.IsNullOrWhiteSpace(etiqueta))
            {
                Logger.Warning("ComprovarMostraDiagnosticPerEtiqueta: etiqueta és null o buida");
                return 0;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT id 
                                  FROM pacients_diagnostics_mostra 
                                  WHERE npat = @pacientSap 
                                  AND tipus_mostra_m = @tipusMostra
                                  AND valoracio = @valoracio
                                  AND etiqueta = @etiqueta
                                  AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra ?? "");
                        cmd.Parameters.AddWithValue("@valoracio", valoracio);
                        cmd.Parameters.AddWithValue("@etiqueta", etiqueta);

                        var result = cmd.ExecuteScalar();
                        int mostraId = result != null ? Convert.ToInt32(result) : 0;

                        if (mostraId > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Mostra trobada per pacient {pacientSap}, tipus '{tipusMostra}', valoració '{valoracio}', etiqueta '{etiqueta}' (ID: {mostraId})");
                        }

                        return mostraId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprobant mostra diagnòstic per etiqueta {etiqueta}: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Crea un nou registre de pacients_diagnostics_mostra
        /// </summary>
        public int CrearMostraDiagnostic(string pacientSap, DateTime? dataMostra, string tipusMostra,
            string tipusProva, string etiqueta, DateTime? dataResultat, DateTime? dataValidacio,
            string mecanismeId, bool? esMicroorganismeEspecial, string microorganismeMecanismeCaptat)
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
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Es procedeix a crear la mostra");

                    conn.Open();

                    // Determinar si el microorganisme és un Virus Respiratori
                    bool esVirusRespiratori = false;
                    if (!string.IsNullOrWhiteSpace(microorganismeMecanismeCaptat))
                    {
                        var tipusMicroorganisme = ObtenirTipusMicroorganisme(microorganismeMecanismeCaptat);
                        esVirusRespiratori = (tipusMicroorganisme == TipusMicroorganisme.VirusRespiratori);
                    }

                    // Determinar si la mostra és positiva:
                    // - Té mecanisme de resistència, o
                    // - El microorganisme és especial, o
                    // - És un Virus Respiratori (sempre positiu)
                    bool esMostraPositiva = !string.IsNullOrWhiteSpace(mecanismeId) ||
                                           (esMicroorganismeEspecial.HasValue && esMicroorganismeEspecial.Value) ||
                                           esVirusRespiratori;

                    // Determinar valoració segons si la mostra és positiva (2) o negativa (1)
                    string valoracio = esMostraPositiva ? "2" : "1";

                    // Determinar estat d'integració segons si està validada o no
                    string estatIntegracio = dataValidacio.HasValue ? "V" : "P";

                    string sql = @"INSERT INTO pacients_diagnostics_mostra 
                                  (npat, data_diagnostic, data_mostra, tipus_mostra_m, usuari, 
                                   dt_create, dt_update, consolidat, valoracio, tipus_prova, 
                                   etiqueta, data_resultat, data_validacio, estat_integracio_m, microorganisme_mecanisme_captat)
                                  VALUES 
                                  (@npat, NULL, @dataMostra, @tipusMostra, 'MODULAB', 
                                   NOW(), NOW(), 'N', @valoracio, @tipusProva, 
                                   @etiqueta, @dataResultat, @dataValidacio, @estatIntegracio, @microorganismeMecanismeCaptat);
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
                        cmd.Parameters.AddWithValue("@microorganismeMecanismeCaptat", microorganismeMecanismeCaptat ?? "");

                        var result = cmd.ExecuteScalar();
                        int nouMostraId = result != null ? Convert.ToInt32(result) : 0;

                        if (nouMostraId > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Creada mostra amb id {nouMostraId} per al pacient {pacientSap} data {dataMostra:dd/MM/yyyy} tipus '{tipusMostra}' valoració '{valoracio}'");
                            return nouMostraId;
                        }
                        else
                        {
                            Logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Error creant mostra per pacient {pacientSap}: no s'ha retornat id");
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
                                    Id = reader.GetInt32("id"),
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
        /// <param name="mostraEntrant">Mostra que estàentrant</param>
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

            // Comparar dates amb tolerància de segons (menys d'1 minut = no es considera canvi)
            bool datesResultatDiferents = false;
            if (mostraExistent.DataResultat.HasValue && dataResultatEntrant.HasValue)
            {
                // Calcular diferència absoluta en segons
                var diferencia = Math.Abs((mostraExistent.DataResultat.Value - dataResultatEntrant.Value).TotalSeconds);

                // Si la diferència és 60 segons o més, es considera diferent
                datesResultatDiferents = diferencia >= 60;
            }
            else if (mostraExistent.DataResultat.HasValue != dataResultatEntrant.HasValue)
            {
                // Una és null i l'altra no, es considera diferent
                datesResultatDiferents = true;
            }

            if (datesResultatDiferents)
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

                // Identificar combinacions noves (queestan a l'entrant però no a l'existent)
                var combinacionsNoves = conjuntEntrant.Except(conjuntExistent).ToList();
                if (combinacionsNoves.Any())
                {
                    resultat.CanvisDetectats.Add($"Combinacions noves: [{string.Join(", ", combinacionsNoves)}]");
                }

                // Identificar combinacions eliminades (queestan a l'existent però no a l'entrant)
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
                                    combinacions.Add(new CombinacioMicroorganismeMecanisme(microorganisme.Trim(), mecanisme1.Trim()));
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
                                    combinacions.Add(new CombinacioMicroorganismeMecanisme(microorganisme.Trim(), ""));
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
        /// NOMÉS retorna les combinacions POSITIVES: amb mecanisme de resistència o microorganisme especial ???
        /// </summary>
        public List<CombinacioMicroorganismeMecanisme> ObtenirCombinacionsMostraEntrant(Mostra mostra)
        {
            var combinacions = new List<CombinacioMicroorganismeMecanisme>();

            if (mostra == null || !mostra.Resultats.Any())
            {
                return combinacions;
            }

            // Agrupar resultats per microorganisme i mecanismes per evitar duplicats
            // Creem una clau única per cada combinació de microorganisme + mecanismes
            var resultatsUnics = mostra.Resultats
                .GroupBy(r => new
                {
                    Microorganisme = r.AillamentDescripcio ?? "",
                    Mec1 = r.MecanismeResistencia1Id ?? "",
                    Mec2 = r.MecanismeResistencia2Id ?? "",
                    Mec3 = r.MecanismeResistencia3Id ?? "",
                    Mec4 = r.MecanismeResistencia4Id ?? "",
                    Mec5 = r.MecanismeResistencia5Id ?? "",
                    EsEspecial = r.EsMicroorganismeEspecial ?? false
                })
                .Select(g => g.First()) // Agafar només el primer de cada grup (els altres són duplicats)
                .ToList();

            // Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Resultats totals: {mostra.Resultats.Count}, Resultats únics: {resultatsUnics.Count}");

            foreach (var resultat in resultatsUnics)
            {
                if (string.IsNullOrWhiteSpace(resultat.AillamentDescripcio))
                {
                    continue;
                }

                var mecanismes = new List<string>();

                // Recollir tots els mecanismes no nuls del resultat
                // NORMALITZAR: tractar "NOCOD" com a buit per evitar falsos positius en detecció de canvis
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia1Id) && 
                    !string.Equals(resultat.MecanismeResistencia1Id, "NOCOD", StringComparison.OrdinalIgnoreCase))
                    mecanismes.Add(resultat.MecanismeResistencia1Id.Trim());
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia2Id) && 
                    !string.Equals(resultat.MecanismeResistencia2Id, "NOCOD", StringComparison.OrdinalIgnoreCase))
                    mecanismes.Add(resultat.MecanismeResistencia2Id.Trim());
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia3Id) && 
                    !string.Equals(resultat.MecanismeResistencia3Id, "NOCOD", StringComparison.OrdinalIgnoreCase))
                    mecanismes.Add(resultat.MecanismeResistencia3Id.Trim());
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia4Id) && 
                    !string.Equals(resultat.MecanismeResistencia4Id, "NOCOD", StringComparison.OrdinalIgnoreCase))
                    mecanismes.Add(resultat.MecanismeResistencia4Id.Trim());
                if (!string.IsNullOrWhiteSpace(resultat.MecanismeResistencia5Id) && 
                    !string.Equals(resultat.MecanismeResistencia5Id, "NOCOD", StringComparison.OrdinalIgnoreCase))
                    mecanismes.Add(resultat.MecanismeResistencia5Id.Trim());

                // FILTRE: només considerem combinacions POSITIVES
                // Una combinació és positiva si:
                // 1. Té mecanismes de resistència, o
                // 2. El microorganisme és especial (encara que no tingui mecanismes)

                bool esMicroorganismeEspecial = resultat.EsMicroorganismeEspecial ?? false;
                bool teMecanismes = mecanismes.Any();

                // TODOCC Tinc els mes dubtes
                // Si no és positiva (ni especial ni té mecanismes), saltar aquest resultat
                //if (!esMicroorganismeEspecial && !teMecanismes)
                //{
                //    continue;
                //}

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
                        combinacions.Add(new CombinacioMicroorganismeMecanisme(microorganismeCodi, mecanisme));
                    }
                }
                else if (esMicroorganismeEspecial)
                {
                    // Si no té mecanismes però és especial, crear una combinació només amb el microorganisme
                    combinacions.Add(new CombinacioMicroorganismeMecanisme(microorganismeCodi, ""));
                }
                else
                {
                    // Si no té mecanismes, crear una combinació només amb el microorganisme (sense NOCOD)
                    combinacions.Add(new CombinacioMicroorganismeMecanisme(microorganismeCodi, ""));
                }
            }

            return combinacions;
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
                return null;
            }

            return null;
        }

        /// <summary>
        /// Comprova si un diagnòstic té alguna mostra associada amb una etiqueta específica
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <param name="etiqueta">Etiqueta de la mostra a comprovar</param>
        /// <param name="tipusMostra">Tipus de mostra per filtrar</param>
        /// <returns>True si el diagnòstic té alguna mostra amb aquesta etiqueta, False en cas contrari</returns>
        public bool DiagnosticTeMostraAmbEtiqueta(int diagnosticId, string etiqueta, string tipusMostra)
        {
            if (diagnosticId <= 0)
            {
                Logger.Warning($"DiagnosticTeMostraAmbEtiqueta: diagnosticId invàlid ({diagnosticId})");
                return false;
            }

            if (string.IsNullOrWhiteSpace(etiqueta))
            {
                Logger.Warning("DiagnosticTeMostraAmbEtiqueta: etiqueta és null o buida");
                return false;
            }

            if (string.IsNullOrWhiteSpace(tipusMostra))
            {
                Logger.Warning("DiagnosticTeMostraAmbEtiqueta: tipusMostra és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT COUNT(*) 
                        FROM mostra_microorganisme mm
                        INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                        WHERE mm.pacient_diagnostic_id = @diagnosticId
                          AND pdm.etiqueta = @etiqueta
                          AND pdm.tipus_mostra_m = @tipusMostra
                          AND pdm.dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);
                        cmd.Parameters.AddWithValue("@etiqueta", etiqueta);
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprobant si diagnòstic {diagnosticId} té mostra amb etiqueta {etiqueta} i tipus {tipusMostra}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Obté els IDs dels diagnòstics positius d'un pacient per qualsevol tipus de mostra
        /// Un diagnòstic positiu és aquell que té valoració = '2'
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="etiqueta">Etiqueta a excloure de la cerca (opcional)</param>
        /// <returns>Llista d'IDs de diagnòstics positius. Retorna llista buida si no n'hi ha</returns>
        public List<int> ObtenirDiagnosticsPositiusPacientAlgunTipusMostra(string pacientSap, string etiqueta = null)
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
                    string infoEtiqueta = string.IsNullOrWhiteSpace(etiqueta)
                        ? ""
                        : $" excloent etiqueta '{etiqueta}'";

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}🔍 Recuperant diagnòstics positius del pacient {pacientSap} per qualsevol tipus de mostra{infoEtiqueta}");

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
                            AND tm.dt_delete IS NULL";

                    // Afegir condició per excloure etiqueta si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(etiqueta))
                    {
                        sql += @"
                            AND (pdm.etiqueta != @etiqueta OR pdm.etiqueta IS NULL)";
                    }

                    sql += @"
                        ORDER BY pd.id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);

                        if (!string.IsNullOrWhiteSpace(etiqueta))
                        {
                            cmd.Parameters.AddWithValue("@etiqueta", etiqueta);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnostics.Add(reader.GetInt32("id"));
                            }
                        }
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Trobats {diagnostics.Count} diagnòstic(s) positiu(s) per pacient {pacientSap}{infoEtiqueta}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint diagnòstics positius per pacient {pacientSap}: {ex.Message}", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// Obté els IDs dels diagnòstics positius d'un pacient per un tipus de mostra específic,
        /// excloent opcionalment una etiqueta concreta
        /// Un diagnòstic positiu és aquell que té mecanisme de resistència (no null/buit)
        /// NOMÉS retorna diagnòstics de microorganismes multiresistents (exclou virus respiratoris).
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra (MOSTRA_DESCRIPCIO)</param>
        /// <param name="etiquetaExcloure">Etiqueta a excloure de la cerca (opcional)</param>
        /// <param name="microorganisme">Microorganisme per filtrar (opcional)</param>
        /// <param name="mecanisme">Mecanisme de resistència per filtrar (opcional)</param>
        /// <returns>Llista d'IDs de diagnòstics positius. Retorna llista buida si no n'hi ha</returns>
        public List<int> ObtenirDiagnosticsPositiusPacientPerTipusMostra(string pacientSap, string tipusMostra, string etiquetaExcloure = null, string microorganisme = null, string mecanisme = null)
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
                        : $" excloent etiqueta '{etiquetaExcloure}'";

                    string infoFiltres = "";
                    if (!string.IsNullOrWhiteSpace(microorganisme))
                        infoFiltres += $" i microorganisme '{microorganisme}'";
                    if (!string.IsNullOrWhiteSpace(mecanisme))
                        infoFiltres += $" i mecanisme '{mecanisme}'";

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔎 Buscant altres diagnòstics positius per tipus mostra '{tipusMostra}' {infoEtiqueta} {infoFiltres}");

                    conn.Open();

                    // Query per obtenir diagnòstics positius:
                    // - Del mateix pacient
                    // - Amb mecanisme de resistència (no null ni buit)
                    // - Que tenen mostres del mateix tipus de mostra
                    // - Excloent l'etiqueta especificada si s'ha proporcionat
                    // - Filtrant per microorganisme i/o mecanisme si s'han proporcionat
                    // NOMÉS microorganismes multiresistents (tipus = 'M' o NULL, excloent tipus = 'R')

                    string sql = @"
                        SELECT DISTINCT pd.id
                        FROM pacients_diagnostics pd
                            INNER JOIN mostra_microorganisme mm ON pd.id = mm.pacient_diagnostic_id 
                            INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id 
                            INNER JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.codi
                            LEFT JOIN microorganismes m ON pd.microorganisme = m.descripcio 
                                AND m.dt_delete IS NULL 
                                AND m.actiu = 1
                        WHERE pd.npat = @pacientSap 
                            AND pdm.valoracio = '2'
                            AND pdm.tipus_mostra_m = @tipusMostra
                            AND pdm.dt_delete IS NULL
                            AND pd.dt_delete IS NULL
                            AND pd.vigent = 'S'
                            AND (m.tipus IS NULL OR m.tipus != 'R')";

                    // Afegir condició per excloure etiqueta si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(etiquetaExcloure))
                    {
                        sql += @"
                            AND (pdm.etiqueta != @etiquetaExcloure OR pdm.etiqueta IS NULL)";
                    }

                    // Afegir condició per filtrar per microorganisme si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(microorganisme))
                    {
                        sql += @"
                            AND pd.microorganisme != @microorganisme";
                    }

                    // Afegir condició per filtrar per mecanisme si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(mecanisme))
                    {
                        sql += @"
                            AND pd.mecanisme != @mecanisme";
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

                        if (!string.IsNullOrWhiteSpace(microorganisme))
                        {
                            cmd.Parameters.AddWithValue("@microorganisme", microorganisme);
                        }

                        if (!string.IsNullOrWhiteSpace(mecanisme))
                        {
                            cmd.Parameters.AddWithValue("@mecanisme", mecanisme);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnostics.Add(reader.GetInt32("id"));
                            }
                        }
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Trobats {diagnostics.Count} diagnòstic(s) positiu(s) per pacient {pacientSap} i tipus mostra '{tipusMostra}' {infoEtiqueta} {infoFiltres}");

                    // Mostrar els IDs trobats al log
                    if (diagnostics.Any())
                    {
                        string idsText = string.Join(", ", diagnostics);
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}IDs dels diagnòstics positius trobats: [{idsText}]");
                    }

                }

            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint diagnòstics positius per pacient {pacientSap} i tipus mostra '{tipusMostra}' i equivalents: {ex.Message}", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// Obté els IDs dels diagnòstics positius VIGENTS d'un pacient per un tipus de mostra específic,
        /// excloent opcionalment una etiqueta concreta.
        /// Un positiu és vigent si no ha superat els dies_vigencia_positiu del tipus de mostra.
        /// </summary>
        public List<int> ObtenirDiagnosticsPositiusVigentsTipusMostra(string pacientSap, string tipusMostra, string etiqueta = null)
        {
            var diagnostics = new List<int>();

            if (string.IsNullOrWhiteSpace(pacientSap) || string.IsNullOrWhiteSpace(tipusMostra))
            {
                Logger.Warning("ObtenirDiagnosticsPositiusVigentsTipusMostra: pacientSap o tipusMostra és null o buit");
                return diagnostics;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    string infoEtiqueta = string.IsNullOrWhiteSpace(etiqueta)
                        ? ""
                        : $" excloent etiqueta '{etiqueta}'";

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔎 Buscant diagnòstics positius VIGENTS per tipus mostra '{tipusMostra}' {infoEtiqueta}");

                    conn.Open();

                    // Query per obtenir diagnòstics positius per tipus de mostra i equivalents
                    // NOMÉS microorganismes multiresistents (tipus = 'M' o NULL, excloent tipus = 'R')
                    string sql = @"
                        SELECT DISTINCT pd.id
                        FROM pacients_diagnostics pd
                            INNER JOIN mostra_microorganisme mm ON pd.id = mm.pacient_diagnostic_id 
                            INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id 
                            INNER JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.codi
                            LEFT JOIN microorganismes m ON pd.microorganisme = m.descripcio 
                                AND m.dt_delete IS NULL 
                                AND m.actiu = 1
                        WHERE pd.npat = @pacientSap 
                            AND pdm.valoracio = '2'
                            AND pdm.tipus_mostra_m = @tipusMostra
                            AND ( 
                                tm.dies_vigencia_positiu IS NULL 
                                OR pdm.data_mostra >= DATE_SUB(CURRENT_DATE, INTERVAL tm.dies_vigencia_positiu DAY) 
                            )
                            AND pdm.dt_delete IS NULL
                            AND pd.dt_delete IS NULL
                            AND pd.vigent = 'S'
                            AND (m.tipus IS NULL OR m.tipus != 'R')";

                    // Afegir condició per excloure etiqueta si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(etiqueta))
                    {
                        sql += @"
                            AND (pdm.etiqueta != @etiqueta OR pdm.etiqueta IS NULL)";
                    }

                    sql += @"
                        ORDER BY pd.id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra);

                        if (!string.IsNullOrWhiteSpace(etiqueta))
                        {
                            cmd.Parameters.AddWithValue("@etiqueta", etiqueta);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnostics.Add(reader.GetInt32("id"));
                            }
                        }
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Trobats {diagnostics.Count} diagnòstic(s) positius VIGENT(s) per pacient {pacientSap} i tipus mostra '{tipusMostra}' {infoEtiqueta}");

                    // Mostrar els IDs trobats al log
                    if (diagnostics.Any())
                    {
                        string idsText = string.Join(", ", diagnostics);
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}IDs dels diagnòstics positius trobats: [{idsText}]");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint diagnòstics positius vigents per pacient {pacientSap} i tipus mostra '{tipusMostra}': {ex.Message}", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// Obté els IDs dels diagnòstics positius d'un pacient per un tipus de mostra específic i els seus equivalents,
        /// excloent opcionalment una etiqueta concreta.
        /// Un diagnòstic positiu és aquell que té mecanisme de resistència (no null/buit).
        /// NOMÉS retorna diagnòstics de microorganismes multiresistents (exclou virus respiratoris).
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra (MOSTRA_DESCRIPCIO)</param>
        /// <param name="etiquetaExcloure">Etiqueta a excloure de la cerca (opcional)</param>
        /// <param name="microorganisme">Microorganisme per filtrar (opcional)</param>
        /// <param name="mecanisme">Mecanisme de resistència per filtrar (opcional)</param>
        /// <returns>Llista d'IDs de diagnòstics positius. Retorna llista buida si no n'hi ha</returns>
        public List<int> ObtenirDiagnosticsPositiusPacientPerTipusMostraIEquivalents(string pacientSap, string tipusMostra, string etiquetaExcloure = null, string microorganisme = null, string mecanisme = null)
        {
            var diagnostics = new List<int>();

            if (string.IsNullOrWhiteSpace(pacientSap) || string.IsNullOrWhiteSpace(tipusMostra))
            {
                Logger.Warning("ObtenirDiagnosticsPositiusPacientPerTipusMostraIEquivalents: pacientSap o tipusMostra és null o buit");
                return diagnostics;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    string infoEtiqueta = string.IsNullOrWhiteSpace(etiquetaExcloure)
                        ? ""
                        : $" excloent etiqueta '{etiquetaExcloure}'";

                    string infoFiltres = "";
                    if (!string.IsNullOrWhiteSpace(microorganisme))
                        infoFiltres += $" i microorganisme '{microorganisme}'";
                    if (!string.IsNullOrWhiteSpace(mecanisme))
                        infoFiltres += $" i mecanisme '{mecanisme}'";

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔎 Buscant altres diagnòstics positius MULTIRESISTENTS per tipus mostra '{tipusMostra}' i equivalents {infoEtiqueta} {infoFiltres}");

                    conn.Open();

                    // Query per obtenir diagnòstics positius per tipus de mostra i equivalents
                    // NOMÉS microorganismes multiresistents (tipus = 'M' o NULL, excloent tipus = 'R')
                    string sql = @"
                        SELECT DISTINCT pd.id
                        FROM pacients_diagnostics pd
                            INNER JOIN mostra_microorganisme mm ON pd.id = mm.pacient_diagnostic_id 
                            INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id 
                            INNER JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.codi
                            LEFT JOIN microorganismes m ON pd.microorganisme = m.descripcio 
                                AND m.dt_delete IS NULL 
                                AND m.actiu = 1
                        WHERE pd.npat = @pacientSap 
                            AND pdm.valoracio = '2'
                            AND pdm.tipus_mostra_m = @tipusMostra
                            AND pdm.dt_delete IS NULL
                            AND pd.dt_delete IS NULL
                            AND pd.vigent = 'S'
                            AND (m.tipus IS NULL OR m.tipus != 'R')";

                    // Afegir condició per excloure etiqueta si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(etiquetaExcloure))
                    {
                        sql += @"
                            AND (pdm.etiqueta != @etiquetaExcloure OR pdm.etiqueta IS NULL)";
                    }

                    // Afegir condició per filtrar per microorganisme si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(microorganisme))
                    {
                        sql += @"
                            AND pd.microorganisme != @microorganisme";
                    }

                    // Afegir condició per filtrar per mecanisme si s'ha proporcionat
                    if (!string.IsNullOrWhiteSpace(mecanisme))
                    {
                        sql += @"
                            AND pd.mecanisme != @mecanisme";
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

                        if (!string.IsNullOrWhiteSpace(microorganisme))
                        {
                            cmd.Parameters.AddWithValue("@microorganisme", microorganisme);
                        }

                        if (!string.IsNullOrWhiteSpace(mecanisme))
                        {
                            cmd.Parameters.AddWithValue("@mecanisme", mecanisme);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnostics.Add(reader.GetInt32("id"));
                            }
                        }
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Trobats {diagnostics.Count} diagnòstic(s) positiu(s) multiresistents(s) per pacient {pacientSap} i tipus mostra '{tipusMostra}' i equivalents {infoEtiqueta} {infoFiltres}");

                    // Mostrar els IDs trobats al log
                    if (diagnostics.Any())
                    {
                        string idsText = string.Join(", ", diagnostics);
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}IDs dels diagnòstics positius multiresistents trobats: [{idsText}]");
                    }

                }

            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint diagnòstics positius multiresistents per pacient {pacientSap} i tipus mostra '{tipusMostra}' i equivalents: {ex.Message}", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// Esborra les dades d'una mostra (soft delete)
        /// També esborra el diagnòstic en el cas que quedi orfe (que no tenen cap altra mostra associada)
        /// </summary>
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

                    // IMPORTANT : fem anar una Transacció

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 0. Obtenir els diagnòstics associats a aquesta mostra
                            var diagnosticsIds = new List<int>();

                            string sqlDiagnostics = @"
                                SELECT DISTINCT mm.pacient_diagnostic_id
                                FROM mostra_microorganisme mm
                                INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                                WHERE pdm.etiqueta = @etiqueta
                                  AND pdm.dt_delete IS NULL";

                            using (var cmdDiag = new MySqlCommand(sqlDiagnostics, conn, transaction))
                            {
                                cmdDiag.Parameters.AddWithValue("@etiqueta", etiquetaId);

                                using (var reader = cmdDiag.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        diagnosticsIds.Add(reader.GetInt32(0));
                                    }
                                }
                            }

                            // Mostrar els IDs dels diagnòstics trobats
                            string idsDiagnostics = string.Join(", ", diagnosticsIds);

                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Trobats {diagnosticsIds.Count} diagnòstic(s) associat(s) a la mostra {etiquetaId} : {idsDiagnostics}");

                            // 1. Esborrar mostra_microorganisme (DELETE directe ja que no té dt_delete)
                            string sqlMostraMicro = @"
                                DELETE mm
                                FROM mostra_microorganisme mm
                                INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                                WHERE pdm.etiqueta = @etiqueta";

                            using (var cmdMicro = new MySqlCommand(sqlMostraMicro, conn, transaction))
                            {
                                cmdMicro.Parameters.AddWithValue("@etiqueta", etiquetaId);
                                int filesAfectades = cmdMicro.ExecuteNonQuery();
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Esborrades {filesAfectades} files de mostra_microorganisme per etiqueta {etiquetaId}");
                            }

                            // 2. Soft delete de pacients_diagnostics_mostra
                            string sqlMostra = @"
                                UPDATE pacients_diagnostics_mostra 
                                SET dt_delete = NOW(), dt_update = NOW()
                                WHERE etiqueta = @etiqueta 
                                  AND dt_delete IS NULL";

                            using (var cmdMostra = new MySqlCommand(sqlMostra, conn, transaction))
                            {
                                cmdMostra.Parameters.AddWithValue("@etiqueta", etiquetaId);
                                int filesAfectades = cmdMostra.ExecuteNonQuery();
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Esborrades {filesAfectades} files de pacients_diagnostics_mostra per etiqueta {etiquetaId}");
                            }

                            // 3. Esborrar diagnòstics orfes (soft delete)
                            // Per cada diagnòstic associat, comprovar si té altres mostres
                            int diagnosticsOrfesEsborrats = 0;

                            foreach (var diagnosticId in diagnosticsIds)
                            {
                                // Comprovar si aquest diagnòstic té altres mostres associades
                                string sqlComprovarMostres = @"
                                    SELECT COUNT(*) 
                                    FROM mostra_microorganisme mm
                                    INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                                    WHERE mm.pacient_diagnostic_id = @diagnosticId
                                      AND pdm.dt_delete IS NULL";

                                int nombreMostres = 0;
                                using (var cmdComprovar = new MySqlCommand(sqlComprovarMostres, conn, transaction))
                                {
                                    cmdComprovar.Parameters.AddWithValue("@diagnosticId", diagnosticId);
                                    nombreMostres = Convert.ToInt32(cmdComprovar.ExecuteScalar());
                                }

                                // Si no té cap altra mostra, esborrar el diagnòstic (soft delete)
                                if (nombreMostres == 0)
                                {
                                    string sqlEsborrarDiagnostic = @"
                                        UPDATE pacients_diagnostics
                                        SET dt_delete = NOW(), dt_update = NOW()
                                        WHERE id = @diagnosticId
                                          AND dt_delete IS NULL";

                                    using (var cmdEsborrar = new MySqlCommand(sqlEsborrarDiagnostic, conn, transaction))
                                    {
                                        cmdEsborrar.Parameters.AddWithValue("@diagnosticId", diagnosticId);
                                        int filesAfectades = cmdEsborrar.ExecuteNonQuery();

                                        if (filesAfectades > 0)
                                        {
                                            diagnosticsOrfesEsborrats++;
                                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Esborrat diagnòstic orfe {diagnosticId} (no té cap altra mostra associada)");
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Diagnòstic {diagnosticId} mantingut (té {nombreMostres} altra(es) mostra(es) associada(es))");
                                }
                            }

                            if (diagnosticsOrfesEsborrats > 0)
                            {
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Total diagnòstics orfes esborrats: {diagnosticsOrfesEsborrats}");
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
