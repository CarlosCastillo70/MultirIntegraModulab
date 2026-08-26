using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Mètodes relacionats amb seguiments de pacients i targetes
    /// </summary>
    public partial class MultiRDbService
    {
        /// <summary>
        /// Actualitza la quantitat de targetes necessàries en seguiments oberts quan es detecta una mostra positiva.
        /// Recalcula automàticament el nombre de targetes per assolir l'objectiu de descolonització (3 mostres negatives consecutives).
        /// </summary>
        /// <param name="npat">Número de pacient</param>
        /// <param name="tipusMostra">Tipus de mostra afectada (ex: "Aspirat traqueal", "Frotis rectal")</param>
        /// <returns>True si s'ha actualitzat almenys un seguiment, False en cas contrari</returns>
        public bool ActualitzarQuantitatTargetes(string npat, string tipusMostra)
        {
            if (string.IsNullOrWhiteSpace(npat) || string.IsNullOrWhiteSpace(tipusMostra))
            {
                Logger.Warning($"ActualitzarQuantitatTargetes: Paràmetres invàlids (npat: {npat}, tipusMostra: {tipusMostra})");
                return false;
            }

            try
            {
                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🎯 Actualitzant possibles targetes de seguiment per pacient {npat}, tipus mostra '{tipusMostra}'");

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // PASO 2: Obtenir seguiments oberts amb aquest tipus de mostra
                    string sqlSeguiments = @"
                        SELECT ps.id as seguiment_id, 
                               ps.data_inici_seguiment, 
                               psm.id as mostra_seguiment_id, 
                               psm.quantitat
                        FROM pacients_seguiments ps
                        INNER JOIN pacients_seguiments_mostres psm ON ps.id = psm.seguiment_id
                        WHERE ps.npat = @npat
                          AND ps.estat = 'O'
                          AND TRIM(psm.tipus_mostra) = TRIM(@tipusMostra)";

                    var seguiments = new List<(int seguimentId, DateTime dataIniciSeguiment, int mostraSeguimentId, int quantitatActual)>();

                    using (var cmd = new MySqlCommand(sqlSeguiments, conn))
                    {
                        cmd.Parameters.AddWithValue("@npat", npat.Trim());
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra.Trim());

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                seguiments.Add((
                                    Convert.ToInt32(reader["seguiment_id"]),
                                    Convert.ToDateTime(reader["data_inici_seguiment"]),
                                    Convert.ToInt32(reader["mostra_seguiment_id"]),
                                    Convert.ToInt32(reader["quantitat"])
                                ));
                            }
                        }
                    }

                    if (seguiments.Count == 0)
                    {
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}ℹ️ No hi ha seguiments oberts per aquest pacient i tipus de mostra '{tipusMostra}'");
                        return false;
                    }

                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}📋 Trobats {seguiments.Count} seguiment(s) obert(s)");

                    bool algunaActualitzacio = false;

                    // Per cada seguiment obert
                    foreach (var (seguimentId, dataIniciSeguiment, mostraSeguimentId, quantitatActual) in seguiments)
                    {
                        Logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}   Processant seguiment ID {seguimentId} (data inici: {dataIniciSeguiment:dd/MM/yyyy})");

                        // PASO 3: Obtenir mostres del seguiment (filtrades per data d'inici)
                        string sqlMostres = @"
                            SELECT id, data_mostra, valoracio
                            FROM pacients_diagnostics_mostra
                            WHERE npat = @npat
                              AND TRIM(tipus_mostra_m) = TRIM(@tipusMostra)
                              AND data_mostra >= @dataIniciSeguiment
                              AND dt_delete IS NULL
                              AND valoracio IS NOT NULL
                            ORDER BY data_mostra ASC, id ASC";

                        var mostres = new List<(int id, DateTime dataMostra, string valoracio)>();

                        using (var cmd = new MySqlCommand(sqlMostres, conn))
                        {
                            cmd.Parameters.AddWithValue("@npat", npat.Trim());
                            cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra.Trim());
                            cmd.Parameters.AddWithValue("@dataIniciSeguiment", dataIniciSeguiment);

                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    mostres.Add((
                                        Convert.ToInt32(reader["id"]),
                                        Convert.ToDateTime(reader["data_mostra"]),
                                        reader["valoracio"]?.ToString() ?? ""
                                    ));
                                }
                            }
                        }

                        int totalMostres = mostres.Count;

                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}📋 Trobades {totalMostres} mostra(es) amb tipus mostra '{tipusMostra}' des de l'inici del seguiment");

                        if (totalMostres == 0)
                        {
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}⚠️ No hi ha mostres per aquest seguiment i tipus mostra '{tipusMostra}'");
                            continue;
                        }

                        // Construir patró visual de les mostres
                        string patroMostres = ConstructPatroMostres(mostres);
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}🔍 Patró actual de mostres: {patroMostres}");

                        // PASO 4: Trobar última mostra positiva
                        int? indexUltimaPositiva = null;
                        const string CODI_POSITIU = "2";

                        for (int i = mostres.Count - 1; i >= 0; i--)
                        {
                            if (mostres[i].valoracio == CODI_POSITIU)
                            {
                                indexUltimaPositiva = i;
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}   🔴 Última mostra positiva trobada a l'índex {i} (Data: {mostres[i].dataMostra:dd/MM/yyyy})");
                                break;
                            }
                        }

                        // PASO 5: Comptar mostres després de l'última positiva
                        int mostresDespres;
                        if (indexUltimaPositiva.HasValue)
                        {
                            mostresDespres = totalMostres - indexUltimaPositiva.Value - 1;
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}Mostres després de l'última positiva: {mostresDespres}");
                        }
                        else
                        {
                            mostresDespres = totalMostres;
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}No hi ha cap mostra positiva. Total mostres: {mostresDespres}");
                        }

                        // PASO 6: Calcular espais lliures necessaris
                        int espaisLliuresNecessaris = Math.Max(0, 3 - mostresDespres);
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}Espais lliures necessaris: {espaisLliuresNecessaris}");

                        // PASO 7: Calcular nova quantitat
                        int novaQuantitat = totalMostres + espaisLliuresNecessaris;

                        // Construir patró visual amb espais lliures
                        string patroAmbEspais = patroMostres + new string('-', espaisLliuresNecessaris);
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}Nova quantitat calculada: {novaQuantitat} (actual: {quantitatActual}) → Patró resultant: {patroAmbEspais}");

                        // PASO 8: Actualitzar només si cal
                        if (novaQuantitat > quantitatActual)
                        {
                            string sqlUpdate = @"
                                UPDATE pacients_seguiments_mostres
                                SET quantitat = @novaQuantitat
                                WHERE id = @mostraSeguimentId";

                            using (var cmd = new MySqlCommand(sqlUpdate, conn))
                            {
                                cmd.Parameters.AddWithValue("@novaQuantitat", novaQuantitat);
                                cmd.Parameters.AddWithValue("@mostraSeguimentId", mostraSeguimentId);

                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✅ Targetes actualitzades: {quantitatActual} → {novaQuantitat} (seguiment ID {seguimentId})");
                                    algunaActualitzacio = true;
                                }
                                else
                                {
                                    Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ No s'ha pogut actualitzar el seguiment ID {seguimentId}");
                                }
                            }
                        }
                        else
                        {
                            Logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}   ℹ️ No cal actualitzar (nova quantitat {novaQuantitat} <= actual {quantitatActual})");
                        }
                    }

                    return algunaActualitzacio;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error actualitzant quantitat de targetes per pacient {npat}, tipus mostra '{tipusMostra}': {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Construeix una representació visual del patró de mostres
        /// </summary>
        /// <param name="mostres">Llista de mostres amb valoració</param>
        /// <returns>Cadena de text amb el patró (ex: "PNNPN")</returns>
        private string ConstructPatroMostres(List<(int id, DateTime dataMostra, string valoracio)> mostres)
{
            if (mostres == null || mostres.Count == 0)
                return "";

            var patroChars = new System.Text.StringBuilder();

            foreach (var mostra in mostres)
            {
                switch (mostra.valoracio)
                {
                    case "1": // Negatiu
                        patroChars.Append("N");
                        break;
                    case "2": // Positiu
                        patroChars.Append("P");
                        break;
                    case "3": // No vàlid
                        patroChars.Append("X");
                        break;
                    case "0": // Pendent
                        patroChars.Append("?");
                        break;
                    default:
                        patroChars.Append("·");
                        break;
                }
            }

            return patroChars.ToString();
        }

        /// <summary>
        /// Actualitza la data de l'última mostra en seguiments oberts quan s'incorpora una mostra (positiva o negativa) de MultiResistent.
        /// Actualitza els camps dt_ultima_mostra a les taules pacients_seguiments i pacients_seguiments_mostres.
        /// </summary>
        /// <param name="npat">Número de pacient</param>
        /// <param name="tipusMostra">Tipus de mostra incorporada</param>
        /// <returns>True si s'ha actualitzat almenys un seguiment, False en cas contrari</returns>
        public bool ActualitzarDataUltimaMostra(string npat, string tipusMostra)
        {
            if (string.IsNullOrWhiteSpace(npat) || string.IsNullOrWhiteSpace(tipusMostra))
            {
                Logger.Warning($"ActualitzarDataUltimaMostra: Paràmetres invàlids (npat: {npat}, tipusMostra: {tipusMostra})");
                return false;
            }

            try
            {
                Logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}📅 Actualitzant data última mostra en seguiments per pacient {npat}, tipus mostra '{tipusMostra}'");

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // Obtenir seguiments oberts amb aquest tipus de mostra
                    string sqlSeguiments = @"
                        SELECT ps.id as seguiment_id, 
                               psm.id as mostra_seguiment_id
                        FROM pacients_seguiments ps
                        INNER JOIN pacients_seguiments_mostres psm ON ps.id = psm.seguiment_id
                        WHERE ps.npat = @npat
                          AND ps.estat = 'O'
                          AND TRIM(psm.tipus_mostra) = TRIM(@tipusMostra)";

                    var seguiments = new List<(int seguimentId, int mostraSeguimentId)>();

                    using (var cmd = new MySqlCommand(sqlSeguiments, conn))
                    {
                        cmd.Parameters.AddWithValue("@npat", npat.Trim());
                        cmd.Parameters.AddWithValue("@tipusMostra", tipusMostra.Trim());

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                seguiments.Add((
                                    Convert.ToInt32(reader["seguiment_id"]),
                                    Convert.ToInt32(reader["mostra_seguiment_id"])
                                ));
                            }
                        }
                    }

                    if (seguiments.Count == 0)
                    {
                        Logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}ℹ️ No hi ha seguiments oberts per actualitzar dt_ultima_mostra");
                        return false;
                    }

                    Logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}📋 Trobats {seguiments.Count} seguiment(s) obert(s) per actualitzar");

                    bool algunaActualitzacio = false;

                    // Per cada seguiment obert
                    foreach (var (seguimentId, mostraSeguimentId) in seguiments)
                    {
                        // Actualitzar pacients_seguiments.dt_ultima_mostra
                        string sqlUpdateSeguiment = @"
                            UPDATE pacients_seguiments
                            SET dt_ultima_mostra = NOW()
                            WHERE id = @seguimentId";

                        using (var cmd = new MySqlCommand(sqlUpdateSeguiment, conn))
                        {
                            cmd.Parameters.AddWithValue("@seguimentId", seguimentId);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                Logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}   ✔️ Actualitzat dt_ultima_mostra a pacients_seguiments (ID {seguimentId})");
                            }
                        }

                        // Actualitzar pacients_seguiments_mostres.dt_ultima_mostra
                        string sqlUpdateMostra = @"
                            UPDATE pacients_seguiments_mostres
                            SET dt_ultima_mostra = NOW()
                            WHERE id = @mostraSeguimentId";

                        using (var cmd = new MySqlCommand(sqlUpdateMostra, conn))
                        {
                            cmd.Parameters.AddWithValue("@mostraSeguimentId", mostraSeguimentId);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                Logger.Debug($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Detall)}   ✔️ Actualitzat dt_ultima_mostra a pacients_seguiments_mostres (ID {mostraSeguimentId})");
                                algunaActualitzacio = true;
                            }
                        }
                    }

                    if (algunaActualitzacio)
                    {
                        Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✅ Data última mostra actualitzada en {seguiments.Count} seguiment(s)");
                    }

                    return algunaActualitzacio;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error actualitzant data última mostra per pacient {npat}, tipus mostra '{tipusMostra}': {ex.Message}", ex);
                return false;
            }
        }
    }
}
