using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using Oracle.ManagedDataAccess.Client;
using System;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Extensions per ModulabDbService per gestionar càrregues amb sincronització optimitzada
    /// </summary>
    public partial class ModulabDbService
    {
        /// <summary>
        /// Carrega resultats de mostres utilitzant filtres de sincronització optimitzats
        /// Implementa:
        /// 1. Filtre per data_resultat >= última processada (amb overlap de 2 min per seguretat)
        /// 2. Filtre per data_validacio >= última processada (amb overlap de 2 min per seguretat)

        /// </summary>
        /// <param name="dadesSincronitzacio">Dades de l'última sincronització (null si és la primera)</param>
        /// <param name="mysqlService">Servei MySQL per consultar microorganismes especials</param>
        /// <param name="limitRegistres">Límit màxim de registres (0 = sense límit)</param>
        /// <returns>Col·lecció de mostres carregades</returns>
        public ColeccioMostres CarregarResultatsAmbSincronitzacio(
            DadesSincronitzacio dadesSincronitzacio, 
            MultiRDbService mysqlService = null, 
            int limitRegistres = 0)
        {

            var coleccioResultats = new ColeccioMostres();
            int registresProcessats = 0;
            int registresAmbError = 0;
            int microorganismesEspecials = 0;

            // Si és la primera execució, carregar els últims 7 dies
            if (dadesSincronitzacio == null)
            {
                _logger.Info("ℹ️ Primera execució - carregant mostres dels últims 7 dies");
                return CarregarResultatsDeMostres(7, mysqlService, limitRegistres);
            }

            // Calcular dates amb overlap de seguretat
            DateTime? dataResultatFiltre = dadesSincronitzacio.DataResultatMaxProcessada?.AddMinutes(-2);
            DateTime? dataValidacioFiltre = dadesSincronitzacio.DataValidacioMaxProcessada?.AddMinutes(-2);
            int diesRevisio = dadesSincronitzacio.DiesRevisioSeguretat;

            _logger.Info("📊 Filtres aplicats:");
            if (dataResultatFiltre.HasValue)
            {
                _logger.Info($"Data resultat > {dataResultatFiltre:dd/MM/yyyy HH:mm} (amb overlap de 2 min)");
            }
            if (dataValidacioFiltre.HasValue)
            {
                _logger.Info($"Data validació > {dataValidacioFiltre:dd/MM/yyyy HH:mm} (amb overlap de 2 min)");
            }
            
            // Precarregar microorganismes especials
            if (mysqlService != null)
            {
                try
                {
                    _logger.Info("📋 Precarregant microorganismes especials...");
                    mysqlService.CarregarMicroorganismesEspecials();
                    string estadistiques = mysqlService.ObtenirEstadistiquesCache();
                    _logger.Info(estadistiques);
                }
                catch (Exception ex)
                {
                    _logger.Error("⚠️ Error precarregant microorganismes especials", ex);
                    Console.WriteLine($"⚠️ Error precarregant microorganismes: {ex.Message}");
                }
            }

            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();

                string sql = ObtenirConsultaAmbFiltresSincronitzacio(
                    dataResultatFiltre,
                    dataValidacioFiltre,
                    diesRevisio,
                    limitRegistres);

                using (var cmd = new OracleCommand(sql, conn))
                {
                    _logger.Info($"🔎 Executant consulta a Modulab amb filtres per dates de resultat i validació processades");

                    using (var reader = cmd.ExecuteReader())
                    {
                        _logger.Info("✅ Consulta executada. Processant registres...");

                        while (reader.Read())
                        {
                            try
                            {
                                registresProcessats++;

                                if (limitRegistres > 0 && registresProcessats > limitRegistres)
                                {
                                    _logger.Info($"⏹ Límit de {limitRegistres} registres assolit");
                                    break;
                                }

                                var registre = CrearRegistreDesDeReader(reader, mysqlService);

                                if (registre.EsMicroorganismeEspecial == true)
                                {
                                    microorganismesEspecials++;
                                }

                                // Utilitzar el mètode ValidarRegistre existent de la classe base
                                var errors = new System.Collections.Generic.List<string>();
                                if (string.IsNullOrWhiteSpace(registre.EtiquetaId))
                                    errors.Add("ETIQUETA_ID buida");
                                if (string.IsNullOrWhiteSpace(registre.PacientSap))
                                    errors.Add("PACIENT_SAP buit");
                                if (registre.DataResultat == default(DateTime))
                                    errors.Add("DATA_RESULTAT invàlida");

                                if (errors.Count == 0)
                                {
                                    coleccioResultats.AfegirResultat(registre);
                                }
                                else
                                {
                                    registresAmbError++;
                                    _logger.Warning($"⚠️ Registre #{registresProcessats} omès: {string.Join("; ", errors)}");
                                }
                            }
                            catch (Exception ex)
                            {
                                registresAmbError++;
                                _logger.Error($"❌ Error processant registre #{registresProcessats}", ex);

                                if (registresAmbError > 10)
                                {
                                    _logger.Error($"🛑 Massa errors ({registresAmbError}). Aturant processament");
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Resum
            string resum = $@"
📊 RESUM CÀRREGA AMB SINCRONITZACIÓ:
   - Resultats processats: {registresProcessats}
   - Resultats carregats: {coleccioResultats.NombreTotalResultats}
   - Errors: {registresAmbError}
   - Microorganismes especials: {microorganismesEspecials}";

            if (limitRegistres > 0)
            {
                resum += $"\n   - Límit aplicat: {limitRegistres} registres";
            }

            if (registresAmbError > 0)
            {
                double percentatgeError = (registresAmbError * 100.0 / registresProcessats);
                resum += $"\n   - % Error: {percentatgeError:F2}%";
            }

            _logger.Info(resum);
            Console.WriteLine(resum);

            return coleccioResultats;
        }

        /// <summary>
        /// Construeix la consulta SQL amb filtres de sincronització
        /// Implementa la lògica de 2 filtres complementaris:
        /// 1. DATA_RESULTAT >= última processada (amb overlap de 2 min)
        /// 2. DATA_VALIDACIO >= última processada (amb overlap de 2 min)
        /// IMPORTANT: Concatena PREFIX formatat a 3 caràcters amb ETIQUETA_ID per obtenir identificador únic real
        /// </summary>
        private string ObtenirConsultaAmbFiltresSincronitzacio(
            DateTime? dataResultatFiltre,
            DateTime? dataValidacioFiltre,
            int diesRevisio,
            int limitRegistres)
        {
            string consultaBase = @"
                SELECT
                  PET.ETIQUETA_ID || LPAD(NVL(CONT.PREFIX, '0'), 3, '0') AS ETIQUETA_ID,
                  PA.PACIENT_SAP,
                  nvl(PA.CIP,'N/D') CIP,
                  ME.COLEGIAT_ID,
                  REPLACE (ME.NOM_METGE,'''','´') AS NOM_METGE,
                  REPLACE (LTRIM(C.CENTRE_DESCRIPCIO),'''','´') AS CENTRE_DESCRIPCIO,
                  PET.DATA_PETICIO_TRUNC,
                  REPLACE (A.AILLAMENT_DESCRIPCIO,'''','´') AS AILLAMENT_DESCRIPCIO,
                  DETALL.MECANISME_RESISTENCIA1_ID,
                  REPLACE (MR.MECANISME_RESISTENCIA_DESCRIP,'''','´') AS MECANISME_RESISTENCIA_DESCRIP,
                  DETALL.MECANISME_RESISTENCIA2_ID,
                  REPLACE (MR2.MECANISME_RESISTENCIA_DESCRIP,'''','´') AS MECANISME_RESISTENCIA_DESCRIP2,
                  DETALL.MECANISME_RESISTENCIA3_ID,
                  REPLACE (MR3.MECANISME_RESISTENCIA_DESCRIP,'''','´') AS MECANISME_RESISTENCIA_DESCRIP3,
                  DETALL.MECANISME_RESISTENCIA4_ID,
                  REPLACE (MR4.MECANISME_RESISTENCIA_DESCRIP,'''','´') AS MECANISME_RESISTENCIA_DESCRIP4,
                  DETALL.MECANISME_RESISTENCIA5_ID,
                  REPLACE (MR5.MECANISME_RESISTENCIA_DESCRIP,'''','´') AS MECANISME_RESISTENCIA_DESCRIP5,
                  REPLACE (S.SERVEI_DESCRIPCIO,'''','´') AS SERVEI_DESCRIPCIO,
                  REPLACE (PR.PROVA_DESCRIPCIO,'''','´') AS PROVA_DESCRIPCIO,
                  REPLACE (MOS.MOSTRA_DESCRIPCIO,'''','´') AS MOSTRA_DESCRIPCIO,
                  DETALL.DATA_RESULTAT,
                  DETALL.DATA_VALIDACIO 
                FROM
                  DWDIMICS.DIM_LAB_MEC_RESISTENCIA MR,
                  DWDIMICS.DIM_LAB_MEC_RESISTENCIA MR2,
                  DWDIMICS.DIM_LAB_MEC_RESISTENCIA MR3,
                  DWDIMICS.DIM_LAB_MEC_RESISTENCIA MR4,
                  DWDIMICS.DIM_LAB_MEC_RESISTENCIA MR5,
                  DWDIMICS.DIM_LAB_CENTRE C,
                  DWDIMICS.DIM_LAB_SERVEI S,
                  DWDIMICS.DIM_LAB_AILLAMENT A,
                  DWDIMICS.DIM_LAB_PROVA PR,
                  DWDIMICS.DIM_LAB_METGE ME,
                  DWDIMICS.DIM_LAB_PACIENTS_DT PA,
                  DWDIMICS.DIM_LAB_PETICIONS_DT PET,
                  DWDIMICS.V_DIM_LAB_CONTENIDOR_DT CONT,
                  DWDIMICS.V_DIM_LAB_MOSTRA_DT MOS,
                  DWFACTICS.FAC_LAB_PROVES_DT DETALL
                WHERE
                  ( PET.PACIENT_ID = PA.PACIENT_ID(+) AND  PET.ORIGEN = PA.ORIGEN(+)  )
                  AND  ( PET.METGE_ID = ME.METGE_ID AND  PET.ORIGEN = ME.ORIGEN  )
                  AND  ( PET.ORIGEN = S.ORIGEN(+) AND  PET.SERVEI_ID = S.SERVEI_ID(+)  )
                  AND  ( PET.ORIGEN = DETALL.ORIGEN AND  PET.PETICIO_ID = DETALL.PETICIO_ID  )
                  AND  ( DETALL.ORIGEN = PR.ORIGEN(+) AND  DETALL.PROVA_ID = PR.PROVA_ID(+)  )
                  AND  ( A.ORIGEN(+) = DETALL.ORIGEN AND  A.AILLAMENT_ID(+)=DETALL.AILLAMENT_ID  )
                  AND  ( S.ORIGEN = C.ORIGEN(+) AND  S.CENTRE_ID = C.CENTRE_ID(+)  )
                  AND  ( MR.ORIGEN(+) = DETALL.ORIGEN AND  MR.MECANISME_RESISTENCIA_CODI(+) = DETALL.MECANISME_RESISTENCIA1_ID  )
                  AND  ( MR2.ORIGEN(+) = DETALL.ORIGEN AND  MR2.MECANISME_RESISTENCIA_CODI(+) = DETALL.MECANISME_RESISTENCIA2_ID  )
                  AND  ( MR3.ORIGEN(+) = DETALL.ORIGEN AND  MR3.MECANISME_RESISTENCIA_CODI(+) = DETALL.MECANISME_RESISTENCIA3_ID  )
                  AND  ( MR4.ORIGEN(+) = DETALL.ORIGEN AND  MR4.MECANISME_RESISTENCIA_CODI(+) = DETALL.MECANISME_RESISTENCIA4_ID  )
                  AND  ( MR5.ORIGEN(+) = DETALL.ORIGEN AND  MR5.MECANISME_RESISTENCIA_CODI(+) = DETALL.MECANISME_RESISTENCIA5_ID  )
                  AND  ( CONT.ORIGEN(+) = DETALL.ORIGEN AND  CONT.CONTENIDOR_ID(+) = DETALL.CONTENIDOR_ID )
                  AND  ( MOS.ORIGEN(+) = CONT.ORIGEN AND  MOS.MOSTRA_ID(+) = CONT.MOSTRA_ID )
                  AND  ( PA.TIPUS is null )
                  AND  PET.ORIGEN  =  'DT'
                  AND  DETALL.TIPUS = 'A'
                  AND  (";

            // Construir la clàusula de filtres de dates amb TO_DATE
            var filtres = new System.Collections.Generic.List<string>();

            // Filtre 1: DATA_RESULTAT >= última processada
            if (dataResultatFiltre.HasValue)
            {
                string dataFormatejada = dataResultatFiltre.Value.ToString("yyyy-MM-dd HH:mm:ss");
                filtres.Add($"DETALL.DATA_RESULTAT >= TO_DATE('{dataFormatejada}', 'YYYY-MM-DD HH24:MI:SS')");
            }

            // Filtre 2: DATA_VALIDACIO >= última processada
            if (dataValidacioFiltre.HasValue)
            {
                string dataFormatejada = dataValidacioFiltre.Value.ToString("yyyy-MM-dd HH:mm:ss");
                filtres.Add($"DETALL.DATA_VALIDACIO >= TO_DATE('{dataFormatejada}', 'YYYY-MM-DD HH24:MI:SS')");
            }

            // Si no hi ha cap filtre, retornar una consulta que no retorni res
            if (filtres.Count == 0)
            {
                filtres.Add("1=0"); // Condició sempre falsa
            }

            // Unir filtres amb OR
            consultaBase += string.Join(" OR ", filtres);
            consultaBase += @"
                  )
                ORDER BY ETIQUETA_ID";

            // Afegir límit si cal
            if (limitRegistres > 0)
            {
                return $@"
                    SELECT * FROM (
                        {consultaBase}
                    ) WHERE ROWNUM <= {limitRegistres}";
            }

            return consultaBase;
        }
    }
}
