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
    /// Extensions per MultiRDbService per gestionar l'historial de mostres
    /// </summary>
    public partial class MultiRDbService
    {
        /// <summary>
        /// Obté estadístiques generals de l'historial de mostres
        /// </summary>
        /// <returns>Estadístiques de l'historial</returns>
        public EstadistiquesHistorial ObtenirEstadistiquesHistorial()
        {
            var estadistiques = new EstadistiquesHistorial();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // Comptar registres totals
                    string sqlTotal = @"SELECT COUNT(*) 
                                       FROM pacients_diagnostics_mostra_historial 
                                       WHERE dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sqlTotal, conn))
                    {
                        estadistiques.TotalRegistresHistorial = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    if (estadistiques.TotalRegistresHistorial > 0)
                    {
                        // Obtenir dates extremes
                        string sqlDates = @"SELECT MIN(data_canvi) as primer, MAX(data_canvi) as ultim
                                           FROM pacients_diagnostics_mostra_historial 
                                           WHERE dt_delete IS NULL AND data_canvi IS NOT NULL";

                        using (var cmd = new MySqlCommand(sqlDates, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    estadistiques.PrimerRegistre = reader["primer"] as DateTime?;
                                    estadistiques.UltimRegistre = reader["ultim"] as DateTime?;
                                }
                            }
                        }

                        // Obtenir distribució per tipus de canvi
                        string sqlTipus = @"SELECT tipus_canvi, COUNT(*) as total
                                           FROM pacients_diagnostics_mostra_historial 
                                           WHERE dt_delete IS NULL 
                                           GROUP BY tipus_canvi
                                           ORDER BY total DESC";

                        using (var cmd = new MySqlCommand(sqlTipus, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string tipus = reader["tipus_canvi"]?.ToString() ?? "DESCONEGUT";
                                    int total = Convert.ToInt32(reader["total"]);
                                    estadistiques.RegistresPerTipus[tipus] = total;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint estadístiques d'historial: {ex.Message}", ex);
                // Retornar estadístiques buides en cas d'error
                estadistiques = new EstadistiquesHistorial();
            }

            return estadistiques;
        }

        /// <summary>
        /// Comprova si existeix historial per una mostra específica
        /// </summary>
        /// <param name="etiquetaId">Identificador de l'etiqueta</param>
        /// <returns>Nombre de registres d'historial per la mostra</returns>
        public int ComprovarHistorialExisteix(string etiquetaId)
        {
            if (string.IsNullOrWhiteSpace(etiquetaId))
            {
                Logger.Warning("ComprovarHistorialExisteix: etiquetaId és null o buit");
                return 0;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT COUNT(*) 
                                  FROM pacients_diagnostics_mostra_historial 
                                  WHERE etiqueta = @etiquetaId 
                                  AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiquetaId", etiquetaId);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error comprovant historial per {etiquetaId}: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Obté l'historial complet d'una mostra
        /// </summary>
        /// <param name="etiquetaId">Identificador de l'etiqueta</param>
        /// <returns>Llista ordenada de registres d'historial (més recent primer)</returns>
        public List<RegistreHistorialMostra> ObtenirHistorialMostra(string etiquetaId)
        {
            var historial = new List<RegistreHistorialMostra>();

            if (string.IsNullOrWhiteSpace(etiquetaId))
            {
                Logger.Warning("ObtenirHistorialMostra: etiquetaId és null o buit");
                return historial;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT id, etiqueta, versio, tipus_canvi, 
                                         combinacions_anteriors, data_resultat_anterior, data_validacio_anterior,
                                         combinacions_noves, data_resultat_nova, data_validacio_nova,
                                         data_canvi, proces_origen, dt_create
                                  FROM pacients_diagnostics_mostra_historial 
                                  WHERE etiqueta = @etiquetaId 
                                  AND dt_delete IS NULL
                                  ORDER BY versio DESC, data_canvi DESC, dt_create DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiquetaId", etiquetaId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var registre = new RegistreHistorialMostra
                                {
                                    Id = reader.GetInt32("id"),
                                    EtiquetaOriginal = reader["etiqueta"]?.ToString(),
                                    Versio = reader["versio"] != DBNull.Value ? Convert.ToInt32(reader["versio"]) : 0,
                                    TipusCanvi = reader["tipus_canvi"]?.ToString(),
                                    CombinacionsAnteriors = reader["combinacions_anteriors"]?.ToString(),
                                    DataResultatAnterior = reader["data_resultat_anterior"] as DateTime?,
                                    DataValidacioAnterior = reader["data_validacio_anterior"] as DateTime?,
                                    CombinacionsNoves = reader["combinacions_noves"]?.ToString(),
                                    DataResultatNova = reader["data_resultat_nova"] as DateTime?,
                                    DataValidacioNova = reader["data_validacio_nova"] as DateTime?,
                                    DataCanvi = reader["data_canvi"] as DateTime?,
                                    ProcesOrigen = reader["proces_origen"]?.ToString(),
                                    DataCreacio = reader["dt_create"] != DBNull.Value ? Convert.ToDateTime(reader["dt_create"]) : DateTime.MinValue
                                };

                                historial.Add(registre);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint historial per {etiquetaId}: {ex.Message}", ex);
            }

            return historial;
        }

        /// <summary>
        /// Guarda un registre d'historial per una mostra amb tota la informació
        /// </summary>
        /// <param name="etiquetaId">Identificador de l'etiqueta</param>
        /// <param name="tipusCanvi">Tipus de canvi realitzat (VALIDADA_AMB_CANVIS, REVALIDADA_AMB_CANVIS, DESVALIDADA_AMB_CANVIS)</param>
        /// <param name="combinacionsAnteriors">Combinacions microorganisme+mecanisme anteriors (JSON o text)</param>
        /// <param name="dataResultatAnterior">Data resultat anterior</param>
        /// <param name="dataValidacioAnterior">Data validació anterior</param>
        /// <param name="combinacionsNoves">Combinacions microorganisme+mecanisme noves (JSON o text)</param>
        /// <param name="dataResultatNova">Data resultat nova</param>
        /// <param name="dataValidacioNova">Data validació nova</param>
        /// <param name="npat">Número de pacient (NPAT)</param>
        /// <returns>True si s'ha guardat correctament</returns>
        public bool GuardarHistorialMostra(
            string etiquetaId, 
            string tipusCanvi, 
            string combinacionsAnteriors = null,
            DateTime? dataResultatAnterior = null,
            DateTime? dataValidacioAnterior = null,
            string combinacionsNoves = null,
            DateTime? dataResultatNova = null,
            DateTime? dataValidacioNova = null,
            string npat = null)
        {
            if (string.IsNullOrWhiteSpace(etiquetaId))
            {
                Logger.Error("GuardarHistorialMostra: etiquetaId és null o buit");
                return false;
            }

            if (string.IsNullOrWhiteSpace(tipusCanvi))
            {
                Logger.Error("GuardarHistorialMostra: tipusCanvi és null o buit");
                return false;
            }

            // Validar que tipusCanvi sigui un dels valors permesos
            var tipusPermesos = new[] { "VALIDADA_AMB_CANVIS", "REVALIDADA_AMB_CANVIS", "DESVALIDADA_AMB_CANVIS" };
            if (!tipusPermesos.Contains(tipusCanvi))
            {
                Logger.Error($"GuardarHistorialMostra: tipusCanvi '{tipusCanvi}' no és vàlid. Ha de ser: {string.Join(", ", tipusPermesos)}");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // Obtenir la versió següent per aquesta etiqueta
                    int versioNova = ObtenirProperaVersio(conn, etiquetaId);

                    string sql = @"INSERT INTO pacients_diagnostics_mostra_historial 
                                  (etiqueta, versio, tipus_canvi, 
                                   combinacions_anteriors, data_resultat_anterior, data_validacio_anterior,
                                   combinacions_noves, data_resultat_nova, data_validacio_nova,
                                   data_canvi, proces_origen, npat)
                                  VALUES 
                                  (@etiquetaId, @versio, @tipusCanvi,
                                   @combinacionsAnteriors, @dataResultatAnterior, @dataValidacioAnterior,
                                   @combinacionsNoves, @dataResultatNova, @dataValidacioNova,
                                   NOW(), 'IntegracioModulab', @npat)";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@etiquetaId", etiquetaId);
                        cmd.Parameters.AddWithValue("@versio", versioNova);
                        cmd.Parameters.AddWithValue("@tipusCanvi", tipusCanvi);
                        cmd.Parameters.AddWithValue("@combinacionsAnteriors", combinacionsAnteriors ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@dataResultatAnterior", dataResultatAnterior ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@dataValidacioAnterior", dataValidacioAnterior ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@combinacionsNoves", combinacionsNoves ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@dataResultatNova", dataResultatNova ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@dataValidacioNova", dataValidacioNova ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@npat", npat ?? (object)DBNull.Value);

                        int filesAfectades = cmd.ExecuteNonQuery();

                        if (filesAfectades > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📋 Registre d'historial (v{versioNova}) guardat per {etiquetaId}: {tipusCanvi}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'han afectat files guardant historial per {etiquetaId}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error guardant historial per {etiquetaId}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Obté la propera versió per una etiqueta
        /// </summary>
        private int ObtenirProperaVersio(MySqlConnection conn, string etiquetaId)
        {
            string sql = @"SELECT COALESCE(MAX(versio), 0) + 1 
                          FROM pacients_diagnostics_mostra_historial 
                          WHERE etiqueta = @etiquetaId";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@etiquetaId", etiquetaId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Guarda historial automàticament quan es detecten canvis en mostres validades/revalidades/desvalidades
        /// NOTA: Aquest mètode està obsolet i ja no s'utilitza. Utilitzeu GuardarHistorialMostra amb tots els paràmetres.
        /// </summary>
        /// <param name="mostra">Mostra que ha canviat</param>
        /// <param name="tipusIncorporacio">Tipus d'incorporació detectat</param>
        /// <param name="observacions">Observacions opcionals sobre el canvi</param>
        /// <returns>True si s'ha guardat correctament</returns>
        [Obsolete("Utilitzeu GuardarHistorialMostra amb tots els paràmetres en lloc d'aquest mètode")]
        public bool GuardarHistorialAutomaticMostra(Mostra mostra, TipusIncorporacio tipusIncorporacio, string observacions = null)
        {
            if (mostra == null)
            {
                Logger.Error("GuardarHistorialAutomaticMostra: mostra és null");
                return false;
            }

            // Només guardar historial per canvis significatius
            string tipusCanvi = null;
            switch (tipusIncorporacio)
            {
                case TipusIncorporacio.Desvalidada:
                    tipusCanvi = "DESVALIDADA_AMB_CANVIS";
                    break;
                case TipusIncorporacio.Validada:
                    tipusCanvi = "VALIDADA_AMB_CANVIS";
                    break;
                case TipusIncorporacio.Revalidada:
                    tipusCanvi = "REVALIDADA_AMB_CANVIS";
                    break;
                default:
                    // No guardar historial per altres tipus
                    return true;
            }

            // Obtenir informació dels microorganismes i mecanismes de la mostra
            var microorganismes = mostra.Microorganismes;
            var mecanismes = mostra.MecanismesResistencia;

            string infoMicroorganismes = microorganismes.Any() ? string.Join(", ", microorganismes) : null;
            string infoMecanismes = mecanismes.Any() ? string.Join(", ", mecanismes) : null;

            string combinacionsNoves = observacions;
            if (!string.IsNullOrEmpty(infoMicroorganismes) || !string.IsNullOrEmpty(infoMecanismes))
            {
                var detalls = new List<string>();
                if (!string.IsNullOrEmpty(infoMicroorganismes))
                    detalls.Add($"Microorganismes: {infoMicroorganismes}");
                if (!string.IsNullOrEmpty(infoMecanismes))
                    detalls.Add($"Mecanismes: {infoMecanismes}");

                string detallsText = string.Join("; ", detalls);
                combinacionsNoves = string.IsNullOrEmpty(observacions) ? detallsText : $"{observacions}. {detallsText}";
            }

            // Cridar el mètode nou amb només les dades disponibles
            return GuardarHistorialMostra(
                mostra.EtiquetaId,
                tipusCanvi,
                null, // combinacionsAnteriors - no disponible aquí
                null, // dataResultatAnterior - no disponible aquí
                null, // dataValidacioAnterior - no disponible aquí
                combinacionsNoves,
                mostra.DataUltimResultat,
                mostra.Resultats.FirstOrDefault()?.DataValidacio);
        }

        /// <summary>
        /// Neteja registres d'historial més antics de X dies (per manteniment)
        /// </summary>
        /// <param name="diesRetencio">Dies de retenció (per defecte 90 dies)</param>
        /// <returns>Nombre de registres eliminats</returns>
        public int NetejarHistorialAntic(int diesRetencio = 90)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"UPDATE pacients_diagnostics_mostra_historial 
                                  SET dt_delete = NOW()
                                  WHERE data_canvi < DATE_SUB(NOW(), INTERVAL @diesRetencio DAY)
                                  AND dt_delete IS NULL";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diesRetencio", diesRetencio);
                        int registresNetejats = cmd.ExecuteNonQuery();

                        if (registresNetejats > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}🗑️ Netejats {registresNetejats} registres d'historial anteriors a {diesRetencio} dies");
                        }

                        return registresNetejats;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error netejant historial antic: {ex.Message}", ex);
                return 0;
            }
        }
    }
}