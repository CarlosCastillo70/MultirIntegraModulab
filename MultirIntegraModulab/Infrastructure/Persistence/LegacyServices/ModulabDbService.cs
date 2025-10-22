using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MultirIntegraModulab
{
    public class ModulabDbService : IDbService
    {
        private readonly string _connectionString;
        private readonly ILoggerService _logger;

        public ModulabDbService(string connectionString, ILoggerService logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public object GetCurrentDate()
        {
            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT SYSDATE FROM dual";
                using (var cmd = new OracleCommand(sql, conn))
                {
                    return cmd.ExecuteScalar();
                }
            }
        }

        public string GetDatabaseType()
        {
            return "Oracle";
        }

        public int GetTableRecordCount(string tableName)
        {
            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                string sql = $"SELECT COUNT(*) FROM {tableName}";
                using (var cmd = new OracleCommand(sql, conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        /// <summary>
        /// Carrega els resultats de mostres de laboratori des de la base de dades Oracle
        /// </summary>
        /// <param name="diesEndarrera">Nombre de dies cap endarrera per carregar (per defecte 1 dia)</param>
        /// <param name="mysqlService">Servei MySQL per consultar microorganismes especials (opcional)</param>
        /// <param name="limitRegistres">Límit màxim de registres a carregar (0 = sense límit, útil per fer proves)</param>
        /// <returns>Col·lecció de resultats de mostres carregada</returns>
        public ColeccioMostres CarregarResultatsDeMostres(int diesEndarrera = 1, MultiRDbService mysqlService = null, int limitRegistres = 0)
        {
            _logger.Info($"Iniciant càrrega de resultats de mostres: dies enrere={diesEndarrera}, límit={limitRegistres}");
            
            var coleccioResultats = new ColeccioMostres();
            int registresProcessats = 0;
            int registresAmbError = 0;
            int microorganismesEspecials = 0;
            
            // Precarregar microorganismes especials si tenim el servei MySQL
            if (mysqlService != null)
            {
                try
                {
                    _logger.Info("📋 Precarregant microorganismes especials des de MySQL...");
                    Console.WriteLine("📋 Precarregant microorganismes especials...");
                    
                    mysqlService.CarregarMicroorganismesEspecials();
                    string estadistiques = mysqlService.ObtenirEstadistiquesCache();
                    
                    _logger.Info($"Microorganismes especials carregats: {estadistiques}");
                    Console.WriteLine(estadistiques);
                }
                catch (Exception ex)
                {
                    _logger.Error("⚠️ Error precarregant microorganismes especials", ex);
                    Console.WriteLine($"⚠️  Error precarregant microorganismes: {ex.Message}");
                    Console.WriteLine("   Continuant sense informació de microorganismes especials");
                }
            }
            
            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                
                string sql = ObtenirConsultaResultatsProves(limitRegistres);
                
                using (var cmd = new OracleCommand(sql, conn))
                {
                    // Afegir paràmetre per als dies
                    cmd.Parameters.Add(new OracleParameter("diesEndarrera", diesEndarrera));

                    if (limitRegistres > 0)
                    {
                        _logger.Info($"🔎 Executant consulta Oracle amb límit de {limitRegistres} registres");
                        Console.WriteLine($"🔎 Recupero les dades de Modulab, de {diesEndarrera} dies enrera, amb límit de {limitRegistres} registres (pot trigar una estona)...");
                    }
                    else
                    {
                        _logger.Info($"🔎 Executant consulta Oracle sense límit de registres ({diesEndarrera} dies enrera)");
                        Console.WriteLine("🔎 Recupero les dades de Modulab (pot trigar una estona)...");
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        _logger.Info("✅ Consulta Oracle executada correctament. Processant registres...");
                        Console.WriteLine("✅ Dades recuperades. Continuo endavant");

                        while (reader.Read())
                        {
                            try
                            {
                                registresProcessats++;
        
                                // Aplicar límit de registres si està especificat
                                if (limitRegistres > 0 && registresProcessats > limitRegistres)
                                {
                                    _logger.Info($"⏹ Límit de {limitRegistres} registres assolit. Aturant la càrrega");
                                    Console.WriteLine($"⏹ Límit de {limitRegistres} registres assolit. Aturant la càrrega.");
                                    break;
                                }
                                
                                var registre = CrearRegistreDesDeReader(reader, mysqlService);
                                
                                // Comptar microorganismes especials
                                if (registre.EsMicroorganismeEspecial == true)
                                {
                                    microorganismesEspecials++;
                                }
                                
                                // Validar dades crítiques abans d'afegir
                                if (ValidarRegistre(registre, registresProcessats))
                                {
                                    coleccioResultats.AfegirResultat(registre);
                                }
                                else
                                {
                                    registresAmbError++;
                                    _logger.Warning($"⚠️ Registre #{registresProcessats} omès per validació fallida - ETIQUETA_ID: {registre.EtiquetaId}");
                                    Console.WriteLine($"⚠️  Registre #{registresProcessats} omès per validació fallida");
                                }
                            }
                            catch (Exception ex)
                            {
                                registresAmbError++;
                                string etiquetaId = ObtenirValorSegur(reader, "ETIQUETA_ID");
                                string pacientSap = ObtenirValorSegur(reader, "PACIENT_SAP");
                                
                                _logger.Error($"❌ Error processant registre #{registresProcessats}: ETIQUETA_ID={etiquetaId}, PACIENT_SAP={pacientSap}", ex);
                                
                                Console.WriteLine($"❌ Error processant registre #{registresProcessats}:");
                                Console.WriteLine($"   - ETIQUETA_ID: {etiquetaId}");
                                Console.WriteLine($"   - PACIENT_SAP: {pacientSap}");
                                Console.WriteLine($"   - Error: {ex.Message}");

                                // Mostrar informació detallada de les dades problemàtiques
                                MostrarDetallsRegistreError(reader, registresProcessats);
                                
                                // Decidir si continuar o aturar el processament
                                if (registresAmbError > 10) // Aturar si hi ha més de 10 errors
                                {
                                    _logger.Error($"🛑 S'han trobat {registresAmbError} errors. Aturant el processament per evitar més problemes");
                                    Console.WriteLine($"🛑  S'han trobat {registresAmbError} errors. Aturant el processament per evitar més problemes.");
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Mostrar i registrar resum del processament
            string resum = $@"
RESUM DE LA INCORPORACIÓ DE LES DADES DE MODULAB:
   - Resultats de mostra processats: {registresProcessats}
   - Resultats de mostra carregats correctament: {coleccioResultats.NombreTotalResultats}
   - Resultats de mostra amb error: {registresAmbError}
   - Microorganismes especials trobats: {microorganismesEspecials}";

            if (limitRegistres > 0)
            {
                resum += $"\n   - Límit aplicat: {limitRegistres} registres (per proves)";
            }
            
            if (registresAmbError > 0)
            {
                double percentatgeError = (registresAmbError * 100.0 / registresProcessats);
                resum += $"\n   - Percentatge d'error: {percentatgeError:F2}%";
            }

            if (mysqlService != null && microorganismesEspecials > 0)
            {
                double percentatgeEspecials = (microorganismesEspecials * 100.0 / coleccioResultats.NombreTotalResultats);
                resum += $"\n   - Percentatge microorganismes especials: {percentatgeEspecials:F2}%";
            }

            _logger.Info($"📊 {resum}");
            Console.WriteLine($"\n📊 {resum}");

            return coleccioResultats;
        }

        /// <summary>
        /// Obte la consulta SQL per carregar els resultats de proves
        /// </summary>
        /// <param name="limitRegistres">Límit màxim de registres (0 = sense límit)</param>
        private string ObtenirConsultaResultatsProves(int limitRegistres = 0)
        {
            string consultaBase = @"
                SELECT
                  PET.ETIQUETA_ID,
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
                  AND  (
                         ( PA.TIPUS is null )
                         AND
                         PET.ORIGEN  =  'DT'
                         AND
                         DETALL.TIPUS = 'A'
                         AND
                         (DETALL.DATA_VALIDACIO_TRUNC >= trunc(sysdate-:diesEndarrera) OR DETALL.DATA_RESULTAT_TRUNC >= trunc(sysdate-:diesEndarrera)) 
                        )
                ORDER BY PET.ETIQUETA_ID";

            // Afegir clàusula ROWNUM si hi ha límit especificat
            if (limitRegistres > 0)
            {
                return $@"
                    SELECT * FROM (
                        {consultaBase}
                    ) WHERE ROWNUM <= {limitRegistres}";
            }

            return consultaBase;
        }

        /// <summary>
        /// Crea un registre de resultat de prova a partir del DataReader
        /// </summary>
        private ResultatMostra CrearRegistreDesDeReader(OracleDataReader reader, MultiRDbService mysqlService = null)
        {
            var registre = new ResultatMostra();

            try
            {
                // Camps obligatoris amb validació
                registre.EtiquetaId = reader["ETIQUETA_ID"]?.ToString()?.Trim();
                registre.PacientSap = reader["PACIENT_SAP"]?.ToString()?.Trim();
                
                // Nous camps afegits
                registre.Cip = reader["CIP"]?.ToString()?.Trim();
                registre.ColegiatId = reader["COLEGIAT_ID"]?.ToString()?.Trim();
                registre.NomMetge = reader["NOM_METGE"]?.ToString()?.Trim();
                registre.CentreDescripcio = reader["CENTRE_DESCRIPCIO"]?.ToString()?.Trim();
                registre.ServeiDescripcio = reader["SERVEI_DESCRIPCIO"]?.ToString()?.Trim();
                
                // Data de petició
                try
                {
                    registre.DataPeticioTrunc = reader["DATA_PETICIO_TRUNC"] != DBNull.Value 
                        ? Convert.ToDateTime(reader["DATA_PETICIO_TRUNC"]) 
                        : (DateTime?)null;
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Error convertint DATA_PETICIO_TRUNC per ETIQUETA_ID={registre.EtiquetaId}: {reader["DATA_PETICIO_TRUNC"]} - {ex.Message}");
                    registre.DataPeticioTrunc = null;
                }
                
                // Camps descriptius existents
                registre.AillamentDescripcio = reader["AILLAMENT_DESCRIPCIO"]?.ToString()?.Trim();
                registre.ProvaDescripcio = reader["PROVA_DESCRIPCIO"]?.ToString()?.Trim();
                registre.MostraDescripcio = reader["MOSTRA_DESCRIPCIO"]?.ToString()?.Trim();

                // Determinar si el microorganisme és especial
                if (mysqlService != null && !string.IsNullOrWhiteSpace(registre.AillamentDescripcio))
                {
                    try
                    {
                        registre.EsMicroorganismeEspecial = mysqlService.EsMicroorganismeEspecial(registre.AillamentDescripcio);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Error consultant si microorganisme '{registre.AillamentDescripcio}' és especial: {ex.Message}");
                        registre.EsMicroorganismeEspecial = null;
                    }
                }

                // Dates amb validació
                try
                {
                    registre.DataResultat = Convert.ToDateTime(reader["DATA_RESULTAT"]);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error convertint DATA_RESULTAT: {reader["DATA_RESULTAT"]} - {ex.Message}");
                }

                try
                {
                    registre.DataValidacio = reader["DATA_VALIDACIO"] != DBNull.Value 
                        ? Convert.ToDateTime(reader["DATA_VALIDACIO"]) 
                        : (DateTime?)null;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error convertint DATA_VALIDACIO: {reader["DATA_VALIDACIO"]} - {ex.Message}");
                }

                // Mecanismes de resistència (IDs) amb validació individual
                registre.MecanismeResistencia1Id = ConvertirMecanismeResistencia(reader, "MECANISME_RESISTENCIA1_ID", 1);
                registre.MecanismeResistencia2Id = ConvertirMecanismeResistencia(reader, "MECANISME_RESISTENCIA2_ID", 2);
                registre.MecanismeResistencia3Id = ConvertirMecanismeResistencia(reader, "MECANISME_RESISTENCIA3_ID", 3);
                registre.MecanismeResistencia4Id = ConvertirMecanismeResistencia(reader, "MECANISME_RESISTENCIA4_ID", 4);
                registre.MecanismeResistencia5Id = ConvertirMecanismeResistencia(reader, "MECANISME_RESISTENCIA5_ID", 5);

                // Descripcions dels mecanismes de resistència
                registre.MecanismeResistenciaDescrip = ConvertirDescripcioMecanismeResistencia(reader, "MECANISME_RESISTENCIA_DESCRIP", 1);
                registre.MecanismeResistenciaDescrip2 = ConvertirDescripcioMecanismeResistencia(reader, "MECANISME_RESISTENCIA_DESCRIP2", 2);
                registre.MecanismeResistenciaDescrip3 = ConvertirDescripcioMecanismeResistencia(reader, "MECANISME_RESISTENCIA_DESCRIP3", 3);
                registre.MecanismeResistenciaDescrip4 = ConvertirDescripcioMecanismeResistencia(reader, "MECANISME_RESISTENCIA_DESCRIP4", 4);
                registre.MecanismeResistenciaDescrip5 = ConvertirDescripcioMecanismeResistencia(reader, "MECANISME_RESISTENCIA_DESCRIP5", 5);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error creant registre des del DataReader: {ex.Message}", ex);
            }

            return registre;
        }

        /// <summary>
        /// Converteix un mecanisme de resistència de forma segura
        /// </summary>
        private string ConvertirMecanismeResistencia(OracleDataReader reader, string nomCamp, int numeroMecanisme)
        {
            try
            {
                var valor = reader[nomCamp];
                if (valor == DBNull.Value)
                    return null;

                return valor.ToString()?.Trim();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error convertint {nomCamp} (Mecanisme #{numeroMecanisme}): {reader[nomCamp]} - {ex.Message}");
            }
        }

        /// <summary>
        /// Converteix una descripció de mecanisme de resistència de forma segura
        /// </summary>
        private string ConvertirDescripcioMecanismeResistencia(OracleDataReader reader, string nomCamp, int numeroMecanisme)
        {
            try
            {
                var valor = reader[nomCamp];
                if (valor == DBNull.Value)
                    return null;

                return valor.ToString()?.Trim();
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error convertint {nomCamp} (Descripció Mecanisme #{numeroMecanisme}): {reader[nomCamp]} - {ex.Message}");
                return null; // En cas d'error, retornar null per les descripcions
            }
        }

        /// <summary>
        /// Carrega resultats de proves per un pacient específic
        /// </summary>
        /// <param name="pacientSap">Identificador SAP del pacient</param>
        /// <param name="diesEndarrera">Nombre de dies cap endarrera per carregar</param>
        /// <param name="limitRegistres">Límit màxim de registres a carregar (0 = sense límit)</param>
        /// <param name="mysqlService">Servei MySQL per consultar microorganismes especials (opcional)</param>
        /// <returns>Col·lecció de resultats filtrada per pacient</returns>
        public ColeccioMostres CarregarResultatsDeMostresPerPacient(string pacientSap, int diesEndarrera = 1, int limitRegistres = 0, MultiRDbService mysqlService = null)
        {
            _logger.Info($"Carregant resultats per pacient: SAP={pacientSap}, dies={diesEndarrera}, límit={limitRegistres}");
            
            var coleccioCompleta = CarregarResultatsDeMostres(diesEndarrera, mysqlService, limitRegistres);
            var coleccioFiltrada = new ColeccioMostres();
            
            var resultatsPacient = coleccioCompleta.ObtenirMostresPerPacient(pacientSap);
            foreach (var mostra in resultatsPacient)
            {
                foreach (var resultat in mostra.Resultats)
                {
                    coleccioFiltrada.AfegirResultat(resultat);
                }
            }
            
            _logger.Info($"Resultats filtrats per pacient {pacientSap}: {coleccioFiltrada.NombreTotalMostres} mostres, {coleccioFiltrada.NombreTotalResultats} resultats");
            
            return coleccioFiltrada;
        }

        /// <summary>
        /// Carrega resultats de proves per un rang de dates específic
        /// </summary>
        /// <param name="dataInici">Data d'inici del rang</param>
        /// <param name="dataFi">Data de fi del rang</param>
        /// <param name="mysqlService">Servei MySQL per consultar microorganismes especials (opcional)</param>
        /// <returns>Col·lecció de resultats filtrada per dates</returns>
        public ColeccioMostres CarregarResultatsDeMostresPerRangDates(DateTime dataInici, DateTime dataFi, MultiRDbService mysqlService = null)
        {
            _logger.Info($"Carregant resultats per rang de dates: {dataInici:dd/MM/yyyy} - {dataFi:dd/MM/yyyy}");
            
            var coleccioResultats = new ColeccioMostres();
            
            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                
                string sql = ObtenirConsultaResultatsProvesPerRangDates();
                
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("dataInici", dataInici.Date));
                    cmd.Parameters.Add(new OracleParameter("dataFi", dataFi.Date.AddDays(1).AddSeconds(-1)));
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var registre = CrearRegistreDesDeReader(reader, mysqlService);
                            coleccioResultats.AfegirRegistre(registre);
                        }
                    }
                }
            }

            _logger.Info($"Resultats carregats per rang de dates: {coleccioResultats.NombreTotalMostres} mostres, {coleccioResultats.NombreTotalRegistres} registres");
            
            return coleccioResultats;
        }

        /// <summary>
        /// Obté la consulta SQL per carregar els resultats per rang de dates
        /// </summary>
        private string ObtenirConsultaResultatsProvesPerRangDates()
        {
            return @"
                SELECT
                  PET.ETIQUETA_ID,
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
                  AND  (
                         ( PA.TIPUS is null )
                         AND
                         PET.ORIGEN  =  'DT'
                         AND
                         DETALL.TIPUS = 'A'
                         AND
                         (DETALL.DATA_VALIDACIO >= :dataInici AND DETALL.DATA_VALIDACIO <= :dataFi)
                         OR
                         (DETALL.DATA_RESULTAT >= :dataInici AND DETALL.DATA_RESULTAT <= :dataFi)
                        )
                ORDER BY PET.ETIQUETA_ID";
        }

        /// <summary>
        /// Obté un valor de forma segura del DataReader
        /// </summary>
        private string ObtenirValorSegur(OracleDataReader reader, string nomCamp)
        {
            try
            {
                var valor = reader[nomCamp];
                return valor == DBNull.Value ? "[NULL]" : valor.ToString();
            }
            catch
            {
                return "[ERROR_LECTURA]";
            }
        }

        /// <summary>
        /// Valida que un registre tingui les dades mínimes necessàries
        /// </summary>
        private bool ValidarRegistre(ResultatMostra registre, int numeroRegistre)
        {
            var errors = new List<string>();

            // Validacions obligatòries
            if (string.IsNullOrWhiteSpace(registre.EtiquetaId))
                errors.Add("ETIQUETA_ID és null o buida");

            if (string.IsNullOrWhiteSpace(registre.PacientSap))
                errors.Add("PACIENT_SAP és null o buit");

            if (registre.DataResultat == default(DateTime))
                errors.Add("DATA_RESULTAT no és vàlida");

            // Validacions opcionals amb avisos
            if (string.IsNullOrWhiteSpace(registre.AillamentDescripcio))
            {
                _logger.Warning($"Registre #{numeroRegistre} (ETIQUETA_ID={registre.EtiquetaId}): AILLAMENT_DESCRIPCIO és null o buida");
            }

            if (string.IsNullOrWhiteSpace(registre.ProvaDescripcio))
            {
                _logger.Warning($"Registre #{numeroRegistre} (ETIQUETA_ID={registre.EtiquetaId}): PROVA_DESCRIPCIO és null o buida");
            }

            if (string.IsNullOrWhiteSpace(registre.MostraDescripcio))
            {
                _logger.Warning($"Registre #{numeroRegistre} (ETIQUETA_ID={registre.EtiquetaId}): MOSTRA_DESCRIPCIO és null o buida");
            }

            // Validació de la data de validació (si existeix, ha de ser posterior a la data de resultat)
            if (registre.DataValidacio.HasValue && registre.DataValidacio.Value < registre.DataResultat)
            {
                errors.Add($"DATA_VALIDACIO ({registre.DataValidacio:dd/MM/yyyy}) és anterior a DATA_RESULTAT ({registre.DataResultat:dd/MM/yyyy})");
            }

            if (errors.Any())
            {
                string missatgeError = $"❌ Errors de validació al registre #{numeroRegistre} (ETIQUETA_ID={registre.EtiquetaId}): {string.Join("; ", errors)}";
                _logger.Error(missatgeError);
                
                Console.WriteLine($"❌ Errors de validació al registre #{numeroRegistre}:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"   - {error}");
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Mostra informació detallada d'un registre que ha causat error
        /// </summary>
        private void MostrarDetallsRegistreError(OracleDataReader reader, int numeroRegistre)
        {
            var detalls = new System.Text.StringBuilder();
            detalls.AppendLine($"📋 Detalls del registre #{numeroRegistre} amb error:");
            
            Console.WriteLine($"   📋 Detalls del registre #{numeroRegistre}:");
            
            try
            {
                // Camps principals
                detalls.AppendLine($"   - ETIQUETA_ID: {ObtenirValorSegur(reader, "ETIQUETA_ID")}");
                detalls.AppendLine($"   - PACIENT_SAP: {ObtenirValorSegur(reader, "PACIENT_SAP")}");
                detalls.AppendLine($"   - CIP: {ObtenirValorSegur(reader, "CIP")}");
                detalls.AppendLine($"   - COLEGIAT_ID: {ObtenirValorSegur(reader, "COLEGIAT_ID")}");
                detalls.AppendLine($"   - NOM_METGE: {ObtenirValorSegur(reader, "NOM_METGE")}");
                detalls.AppendLine($"   - CENTRE_DESCRIPCIO: {ObtenirValorSegur(reader, "CENTRE_DESCRIPCIO")}");
                detalls.AppendLine($"   - SERVEI_DESCRIPCIO: {ObtenirValorSegur(reader, "SERVEI_DESCRIPCIO")}");
                detalls.AppendLine($"   - AILLAMENT_DESCRIPCIO: {ObtenirValorSegur(reader, "AILLAMENT_DESCRIPCIO")}");
                detalls.AppendLine($"   - PROVA_DESCRIPCIO: {ObtenirValorSegur(reader, "PROVA_DESCRIPCIO")}");
                detalls.AppendLine($"   - MOSTRA_DESCRIPCIO: {ObtenirValorSegur(reader, "MOSTRA_DESCRIPCIO")}");
                detalls.AppendLine($"   - DATA_PETICIO_TRUNC: {ObtenirValorSegur(reader, "DATA_PETICIO_TRUNC")}");
                detalls.AppendLine($"   - DATA_RESULTAT: {ObtenirValorSegur(reader, "DATA_RESULTAT")}");
                detalls.AppendLine($"   - DATA_VALIDACIO: {ObtenirValorSegur(reader, "DATA_VALIDACIO")}");

                Console.WriteLine($"      - ETIQUETA_ID: {ObtenirValorSegur(reader, "ETIQUETA_ID")}");
                Console.WriteLine($"      - PACIENT_SAP: {ObtenirValorSegur(reader, "PACIENT_SAP")}");
                Console.WriteLine($"      - CIP: {ObtenirValorSegur(reader, "CIP")}");
                Console.WriteLine($"      - COLEGIAT_ID: {ObtenirValorSegur(reader, "COLEGIAT_ID")}");
                Console.WriteLine($"      - NOM_METGE: {ObtenirValorSegur(reader, "NOM_METGE")}");
                Console.WriteLine($"      - CENTRE_DESCRIPCIO: {ObtenirValorSegur(reader, "CENTRE_DESCRIPCIO")}");
                Console.WriteLine($"      - SERVEI_DESCRIPCIO: {ObtenirValorSegur(reader, "SERVEI_DESCRIPCIO")}");
                Console.WriteLine($"      - AILLAMENT_DESCRIPCIO: {ObtenirValorSegur(reader, "AILLAMENT_DESCRIPCIO")}");
                Console.WriteLine($"      - PROVA_DESCRIPCIO: {ObtenirValorSegur(reader, "PROVA_DESCRIPCIO")}");
                Console.WriteLine($"      - MOSTRA_DESCRIPCIO: {ObtenirValorSegur(reader, "MOSTRA_DESCRIPCIO")}");
                Console.WriteLine($"      - DATA_PETICIO_TRUNC: {ObtenirValorSegur(reader, "DATA_PETICIO_TRUNC")}");
                Console.WriteLine($"      - DATA_RESULTAT: {ObtenirValorSegur(reader, "DATA_RESULTAT")}");
                Console.WriteLine($"      - DATA_VALIDACIO: {ObtenirValorSegur(reader, "DATA_VALIDACIO")}");
                
                // Mecanismes de resistència
                for (int i = 1; i <= 5; i++)
                {
                    detalls.AppendLine($"   - MECANISME_RESISTENCIA{i}_ID: {ObtenirValorSegur(reader, $"MECANISME_RESISTENCIA{i}_ID")}");
                    detalls.AppendLine($"   - MECANISME_RESISTENCIA_DESCRIP{(i > 1 ? i.ToString() : "")}: {ObtenirValorSegur(reader, $"MECANISME_RESISTENCIA_DESCRIP{(i > 1 ? i.ToString() : "")}")}");
                    
                    Console.WriteLine($"      - MECANISME_RESISTENCIA{i}_ID: {ObtenirValorSegur(reader, $"MECANISME_RESISTENCIA{i}_ID")}");
                    Console.WriteLine($"      - MECANISME_RESISTENCIA_DESCRIP{(i > 1 ? i.ToString() : "")}: {ObtenirValorSegur(reader, $"MECANISME_RESISTENCIA_DESCRIP{(i > 1 ? i.ToString() : "")}")}");
                }

                _logger.Error(detalls.ToString());
            }
            catch (Exception ex)
            {
                _logger.Error($"⚠ Error mostrant detalls del registre #{numeroRegistre}: {ex.Message}", ex);
                Console.WriteLine($"      ⚠ Error mostrant detalls del registre: {ex.Message}");
            }
            
            Console.WriteLine(); // Línia en blanc per separar
        }
    }
}
