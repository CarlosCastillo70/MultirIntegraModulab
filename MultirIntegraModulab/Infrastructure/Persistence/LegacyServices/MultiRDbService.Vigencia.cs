using MultirIntegraModulab.Application.Helpers;
using MultirIntegraModulab.Domain.Entities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService per gestionar la vigència dels diagnòstics
    /// </summary>
    public partial class MultiRDbService
    {
        #region Vigència de Diagnòstics

        /// <summary>
        /// Marca un diagnòstic com a no vigent
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <param name="responsable">Usuari que marca com a no vigent</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        public bool MarcarDiagnosticNoVigent(int diagnosticId, string responsable)
        {
            if (diagnosticId <= 0)
            {
                Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}MarcarDiagnosticNoVigent: diagnosticId invàlid ({diagnosticId})");
                return false;
            }

            if (string.IsNullOrWhiteSpace(responsable))
            {
                Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}MarcarDiagnosticNoVigent: responsable és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        UPDATE pacients_diagnostics 
                        SET vigent = 'N',
                            responsable_no_vigent = @responsable,
                            data_no_vigent = NOW(),
                            dt_update = NOW()
                        WHERE id = @diagnosticId
                          AND dt_delete IS NULL
                          AND vigent = 'S'";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);
                        cmd.Parameters.AddWithValue("@responsable", responsable);

                        int filesAfectades = cmd.ExecuteNonQuery();

                        if (filesAfectades > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Diagnòstic {diagnosticId} marcat com a NO vigent per {responsable}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut marcar diagnòstic {diagnosticId} com a no vigent (ja era no vigent o no existeix)");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marcant diagnòstic {diagnosticId} com a no vigent: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Reactiva un diagnòstic (el torna a marcar com a vigent)
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <param name="responsable">Usuari que reactiva</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        public bool ReactivarDiagnostic(int diagnosticId, string responsable)
        {
            if (diagnosticId <= 0)
            {
                Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ReactivarDiagnostic: diagnosticId invàlid ({diagnosticId})");
                return false;
            }

            if (string.IsNullOrWhiteSpace(responsable))
            {
                Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ReactivarDiagnostic: responsable és null o buit");
                return false;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        UPDATE pacients_diagnostics 
                        SET vigent = 'S',
                            responsable_no_vigent = NULL,
                            data_no_vigent = NULL,
                            dt_update = NOW()
                        WHERE id = @diagnosticId
                          AND dt_delete IS NULL
                          AND vigent = 'N'";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@diagnosticId", diagnosticId);

                        int filesAfectades = cmd.ExecuteNonQuery();

                        if (filesAfectades > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Diagnòstic {diagnosticId} reactivat per {responsable}");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut reactivar diagnòstic {diagnosticId} (ja era vigent o no existeix)");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reactivant diagnòstic {diagnosticId}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Obté els diagnòstics actius (vigents) d'un pacient amb el darrer positiu associat
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient (npat)</param>
        /// <returns>Llista de diagnòstics actius amb informació del darrer positiu</returns>
        public List<DiagnosticActiuPacient> ObtenirDiagnosticsActiusPacient(string pacientSap)
        {
            var diagnostics = new List<DiagnosticActiuPacient>();

            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ObtenirDiagnosticsActiusPacient: pacientSap és null o buit");
                return diagnostics;
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}🔎 Obtenint diagnòstics actius del pacient {pacientSap} amb data del darrer positiu");

                    conn.Open();

                    // Query per obtenir els diagnòstics vigents amb el darrer positiu associat
                    string sql = @"
                        SELECT DISTINCT
                            pd.id AS diagnostic_id,
                            pd.npat,
                            pd.microorganisme,
                            pd.mecanisme,
                            pd.tipus_mecanisme,
                            pd.data_diagnostic,
                            -- Darrer positiu associat
                            MAX(pdm.data_mostra) AS data_darrer_positiu,
                            pdm_darrer.tipus_mostra_m AS tipus_mostra,
                            tm.descripcio AS descripcio_tipus_mostra,
                            -- Camps nota_curs_clinic
                            mec.nota_curs_clinic AS mecanisme_nota_curs_clinic,
                            micro.nota_curs_clinic AS microorganisme_nota_curs_clinic
                        FROM pacients_diagnostics pd
                            -- Relació amb mostres a través de mostra_microorganisme
                            INNER JOIN mostra_microorganisme mm ON pd.id = mm.pacient_diagnostic_id
                            INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
                            -- Obtenir el darrer positiu
                            LEFT JOIN pacients_diagnostics_mostra pdm_darrer ON pdm_darrer.id = (
                                SELECT pdm_sub.id
                                FROM mostra_microorganisme mm_sub
                                    INNER JOIN pacients_diagnostics_mostra pdm_sub ON mm_sub.pacient_diagnostic_mostra_id = pdm_sub.id
                                WHERE mm_sub.pacient_diagnostic_id = pd.id
                                  AND pdm_sub.valoracio = '2'
                                  AND pdm_sub.dt_delete IS NULL
                                ORDER BY pdm_sub.data_mostra DESC
                                LIMIT 1
                            )
                            -- Tipus de mostra
                            LEFT JOIN tipusmostra_m tm ON pdm_darrer.tipus_mostra_m = tm.codi
                            -- Mecanisme
                            LEFT JOIN mecanismes mec ON pd.mecanisme = mec.codi AND mec.dt_delete IS NULL
                            -- Microorganisme
                            LEFT JOIN microorganismes micro ON pd.microorganisme = micro.codi AND micro.dt_delete IS NULL
                        WHERE pd.npat = @pacientSap
                          AND pd.vigent = 'S'
                          AND pd.dt_delete IS NULL
                          AND pdm.valoracio = '2'
                          AND pdm.dt_delete IS NULL
                        GROUP BY pd.id, pd.npat, pd.microorganisme, pd.mecanisme, pd.tipus_mecanisme, 
                                 pd.data_diagnostic, pdm_darrer.tipus_mostra_m, tm.descripcio,
                                 mec.nota_curs_clinic, micro.nota_curs_clinic
                        ORDER BY MAX(pdm.data_mostra) DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@pacientSap", pacientSap);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Lectura dels camps nota_curs_clinic
                                // Amb TINYINT(2), MySQL retorna el valor com a byte (0-255)
                                int? mecanismeNota = null;
                                if (reader["mecanisme_nota_curs_clinic"] != DBNull.Value)
                                {
                                    mecanismeNota = Convert.ToInt32(reader["mecanisme_nota_curs_clinic"]);
                                }

                                int? microorganismeNota = null;
                                if (reader["microorganisme_nota_curs_clinic"] != DBNull.Value)
                                {
                                    microorganismeNota = Convert.ToInt32(reader["microorganisme_nota_curs_clinic"]);
                                }

                                diagnostics.Add(new DiagnosticActiuPacient
                                {
                                    DiagnosticId = reader.GetInt32("diagnostic_id"),
                                    PacientSap = reader["npat"]?.ToString(),
                                    Microorganisme = reader["microorganisme"]?.ToString(),
                                    Mecanisme = reader["mecanisme"] != DBNull.Value ? reader["mecanisme"]?.ToString() : null,
                                    TipusMecanisme = reader["tipus_mecanisme"] != DBNull.Value ? reader["tipus_mecanisme"]?.ToString() : null,
                                    DataDiagnostic = reader["data_diagnostic"] as DateTime?,
                                    DataDarrerPositiu = reader["data_darrer_positiu"] as DateTime?,
                                    TipusMostra = reader["tipus_mostra"]?.ToString(),
                                    DescripcioTipusMostra = reader["descripcio_tipus_mostra"]?.ToString(),
                                    MecanismeNotaCursClinic = mecanismeNota,
                                    MicroorganismeNotaCursClinic = microorganismeNota
                                });
                            }
                        }
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Trobats {diagnostics.Count} diagnòstic(s) actiu(s) per al pacient {pacientSap}");

                    // Mostrar detall de cada diagnòstic
                    if (diagnostics.Count > 0)
                    {
                        foreach (var diag in diagnostics)
                        {
                            string infoMecanisme = !string.IsNullOrWhiteSpace(diag.Mecanisme) 
                                ? $"{diag.Mecanisme} - {diag.TipusMecanisme}" 
                                : "";

                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}  - Id: {diag.DiagnosticId}. Tipus mostra: {diag.TipusMostra}. Data mostra darrer positiu: {diag.DataDarrerPositiu:dd/MM/yyyy}. Microorganisme: {diag.Microorganisme}. Mecanisme: {infoMecanisme}");

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obtenint diagnòstics actius del pacient {pacientSap}: {ex.Message}", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// Confecciona la nota del curs clínic amb la llista de diagnòstics actius del pacient
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <returns>Nota formattejada amb els diagnòstics actius</returns>
        public string ConfeccionarNotaCursClinic(string pacientSap)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ConfeccionarNotaCursClinic: pacientSap és null o buit");
                return string.Empty;
            }

            try
            {
                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Confeccionant nota curs clínic per pacient {pacientSap}");

                // Obtenir els diagnòstics actius del pacient
                var diagnostics = ObtenirDiagnosticsActiusPacient(pacientSap);

                if (diagnostics == null || diagnostics.Count == 0)
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ℹ️ El pacient no té diagnòstics actius");
                    return string.Empty;
                }

                // Filtrar diagnòstics que tenen nota especificada
                // Un diagnòstic requereix nota si:
                // - Té mecanisme de resistència i mecanisme.nota_curs_clinic té valor (1 o 2)
                // - NO té mecanisme de resistència (microorganisme especial) i microorganisme.nota_curs_clinic té valor (1 o 2)
                var diagnosticsAmbNota = diagnostics.Where(d =>
                {
                    // Si té mecanisme, comprovar si el mecanisme té nota
                    if (!string.IsNullOrWhiteSpace(d.Mecanisme))
                    {
                        return d.MecanismeNotaCursClinic.HasValue;
                    }
                    // Si no té mecanisme (microorganisme especial), comprovar si el microorganisme té nota
                    else
                    {
                        return d.MicroorganismeNotaCursClinic.HasValue;
                    }
                }).ToList();

                if (diagnosticsAmbNota.Count == 0)
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ℹ️ El pacient no té diagnòstics amb nota especificada");
                    return string.Empty;
                }

                // Determinar el tipus de nota més restrictiu
                // Si hi ha algun diagnòstic amb nota tipus 1 → text general (més restrictiu)
                // Si TOTS són tipus 2 → text específic (àrees crítiques)
                // NOTA: El camp nota_curs_clinic pot ser NULL, 1 o 2
                
                bool hiHaTipus1 = diagnosticsAmbNota.Any(d =>
                {
                    // Comprovar si el mecanisme té nota de tipus 1
                    if (!string.IsNullOrWhiteSpace(d.Mecanisme) && d.MecanismeNotaCursClinic.HasValue)
                    {
                        return d.MecanismeNotaCursClinic.Value == 1; // Tipus 1 = nota general
                    }
                    // Comprovar si el microorganisme té nota de tipus 1
                    if (d.MicroorganismeNotaCursClinic.HasValue)
                    {
                        return d.MicroorganismeNotaCursClinic.Value == 1; // Tipus 1 = nota general
                    }
                    return false;
                });

                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Numero de diagnòstics amb tipus de nota: {diagnosticsAmbNota.Count}. Hi ha diagnòstics amb tipus 1 (més general): {hiHaTipus1}");

                // Confeccionar la nota

                // Capçalera de la nota
                var nota = new StringBuilder();
                nota.AppendLine("MEDICINA PREVENTIVA - EQUIP DE CONTROL DE LA INFECCIÓ ");
                nota.AppendLine();
                nota.AppendLine("El Servei de Medicina Preventiva i Salut Pública de l'Hospital Universitari de Girona Dr. Josep Trueta informa que el/s cultiu/s de:");
                nota.AppendLine();

                // Detall de cada diagnòstic amb nota
                foreach (var diagnostic in diagnosticsAmbNota)
                {
                    // Tipus de mostra
                    string tipusMostra = !string.IsNullOrWhiteSpace(diagnostic.DescripcioTipusMostra)
                        ? diagnostic.DescripcioTipusMostra
                        : diagnostic.TipusMostra ?? "N/D";

                    // Microorganisme
                    string microorganisme = diagnostic.Microorganisme ?? "N/D";

                    // Mecanisme de resistència (opcional)
                    string infoMecanisme = !string.IsNullOrWhiteSpace(diagnostic.Mecanisme)
                        ? $"{diagnostic.Mecanisme} - {diagnostic.TipusMecanisme}"
                        : "";

                    // Data del darrer positiu
                    string dataPositiu = diagnostic.DataDarrerPositiu.HasValue
                        ? diagnostic.DataDarrerPositiu.Value.ToString("dd/MM/yyyy")
                        : "N/D";

                    // Afegir línia del diagnòstic
                    nota.AppendLine($"    - {tipusMostra} cursat el dia {dataPositiu} és positiu per {microorganisme} {infoMecanisme} ");
                    nota.AppendLine();
                }

                nota.AppendLine();

                // Recomanació (depenent del tipus de nota més restrictiu)
                if (hiHaTipus1)
                {
                    // Tipus 1: Recomanacions generals (més restrictiu)
                    nota.AppendLine("Recomanem que s'han de seguir les següents precaucions per a reduir el risc de transmissió per contacte:");
                }
                else
                {
                    // Tipus 2: Recomanacions específiques per àrees crítiques
                    nota.AppendLine("Recomanem que s'han de seguir les següents precaucions per a reduir el risc de transmissió per contacte únicament durant l'ingrés en àrea de crítics (UCI/UCO/UCIP, Quiròfan/Intervencionisme, REA/URPA, UCRI, SCP, Unitat d'Ictus, Neonatologia (CIN/CSIN) i Oncohematologia):");
                }

                // Altres recomanacions generals que s'afegeixen sempre
                nota.AppendLine();
                nota.AppendLine("• Realització de la higiene de mans dels professionals amb productes de base alcohòlica ABANS i DESPRÉS de qualsevol contacte amb l’usuari.");
                nota.AppendLine();
                nota.AppendLine("• Utilització de guants si es preveu contacte amb l’usuari. Cal realitzar la higiene de mans ABANS i DESPRÉS del seu ús.");
                nota.AppendLine();
                nota.AppendLine("• Utilització de bata d’un sol ús si es preveu la realització de cures o de contacte pròxim amb l’usuari.  ");
                nota.AppendLine();
                nota.AppendLine("• Desinfecció de les superfícies o equipaments que han estat en contacte amb l’usuari amb productes tipus Surfasafe®.");
                nota.AppendLine();
                nota.AppendLine();
                nota.AppendLine("En cas de dubtes o consultes podeu contactar amb el servei a l'extensió 2394 o al cercapersones 4383. ");

                string notaFinal = nota.ToString().TrimEnd();

                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Nota confeccionada: {diagnosticsAmbNota.Count} diagnòstic(s) amb nota especificada");

                return notaFinal;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error confeccionant nota curs clínic per pacient {pacientSap}: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Afegeix una nota al curs clínic del pacient si s'han creat nous diagnòstics positius
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="sShanAfegitPositius">Indica si s'han afegit positius en el processament</param>
        /// <returns>True si s'ha inserit la nota, false en cas contrari</returns>
        public bool AfegirNotaCursClinicSiCal(string pacientSap, bool sShanAfegitPositius)
        {
            if (!sShanAfegitPositius)
            {
                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ℹ️ No s'han afegit positius. No cal afegir nota al curs clínic");
                return false;
            }

            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("AfegirNotaCursClinicSiCal: pacientSap és null o buit");
                return false;
            }

            try
            {
                // Confeccionar la nota
                string nota = ConfeccionarNotaCursClinic(pacientSap);

                if (string.IsNullOrWhiteSpace(nota))
                {
                    Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ La nota està buida. No s'afegirà al curs clínic");
                    return false;
                }

                // Inserir la nota MMR - passar tipus 'M'
                bool inserit = this.InserirNotaCursClinic(pacientSap, nota, "M");

                if (inserit)
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Nota afegida correctament al curs clínic del pacient {pacientSap}");
                }

                return inserit;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error afegint nota curs clínic per pacient {pacientSap}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Afegeix una nota al curs clínic del pacient per Virus Respiratoris si s'han creat nous diagnòstics positius
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="sShanAfegitPositius">Indica si s'han afegit positius en el processament</param>
        /// <returns>True si s'ha inserit la nota, false en cas contrari</returns>
        public bool AfegirNotaCursClinicVirusRespiratoriSiCal(string pacientSap, bool sShanAfegitPositius)
        {
            if (!sShanAfegitPositius)
            {
                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ℹ️ No s'han afegit positius VR. No cal afegir nota al curs clínic");
                return false;
            }

            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("AfegirNotaCursClinicVirusRespiratoriSiCal: pacientSap és null o buit");
                return false;
            }

            try
            {
                // Confeccionar la nota específica per Virus Respiratoris
                string nota = ConfeccionarNotaCursClinicVirusRespiratori(pacientSap);

                if (string.IsNullOrWhiteSpace(nota))
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ La nota VR està buida. No s'afegirà al curs clínic");
                    return false;
                }

                // Inserir la nota VR - passar tipus 'R'
                bool inserit = this.InserirNotaCursClinic(pacientSap, nota, "R");

                if (inserit)
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Nota VR afegida correctament al curs clínic del pacient {pacientSap}");
                }

                return inserit;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error afegint nota VR curs clínic per pacient {pacientSap}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Confecciona la nota del curs clínic per Virus Respiratoris amb la llista de diagnòstics actius del pacient
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <returns>Nota formattejada amb els diagnòstics actius de Virus Respiratoris</returns>
        public string ConfeccionarNotaCursClinicVirusRespiratori(string pacientSap)
        {
            if (string.IsNullOrWhiteSpace(pacientSap))
            {
                Logger.Warning("ConfeccionarNotaCursClinicVirusRespiratori: pacientSap és null o buit");
                return string.Empty;
            }

            try
            {
                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Confeccionant nota curs clínic per Virus Respiratoris - pacient {pacientSap}");

                // Obtenir els diagnòstics actius de Virus Respiratoris del pacient
                var diagnostics = ObtenirDiagnosticsActiusPacient(pacientSap);

                if (diagnostics == null || diagnostics.Count == 0)
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ℹ️ El pacient no té diagnòstics actius VR");
                    return string.Empty;
                }

                // Filtrar diagnòstics que tenen nota especificada
                // Un diagnòstic VR requereix nota si:
                // - NO té mecanisme de resistència (els VR no en tenen)
                // - El microorganisme té nota_curs_clinic amb valor (1 o 2)
                var diagnosticsAmbNota = diagnostics.Where(d =>
                {
                    // Els VR no tenen mecanisme, només comprovar el microorganisme
                    if (string.IsNullOrWhiteSpace(d.Mecanisme))
                    {
                        return d.MicroorganismeNotaCursClinic.HasValue;
                    }
                    return false;
                }).ToList();

                if (diagnosticsAmbNota.Count == 0)
                {
                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}ℹ️ El pacient no té diagnòstics VR amb tipus de nota de curs clìnic especificada");
                    return string.Empty;
                }

                // Determinar el tipus de nota més restrictiu
                // Si hi ha algun diagnòstic amb nota tipus 1 → text general (més restrictiu)
                // Si TOTS són tipus 2 → text específic (àrees crítiques)
                
                bool hiHaTipus1 = diagnosticsAmbNota.Any(d =>
                {
                    // Comprovar si el microorganisme té nota de tipus 1
                    if (d.MicroorganismeNotaCursClinic.HasValue)
                    {
                        return d.MicroorganismeNotaCursClinic.Value == 1; // Tipus 1 = nota general
                    }
                    return false;
                });

                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Numero de diagnòstics VR amb tipus de nota: {diagnosticsAmbNota.Count}. Hi ha diagnòstics amb tipus 1 (més general): {hiHaTipus1}");

                // Confeccionar la nota

                // Capçalera de la nota
                var nota = new StringBuilder();
                nota.AppendLine("MEDICINA PREVENTIVA - EQUIP DE CONTROL DE LA INFECCIÓ ");
                nota.AppendLine();
                nota.AppendLine("El Servei de Medicina Preventiva i Salut Pública de l'Hospital Universitari de Girona Dr. Josep Trueta informa que el/s cultiu/s de:");
                nota.AppendLine();

                // Detall de cada diagnòstic VR amb nota
                foreach (var diagnostic in diagnosticsAmbNota)
                {
                    // Tipus de mostra
                    string tipusMostra = !string.IsNullOrWhiteSpace(diagnostic.DescripcioTipusMostra)
                        ? diagnostic.DescripcioTipusMostra
                        : diagnostic.TipusMostra ?? "N/D";

                    // Microorganisme (Virus Respiratori)
                    string microorganisme = diagnostic.Microorganisme ?? "N/D";

                    // Data del darrer positiu
                    string dataPositiu = diagnostic.DataDarrerPositiu.HasValue
                        ? diagnostic.DataDarrerPositiu.Value.ToString("dd/MM/yyyy")
                        : "N/D";

                    // Afegir línia del diagnòstic VR (sense mecanisme)
                    nota.AppendLine($"    - {tipusMostra} cursat el dia {dataPositiu} és positiu per {microorganisme}");
                    nota.AppendLine();
                }

                nota.AppendLine();

                // Recomanació (depenent del tipus de nota més restrictiu)
                if (hiHaTipus1)
                {
                    // Tipus 1: Recomanacions generals (més restrictiu)
                    nota.AppendLine("Recomanem que s'han de seguir les següents precaucions per a reduir el risc de transmissió per contacte:");
                }
                else
                {
                    // Tipus 2: Recomanacions específiques per àrees crítiques
                    nota.AppendLine("Recomanem que s'han de seguir les següents precaucions per a reduir el risc de transmissió per contacte únicament durant l'ingrés en àrea de crítics (UCI/UCO/UCIP, Quiròfan/Intervencionisme, REA/URPA, UCRI, SCP, Unitat d'Ictus, Neonatologia (CIN/CSIN) i Oncohematologia):");
                }

                // Altres recomanacions generals que s'afegeixen sempre
                nota.AppendLine();
                nota.AppendLine("• Realització de la higiene de mans dels professionals amb productes de base alcohòlica ABANS i DESPRÉS de qualsevol contacte amb l'usuari.");
                nota.AppendLine();
                nota.AppendLine("• Utilització de guants si es preveu contacte amb l'usuari. Cal realitzar la higiene de mans ABANS i DESPRÉS del seu ús.");
                nota.AppendLine();
                nota.AppendLine("• Utilització de bata d'un sol ús si es preveu la realització de cures o de contacte pròxim amb l'usuari.  ");
                nota.AppendLine();
                nota.AppendLine("• Desinfecció de les superfícies o equipaments que han estat en contacte amb l'usuari amb productes tipus Surfasafe®.");
                nota.AppendLine();
                nota.AppendLine();
                nota.AppendLine("En cas de dubtes o consultes podeu contactar amb el servei a l'extensió 2394 o al cercapersones 4383. ");

                string notaFinal = nota.ToString().TrimEnd();

                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Nota VR confeccionada: {diagnosticsAmbNota.Count} diagnòstic(s) amb nota especificada");

                return notaFinal;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error confeccionant nota VR curs clínic per pacient {pacientSap}: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Insereix una nota al curs clínic d'un pacient
        /// </summary>
        /// <param name="npat">Identificador del pacient</param>
        /// <param name="nota">Contingut de la nota</param>
        /// <param name="tipus">Tipus de nota: 'M' = Multiresistent, 'R' = Respiratori (per defecte 'M')</param>
        /// <returns>True si s'ha inserit correctament</returns>
        public bool InserirNotaCursClinic(string npat, string nota, string tipus = "M")
        {
            if (string.IsNullOrWhiteSpace(npat))
            {
                Logger.Warning("InserirNotaCursClinic: npat és null o buit");
                return false;
            }

            if (string.IsNullOrWhiteSpace(nota))
            {
                Logger.Warning("InserirNotaCursClinic: nota és null o buida");
                return false;
            }

            // Validar tipus
            if (tipus != "M" && tipus != "R")
            {
                Logger.Warning($"InserirNotaCursClinic: tipus '{tipus}' no vàlid. S'utilitzarà 'M' per defecte");
                tipus = "M";
            }

            try
            {
                string tipusText = tipus == "M" ? "MMR" : "VR";
                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📝 Inserint nota curs clínic ({tipusText}) per pacient {npat}");

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        INSERT INTO notes_curs_clinic 
                            (npat, nota, dt_create, dt_update, enviada, tipus) 
                        VALUES 
                            (@npat, @nota, NOW(), NOW(), 0, @tipus)";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@npat", npat);
                        cmd.Parameters.AddWithValue("@nota", nota);
                        cmd.Parameters.AddWithValue("@tipus", tipus);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Nota curs clínic ({tipusText}) inserida correctament ({rowsAffected} registre)");
                            return true;
                        }
                        else
                        {
                            Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha inserit cap nota curs clínic");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error inserint nota curs clínic per pacient {npat}: {ex.Message}", ex);
                return false;
            }
        }

        #endregion
    }
}
