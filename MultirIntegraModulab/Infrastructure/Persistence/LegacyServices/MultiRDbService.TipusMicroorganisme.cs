using System;
using MySql.Data.MySqlClient;
using MultirIntegraModulab.Domain.Enums;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per MultiRDbService - Gestió del Tipus de Microorganisme (MMR vs VR)
    /// </summary>
    public partial class MultiRDbService
    {
        /// <summary>
        /// Obté el tipus de microorganisme (Multiresistent o Virus Respiratori)
        /// basant-se en el camp 'tipus' de la taula microorganismes
        /// </summary>
        /// <param name="microorganismeDescripcio">Descripció del microorganisme</param>
        /// <returns>
        /// TipusMicroorganisme.VirusRespiratori si tipus = 'R'
        /// TipusMicroorganisme.Multiresistent si tipus = 'M' o per defecte
        /// </returns>
        public TipusMicroorganisme ObtenirTipusMicroorganisme(string microorganismeDescripcio)
        {
            if (string.IsNullOrWhiteSpace(microorganismeDescripcio))
            {
                Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Intentant obtenir tipus de microorganisme amb descripció buida");
                return TipusMicroorganisme.Multiresistent; // Per defecte
            }

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Consulta per obtenir el tipus del microorganisme
                    string sql = @"
                        SELECT tipus 
                        FROM microorganismes 
                        WHERE UPPER(descripcio) = UPPER(@microorganisme)
                          AND dt_delete IS NULL 
                          AND actiu = 1
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@microorganisme", microorganismeDescripcio.Trim());

                        var result = cmd.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            string tipus = result.ToString().Trim().ToUpper();
                            
                            if (tipus == "R")
                            {
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Microorganisme '{microorganismeDescripcio}' és VIRUS RESPIRATORI");
                                return TipusMicroorganisme.VirusRespiratori;
                            }
                            else if (tipus == "M")
                            {
                                Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Microorganisme '{microorganismeDescripcio}' és MULTIRESISTENT");
                                return TipusMicroorganisme.Multiresistent;
                            }
                            else
                            {
                                Logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Microorganisme '{microorganismeDescripcio}' amb tipus desconegut '{tipus}' → Assumint MULTIRESISTENT");
                                return TipusMicroorganisme.Multiresistent;
                            }
                        }
                        else
                        {
                            // No s'ha trobat o el camp tipus és null
                            Logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}ℹ️ Microorganisme '{microorganismeDescripcio}' sense tipus definit → Assumint MULTIRESISTENT per defecte");
                            return TipusMicroorganisme.Multiresistent;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Error consultant tipus de microorganisme '{microorganismeDescripcio}'", ex);
                // En cas d'error, retornar Multiresistent per defecte (més conservador)
                return TipusMicroorganisme.Multiresistent;
            }
        }
    }
}
