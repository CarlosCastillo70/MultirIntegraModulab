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
            else if (dataResultatFiltre.HasValue)
            {
                _logger.Info("Data validació = NULL (capturant resultats sense validar)");
            }
            if (dataResultatFiltre.HasValue || dataValidacioFiltre.HasValue)
            {
                _logger.Info("➕ Filtre addicional: FVDATE IS NULL + REQUESTDATE últim dia (equivalent a 'dies enrere 1 dia' per mostres sense validar)");
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
                SELECT DISTINCT /*+ USE_CONCAT INDEX(rc PK_REQUESTCONTAINER) INDEX(rt PK_REQUESTTEST) */
                    r.REQUESTLABEL || LPAD(NVL(con2.PREFIX, '0'), 3, '0') AS ETIQUETA_ID,
                    con2.PREFIX AS PREFIX,
                    p.EXTERNALID AS PACIENT_SAP,
                    p.NTS AS CIP,
                    d.COLLEGIATEID AS COLEGIAT_ID,
                    REPLACE(d.DOCTORNAME, '''', '´') AS NOM_METGE,
                    REPLACE(LTRIM(cent.CENTERDESCRIPTION), '''', '´') AS CENTRE_DESCRIPCIO,
                    r.requestdate AS DATA_PETICIO_TRUNC,
                    REPLACE(i.isolationdescription, '''', '´') AS AILLAMENT_DESCRIPCIO,
                    ci.resistancemechanismcode1 AS MECANISME_RESISTENCIA1_ID,
                    REPLACE(rm1.RESISTANCEMECHANISMDESCRIPTION, '''', '´') AS MECANISME_RESISTENCIA_DESCRIP,
                    ci.resistancemechanismcode2 AS MECANISME_RESISTENCIA2_ID,
                    REPLACE(rm2.RESISTANCEMECHANISMDESCRIPTION, '''', '´') AS MECANISME_RESISTENCIA_DESCRIP2,
                    ci.resistancemechanismcode3 AS MECANISME_RESISTENCIA3_ID,
                    REPLACE(rm3.RESISTANCEMECHANISMDESCRIPTION, '''', '´') AS MECANISME_RESISTENCIA_DESCRIP3,
                    ci.resistancemechanismcode4 AS MECANISME_RESISTENCIA4_ID,
                    REPLACE(rm4.RESISTANCEMECHANISMDESCRIPTION, '''', '´') AS MECANISME_RESISTENCIA_DESCRIP4,
                    ci.resistancemechanismcode5 AS MECANISME_RESISTENCIA5_ID,
                    REPLACE(rm5.RESISTANCEMECHANISMDESCRIPTION, '''', '´') AS MECANISME_RESISTENCIA_DESCRIP5,
                    UPPER(REPLACE(ser.SERVICEDESCRIPTION, '''', '´')) AS SERVEI_DESCRIPCIO,
                    REPLACE(test2.testdescription, '''', '´') AS PROVA_DESCRIPCIO,
                    REPLACE(sam2.sampledescription, '''', '´') AS MOSTRA_DESCRIPCIO,
                    rt.RESULTDATE AS DATA_RESULTAT,
                    rt.FVDATE AS DATA_VALIDACIO,
                    rcvf.SHORTDESCRIPTION AS SHORTDESCRIPTION1
                FROM
                    MG.CULTUREISOLATION ci
                    LEFT JOIN MG.REQUEST r ON r.REQUESTID = ci.REQUESTID
                    LEFT JOIN MG.PATIENT p ON p.PATIENTID = r.PATIENTID
                    LEFT JOIN MG.ISOLATION i ON i.ISOLATIONID = ci.ISOLATIONID
                    LEFT JOIN MG.RESISTANCEMECHANISM rm1 ON rm1.RESISTANCEMECHANISMCODE = ci.RESISTANCEMECHANISMCODE1
                    LEFT JOIN MG.RESISTANCEMECHANISM rm2 ON rm2.RESISTANCEMECHANISMCODE = ci.RESISTANCEMECHANISMCODE2
                    LEFT JOIN MG.RESISTANCEMECHANISM rm3 ON rm3.RESISTANCEMECHANISMCODE = ci.RESISTANCEMECHANISMCODE3
                    LEFT JOIN MG.RESISTANCEMECHANISM rm4 ON rm4.RESISTANCEMECHANISMCODE = ci.RESISTANCEMECHANISMCODE4
                    LEFT JOIN MG.RESISTANCEMECHANISM rm5 ON rm5.RESISTANCEMECHANISMCODE = ci.RESISTANCEMECHANISMCODE5
                    LEFT JOIN MG.DOCTOR d ON d.DOCTORID = r.DOCTORID
                    LEFT JOIN MG.SERVICE ser ON ser.serviceid = r.serviceid
                    LEFT JOIN MG.SAMPLECOLLECTIONCENTER scol ON scol.samplecollectioncenterid = r.samplecollectioncenterid
                    LEFT JOIN MG.REQUESTTESTADDITIONALINFO rtai ON rtai.REQUESTID = ci.REQUESTID
                        AND rtai.CONTAINERID = ci.CONTAINERID
                        AND rtai.TESTID = ci.TESTID
                    LEFT JOIN MG.CONTAINER con ON rtai.CONTAINERID = con.CONTAINERID
                    LEFT JOIN MG.SAMPLE sam ON sam.SAMPLEID = con.SAMPLEID
                    LEFT JOIN MG.ADDITIONALINFO ai ON ai.ADDITIONALINFOID = rtai.ADDITIONALINFOID
                    LEFT JOIN MG.REQUESTDIAGNOSIS rd ON rd.REQUESTID = r.REQUESTID
                    LEFT JOIN MG.DIAGNOSIS dia ON dia.DIAGNOSISID = rd.DIAGNOSISID
                    LEFT JOIN MG.REQUESTTEST rt ON rt.REQUESTID = ci.REQUESTID
                        AND rt.CONTAINERID = ci.CONTAINERID
                        AND rt.TESTID = ci.TESTID
                    LEFT JOIN MG.REQUESTCONTAINER rc ON rc.REQUESTID = rt.REQUESTID
                        AND rc.CONTAINERID = rt.CONTAINERID
                    LEFT JOIN MG.CONTAINER con2 ON con2.CONTAINERID = rc.CONTAINERID
                    LEFT JOIN MG.SAMPLE sam2 ON sam2.SAMPLEID = con2.SAMPLEID
                    LEFT JOIN MG.CENTER cent ON cent.CENTERID = ser.CENTERID
                    LEFT JOIN MG.RESULTCODEDVALUEFINAL rcvf ON rt.REQUESTID = rcvf.REQUESTID
                        AND rt.CONTAINERID = rcvf.CONTAINERID
                        AND rt.TESTID = rcvf.TESTID
                    LEFT JOIN MG.TEST test2 ON rt.TESTID = test2.TESTID
                WHERE
                  r.REQUESTDATE >= TRUNC(SYSDATE) - 15
                  AND (";

            // Construir la clàusula de filtres de dates amb TO_TIMESTAMP
            var filtres = new System.Collections.Generic.List<string>();

            // Filtre 1: RESULTDATE >= última processada
            if (dataResultatFiltre.HasValue)
            {
                string dataFormatejada = dataResultatFiltre.Value.ToString("yyyy-MM-dd HH:mm:ss");
                filtres.Add($"rt.RESULTDATE >= TO_TIMESTAMP('{dataFormatejada}', 'YYYY-MM-DD HH24:MI:SS')");
            }

            // Filtre 2: FVDATE >= última processada
            if (dataValidacioFiltre.HasValue)
            {
                string dataFormatejada = dataValidacioFiltre.Value.ToString("yyyy-MM-dd HH:mm:ss");
                filtres.Add($"rt.FVDATE >= TO_TIMESTAMP('{dataFormatejada}', 'YYYY-MM-DD HH24:MI:SS')");
            }

            // Filtre 3: FVDATE IS NULL de l'últim dia (resultats sense validar)
            // Cobreix commits tardans a Oracle: la fila pot existir amb RESULTDATE/FVDATE NULL
            // en el moment de la consulta i ser visible al proper cicle.
            // Equivalent a fer una càrrega "dies enrere (1 dia)" però només per mostres sense validar,
            // evitant reprocessar les ja validades. Limitat a 1 dia per rendiment.
            if (dataResultatFiltre.HasValue || dataValidacioFiltre.HasValue)
            {
                filtres.Add("(rt.FVDATE IS NULL AND r.REQUESTDATE >= TRUNC(SYSDATE) - 1)");
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
                ORDER BY PACIENT_SAP, ETIQUETA_ID";

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
