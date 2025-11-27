using MultirIntegraModulab.Application.Helpers;
using MultirIntegraModulab.Domain.Entities;
using MySql.Data.MySqlClient;
using System;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService per gestionar el control de sincronització
    /// Permet tracking de càrregues i optimització de futures sincronitzacions
    /// </summary>
    public partial class MultiRDbService
    {
        #region Control de Sincronització

        /// <summary>
        /// Obté les dades de l'última sincronització exitosa
        /// Busca el registre més recent amb estat 'OK' o 'PARCIAL'
        /// </summary>
        /// <returns>Dades de sincronització o null si és la primera execució</returns>
        public DadesSincronitzacio ObtenirUltimaSincronitzacio()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT id, 
                               data_resultat_max_processada,
                               data_validacio_max_processada,
                               data_sincronitzacio,
                               nombre_mostres_processades,
                               nombre_mostres_error,
                               dies_revisio_seguretat,
                               estat,
                               observacions,
                               durada_segons
                        FROM integracio_modulab_sincronitzacio
                        WHERE estat IN ('OK', 'PARCIAL')
                        ORDER BY data_sincronitzacio DESC
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var dades = new DadesSincronitzacio
                                {
                                    Id = reader.GetInt32("id"),
                                    DataResultatMaxProcessada = reader["data_resultat_max_processada"] as DateTime?,
                                    DataValidacioMaxProcessada = reader["data_validacio_max_processada"] as DateTime?,
                                    DataSincronitzacio = reader.GetDateTime("data_sincronitzacio"),
                                    NombreMostresProcessades = reader.GetInt32("nombre_mostres_processades"),
                                    NombreMostresError = reader.GetInt32("nombre_mostres_error"),
                                    DiesRevisioSeguretat = reader.GetInt32("dies_revisio_seguretat"),
                                    Estat = reader["estat"]?.ToString() ?? "OK",
                                    Observacions = reader["observacions"]?.ToString(),
                                    DuradaSegons = reader["durada_segons"] != DBNull.Value 
                                        ? Convert.ToDouble(reader["durada_segons"]) 
                                        : (double?)null
                                };

                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}📊 Última sincronització: {dades.DataSincronitzacio:dd/MM/yyyy HH:mm} - {dades.NombreMostresProcessades} mostres");
                                
                                if (dades.DataResultatMaxProcessada.HasValue)
                                {
                                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Data resultat màx: {dades.DataResultatMaxProcessada:dd/MM/yyyy HH:mm}");
                                }
                                
                                if (dades.DataValidacioMaxProcessada.HasValue)
                                {
                                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Data validació màx: {dades.DataValidacioMaxProcessada:dd/MM/yyyy HH:mm}");
                                }

                                return dades;
                            }
                        }
                    }
                }

                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Principal)}ℹ️ Primera execució - no hi ha sincronitzacions prèvies");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint última sincronització: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Guarda les dades d'una nova sincronització
        /// </summary>
        /// <param name="dades">Dades de sincronització a guardar</param>
        /// <returns>ID del registre creat, 0 si ha fallat</returns>
        public int GuardarDadesSincronitzacio(DadesSincronitzacio dades)
        {
            if (dades == null)
            {
                Logger.Error("GuardarDadesSincronitzacio: dades és null");
                return 0;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        INSERT INTO integracio_modulab_sincronitzacio (
                            data_resultat_max_processada,
                            data_validacio_max_processada,
                            data_sincronitzacio,
                            nombre_mostres_processades,
                            nombre_mostres_error,
                            dies_revisio_seguretat,
                            estat,
                            observacions,
                            durada_segons,
                            dt_create,
                            dt_update
                        ) VALUES (
                            @dataResultatMax,
                            @dataValidacioMax,
                            @dataSincronitzacio,
                            @nombreMostres,
                            @nombreErrors,
                            @diesRevisio,
                            @estat,
                            @observacions,
                            @duradaSegons,
                            NOW(),
                            NOW()
                        );
                        SELECT LAST_INSERT_ID();";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@dataResultatMax", 
                            dades.DataResultatMaxProcessada.HasValue 
                                ? (object)dades.DataResultatMaxProcessada.Value 
                                : DBNull.Value);
                        
                        cmd.Parameters.AddWithValue("@dataValidacioMax", 
                            dades.DataValidacioMaxProcessada.HasValue 
                                ? (object)dades.DataValidacioMaxProcessada.Value 
                                : DBNull.Value);
                        
                        cmd.Parameters.AddWithValue("@dataSincronitzacio", dades.DataSincronitzacio);
                        cmd.Parameters.AddWithValue("@nombreMostres", dades.NombreMostresProcessades);
                        cmd.Parameters.AddWithValue("@nombreErrors", dades.NombreMostresError);
                        cmd.Parameters.AddWithValue("@diesRevisio", dades.DiesRevisioSeguretat);
                        cmd.Parameters.AddWithValue("@estat", dades.Estat ?? "OK");
                        cmd.Parameters.AddWithValue("@observacions", 
                            !string.IsNullOrWhiteSpace(dades.Observacions) 
                                ? (object)dades.Observacions 
                                : DBNull.Value);
                        cmd.Parameters.AddWithValue("@duradaSegons", 
                            dades.DuradaSegons.HasValue 
                                ? (object)dades.DuradaSegons.Value 
                                : DBNull.Value);

                        var result = cmd.ExecuteScalar();
                        int nouId = result != null ? Convert.ToInt32(result) : 0;

                        if (nouId > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Sincronització guardada correctament (ID: {nouId})");
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Mostres processades: {dades.NombreMostresProcessades}");
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Errors: {dades.NombreMostresError}");
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Estat: {dades.Estat}");
                            
                            if (dades.DuradaSegons.HasValue)
                            {
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Durada: {dades.DuradaSegons:F2}s");
                            }
                        }
                        else
                        {
                            Logger.Error("No s'ha pogut guardar la sincronització: no s'ha retornat ID");
                        }

                        return nouId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error guardant dades de sincronització: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Actualitza l'estat d'una sincronització
        /// Útil per marcar errors o canvis d'estat després de la creació
        /// </summary>
        /// <param name="id">ID de la sincronització</param>
        /// <param name="estat">Nou estat (OK, ERROR, PARCIAL)</param>
        /// <param name="observacions">Observacions adicionals (opcional)</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        public bool ActualitzarEstatSincronitzacio(int id, string estat, string observacions = null)
        {
            if (id <= 0)
            {
                Logger.Error($"ActualitzarEstatSincronitzacio: id invàlid ({id})");
                return false;
            }

            if (string.IsNullOrWhiteSpace(estat))
            {
                Logger.Error("ActualitzarEstatSincronitzacio: estat és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        UPDATE integracio_modulab_sincronitzacio
                        SET estat = @estat,
                            observacions = CASE 
                                WHEN @observacions IS NOT NULL THEN @observacions
                                ELSE observacions 
                            END,
                            dt_update = NOW()
                        WHERE id = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@estat", estat);
                        cmd.Parameters.AddWithValue("@observacions", 
                            !string.IsNullOrWhiteSpace(observacions) 
                                ? (object)observacions 
                                : DBNull.Value);

                        int filesAfectades = cmd.ExecuteNonQuery();

                        if (filesAfectades > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✅ Estat de sincronització {id} actualitzat a '{estat}'");
                            
                            if (!string.IsNullOrWhiteSpace(observacions))
                            {
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}   • Observacions: {observacions}");
                            }
                            
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"No s'ha trobat la sincronització amb ID {id}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error actualitzant estat de sincronització {id}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Neteja registres de sincronització antics per mantenir l'historial controlat
        /// </summary>
        /// <param name="diesRetencio">Nombre de dies a mantenir (per defecte 90)</param>
        /// <returns>Nombre de registres esborrats</returns>
        public int NetejarHistorialSincronitzacio(int diesRetencio = 90)
        {
            if (diesRetencio < 1)
            {
                Logger.Warning($"NetejarHistorialSincronitzacio: diesRetencio invàlid ({diesRetencio}), s'utilitza valor per defecte 90");
                diesRetencio = 90;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // Primer comptar quants registres s'esborraran
                    string sqlCount = @"
                        SELECT COUNT(*) 
                        FROM integracio_modulab_sincronitzacio
                        WHERE data_sincronitzacio < DATE_SUB(NOW(), INTERVAL @diesRetencio DAY)";

                    int registresAEsborrar = 0;
                    using (var cmdCount = new MySqlCommand(sqlCount, conn))
                    {
                        cmdCount.Parameters.AddWithValue("@diesRetencio", diesRetencio);
                        registresAEsborrar = Convert.ToInt32(cmdCount.ExecuteScalar());
                    }

                    if (registresAEsborrar == 0)
                    {
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}ℹ️ No hi ha registres de sincronització antics a esborrar (>{diesRetencio} dies)");
                        return 0;
                    }

                    // Esborrar registres antics
                    string sqlDelete = @"
                        DELETE FROM integracio_modulab_sincronitzacio
                        WHERE data_sincronitzacio < DATE_SUB(NOW(), INTERVAL @diesRetencio DAY)";

                    using (var cmdDelete = new MySqlCommand(sqlDelete, conn))
                    {
                        cmdDelete.Parameters.AddWithValue("@diesRetencio", diesRetencio);
                        int filesEsborrades = cmdDelete.ExecuteNonQuery();

                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}🗑️ Esborrats {filesEsborrades} registre(s) de sincronització amb més de {diesRetencio} dies");
                        
                        return filesEsborrades;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error netejant historial de sincronització: {ex.Message}", ex);
                return 0;
            }
        }

        #endregion
    }
}