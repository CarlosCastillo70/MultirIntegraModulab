using System;
using System.Linq;
using System.Threading.Tasks;
using MultirIntegraModulab.Infrastructure.Configuration;
using MultirIntegraModulab.Infrastructure.ExternalServices.Logger;
using MultirIntegraModulab.Infrastructure.ExternalServices.Pacient;
using MultirIntegraModulab.Infrastructure.ExternalServices.Email;
using MultirIntegraModulab.Infrastructure.Persistence.Repositories;
using MultirIntegraModulab.Application.Services;
using MultirIntegraModulab.Domain.Entities;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Exemple d'ús del sistema utilitzant Clean Architecture
    /// Aquest fitxer demostra com utilitzar la nova arquitectura
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            // ===========================================================
            // FASE 1: CONFIGURACIÓ - INICIALITZACIÓ
            // ===========================================================

            // 1.1 Configurar els serveis d'infraestructura bàsics
            var loggerService = new LoggerService();
            loggerService.MarcarIniciExecucio();
            loggerService.Info("=== Iniciant aplicació d' integració de dades de Modulab a MultiR ===");

            // 1.2 Configurar connexió temporal a MultiR per llegir paràmetres
            // NOTA: Utilitzem ConfigurationService base per obtenir connection string
            var configServiceTemp = new ConfigurationService();
            var multiRDbServiceTemp = new MultiRDbService(configServiceTemp.MySqlConnectionString);
            var multiRRepositoryTemp = new MultiRRepository(multiRDbServiceTemp, loggerService);

            // 1.3 Crear ConfigurationService HÍBRID que llegeix de BD + App.config
            var configService = new ConfigurationServiceHibrid(multiRRepositoryTemp, loggerService);

            Application.DTOs.ResumProcessamentDto resum = null;
            bool hiHaHagutError = false;
            Exception errorGeneral = null;
            
            try
            {
                // 1.2 Validar i mostrar configuració
                configService.ValidarConfiguracio();
                
                // Mostrar resum de configuració amb un log únic i compacte
                loggerService.Info("📋 Carregant configuració de l'aplicació...");
                var resumConfig = configService.ObtenirResumConfiguracio();
                loggerService.Info(resumConfig);
                Console.WriteLine(resumConfig);

                // 1.3 Configurar connexions a bases de dades
                var modulabDbService = new ModulabDbService(configService.OracleConnectionString, loggerService);
                var multiRDbService = new MultiRDbService(configService.MySqlConnectionString);

                // 1.4 Configurar repositoris (adaptadors)
                var modulabRepository = new ModulabRepository(modulabDbService, multiRDbService, loggerService);
                var multiRRepository = new MultiRRepository(multiRDbService, loggerService);

                // 1.5 Configurar servei de pacients
                var pacientWebService = new PacientWebServiceAdapter(
                    "http://10.80.160.178/flamma/ws/consultaPacient/consultaPacient.php", 
                    loggerService);
                loggerService.Info("✅ Web service SAP de pacients configurat");

                // 1.6 Configurar servei d'email per MDO (utilitzant la mateixa configuració)
                EmailService emailServiceMDO = null;
                if (configService.EnviarEmailLog)
                {
                    try
                    {
                        emailServiceMDO = new EmailService(
                            configService.SmtpServer,
                            configService.SmtpPort,
                            configService.SmtpUsuari,
                            configService.SmtpPassword,
                            configService.SmtpUsarSSL,
                            configService.EmailFrom,
                            configService.EmailsDestinataris,
                            loggerService
                        );
                        loggerService.Info("✅ Servei d'email per MDO configurat");
                    }
                    catch (Exception exEmail)
                    {
                        loggerService.Warning($"⚠️ No s'ha pogut configurar el servei d'email per MDO: {exEmail.Message}");
                    }
                }
                else
                {
                    loggerService.Info("ℹ️ Servei d'email desactivat - no s'enviaran alertes MDO");
                }

                // 1.7 Configurar servei d'aplicació
                var processamentService = new ProcessamentMostresService(
                    modulabRepository,
                    multiRRepository,
                    pacientWebService,
                    loggerService,
                    configService,
                    emailServiceMDO  // Passar el servei d'email
                );

                // ===========================================================
                // FASE 2: TEST DE CONNEXIONS
                // ===========================================================
                
                loggerService.Info("👀 Comprovant connexions a bases de dades...");
                TestConnexions(modulabRepository, multiRRepository, loggerService);

                // ===========================================================
                // FASE 3: CÀRREGA DE DADES
                // ===========================================================
                
                int limitRegistres = configService.EsEntornProduccio ? 0 : configService.LimitResultatsProves;
                
                if (limitRegistres > 0)
                {
                    loggerService.Info($"⚠️ Mode PROVES: Procés limitat a {limitRegistres} resultats");
                }

                ColeccioMostres mostres;
                
                // ===========================================================
                // DETERMINAR TIPUS DE CÀRREGA SEGONS PRIORITAT
                // Prioritat: 1. Incremental, 2. Dies Enrere, 3. Rang de Dates
                // ===========================================================
                
                if (configService.CarregaIncremental_Activa)
                {
                    // ===========================================================
                    // TIPUS 1: CÀRREGA INCREMENTAL OPTIMITZADA (Prioritat Alta)
                    // ===========================================================
                    
                    loggerService.Info($"🔍 Mode: CÀRREGA INCREMENTAL (Prioritat Alta)");
                    Console.WriteLine($"\n🔍 Mode: CÀRREGA INCREMENTAL");

                    // 1. Obtenir última sincronització exitosa
                    var ultimaSincronitzacio = multiRRepository.ObtenirUltimaSincronitzacio();
                    
                    if (ultimaSincronitzacio != null)
                    {
                        loggerService.Info($"📅 Última sincronització: {ultimaSincronitzacio.DataSincronitzacio:dd/MM/yyyy HH:mm}");
                        Console.WriteLine($"📅 Última sincronització: {ultimaSincronitzacio.DataSincronitzacio:dd/MM/yyyy HH:mm}");
                        
                        // 2. Carregar amb filtres incrementals
                        mostres = modulabRepository.CarregarResultatsIncremental(
                            ultimaSincronitzacio, 
                            limitRegistres);
                    }
                    else
                    {
                        int diesInicials = configService.CarregaIncremental_DiesRevisioSeguretat;
                        loggerService.Info($"ℹ️ Primera execució - carregant mostres dels últims {diesInicials} dies");
                        Console.WriteLine($"ℹ️ Primera execució - carregant mostres dels últims {diesInicials} dies");
                        
                        // 3. Primera càrrega (utilitzar dies de revisió de seguretat)
                        mostres = modulabRepository.CarregarResultatsDiesEndarrera(diesInicials, limitRegistres);
                    }
                }
                else if (configService.CarregaDiesEnrere_Activa)
                {
                    // ===========================================================
                    // TIPUS 2: CÀRREGA PER DIES ENRERE (Prioritat Mitjana)
                    // ===========================================================
                    
                    int diesEnrere = configService.CarregaDiesEnrere_NombreDies;
                    
                    loggerService.Info($"🔍 Mode: CÀRREGA PER DIES ENRERE");
                    loggerService.Info($"📅 Carregant mostres dels últims {diesEnrere} dies");
                    Console.WriteLine($"\n🔍 Mode: CÀRREGA PER DIES ENRERE");
                    Console.WriteLine($"📅 Carregant mostres dels últims {diesEnrere} dies...");
                    
                    mostres = modulabRepository.CarregarResultatsDiesEndarrera(diesEnrere, limitRegistres);
                }
                else if (configService.CarregaRangDates_Activa)
                {
                    // ===========================================================
                    // TIPUS 3: CÀRREGA PER RANG DE DATES (Prioritat Baixa)
                    // ===========================================================
                    
                    if (!configService.CarregaRangDates_DataInici.HasValue || 
                        !configService.CarregaRangDates_DataFi.HasValue)
                    {
                        throw new InvalidOperationException(
                            "CarregaRangDates_Activa està activat però les dates no estan configurades correctament. " +
                            "Revisar CarregaRangDates_DataInici i CarregaRangDates_DataFi a App.config");
                    }
                    
                    var dataInici = configService.CarregaRangDates_DataInici.Value;
                    var dataFi = configService.CarregaRangDates_DataFi.Value;
                    
                    loggerService.Info($"🔍 Mode: CÀRREGA PER RANG DE DATES (Prioritat Baixa)");
                    loggerService.Info($"📅 Carregant mostres del {dataInici:dd/MM/yyyy} al {dataFi:dd/MM/yyyy}");
                    Console.WriteLine($"\n🔍 Mode: CÀRREGA PER RANG DE DATES");
                    Console.WriteLine($"📅 Del {dataInici:dd/MM/yyyy} al {dataFi:dd/MM/yyyy}...");
                    
                    mostres = modulabRepository.CarregarResultatsPerRangDates(
                        dataInici, 
                        dataFi, 
                        limitRegistres);
                }
                else
                {
                    // ===========================================================
                    // ERROR: CAP TIPUS DE CÀRREGA ACTIVAT
                    // ===========================================================
                    
                    throw new InvalidOperationException(
                        "Cap tipus de càrrega està activat. " +
                        "Activar almenys un tipus a App.config: " +
                        "CarregaIncremental_Activa, CarregaDiesEnrere_Activa o CarregaRangDates_Activa");
                }


                // ===========================================================
                // FASE 4: PROCESSAMENT
                // ===========================================================
                
                if (mostres.NombreTotalMostres > 0)
                {
                    MostrarEstadistiques(mostres, loggerService);
                    
                    loggerService.Info("🔄 Començem a processar les mostres ...");
                    Console.WriteLine("\n🔄 Processant mostres...");

                    // Processar mostres utilitzant el servei d'aplicació
                    // --------------------------------------------------
                    resum = await processamentService.ProcessarMostresAsync(mostres);

                    // Mostrar resultats
                    MostrarResumProcessament(resum, loggerService);
                    
                    // ===========================================================
                    // GUARDAR DADES DE SINCRONITZACIÓ
                    // ===========================================================
                    
                    // Només guardar sincronització si estem en mode incremental
                    if (configService.CarregaIncremental_Activa && resum.MostresAmbError == 0)
                    {
                        try
                        {
                            loggerService.Info("💾 Guardant dades de sincronització...");
                            
                            var dadesSincronitzacio = new DadesSincronitzacio
                            {
                                DataResultatMaxProcessada = mostres.ObtenirDataResultatMaxima(),
                                DataValidacioMaxProcessada = mostres.ObtenirDataValidacioMaxima(),
                                DataSincronitzacio = DateTime.Now,
                                NombreMostresProcessades = mostres.NombreTotalMostres,
                                NombreMostresError = resum.MostresAmbError,
                                DiesRevisioSeguretat = configService.CarregaIncremental_DiesRevisioSeguretat,
                                Estat = "OK",
                                DuradaSegons = resum.DuradaProcessament.TotalSeconds
                            };
                            
                            int idSincronitzacio = multiRRepository.GuardarDadesSincronitzacio(dadesSincronitzacio);
                            
                            if (idSincronitzacio > 0)
                            {
                                loggerService.Info($"✅ Sincronització guardada correctament (ID: {idSincronitzacio})");
                            }
                            else
                            {
                                loggerService.Warning("⚠️ No s'ha pogut guardar la sincronització");
                            }
                        }
                        catch (Exception exSync)
                        {
                            loggerService.Error("❌ Error guardant sincronització", exSync);
                        }
                    }
                    else if (!configService.CarregaIncremental_Activa)
                    {
                        loggerService.Info("ℹ️ Mode càrrega incremental desactivat - no es guarden dades de sincronització");
                    }
                    else if (resum.MostresAmbError > 0)
                    {
                        loggerService.Warning($"⚠️ No es guarda sincronització perquè hi ha {resum.MostresAmbError} mostres amb error");
                        Console.WriteLine($"\n⚠️ Sincronització no guardada ({resum.MostresAmbError} errors detectats)");
                    }
                }
                else
                {
                    loggerService.Warning("⚠️ No s'han trobat mostres per processar");
                    Console.WriteLine("⚠️ No s'han trobat mostres per processar");
                }

                loggerService.Info("✅ Execució finalitzada correctament");
            }
            catch (Exception ex)
            {
                hiHaHagutError = true;
                errorGeneral = ex;
                
                loggerService.Error("\n❌ Error general en l'aplicació", ex);
                Console.WriteLine($"\n❌ Error general: {ex.Message}");
                Console.WriteLine($"Detalls: {ex.StackTrace}");
            }
            finally
            {
                loggerService.MarcarFinalExecucio();

                // ===========================================================
                // FASE 5: ENVIAMENT D'EMAIL (SI ESTÀ CONFIGURAT)
                // ===========================================================
                
                if (configService.EnviarEmailLog)
                {
                    try
                    {
                        // Determinar si s'ha d'enviar l'email
                        bool enviarEmail = true;
                        
                        if (configService.EmailNomesEnErrors && !hiHaHagutError && 
                            (resum == null || resum.MostresAmbError == 0))
                        {
                            enviarEmail = false;
                            loggerService.Info("ℹ️ No s'envia email perquè no hi ha errors (EmailNomesEnErrors=true)");
                        }

                        if (enviarEmail)
                        {
                            Console.WriteLine("\n📧 Preparant enviament d'email...");
                            
                            // Obtenir la ruta del log actual ABANS de tancar-lo
                            string logFilePathOriginal = loggerService.ObtenirRutaLogAvui();
                            
                            // Assegurar que tots els logs s'han escrit i tancar el fitxer
                            loggerService.FlushLogs();
                            
                            // Delay per assegurar que el fitxer està completament alliberat
                            System.Threading.Thread.Sleep(500);
                            
                            // Crear una còpia temporal del log per adjuntar a l'email
                            string logFilePathTemp = null;
                            if (System.IO.File.Exists(logFilePathOriginal))
                            {
                                try
                                {
                                    logFilePathTemp = logFilePathOriginal.Replace(".log", "_temp.log");
                                    System.IO.File.Copy(logFilePathOriginal, logFilePathTemp, overwrite: true);
                                    Console.WriteLine($"✅ Còpia temporal del log creada: {System.IO.Path.GetFileName(logFilePathTemp)}");
                                }
                                catch (Exception exCopy)
                                {
                                    Console.WriteLine($"⚠️ No s'ha pogut crear còpia del log: {exCopy.Message}");
                                    // Si no podem copiar, intentarem amb l'original
                                    logFilePathTemp = logFilePathOriginal;
                                }
                            }
                            
                            // A partir d'aquí, NO utilitzem més loggerService per evitar reobrir el fitxer
                            Console.WriteLine("📧 Enviant email amb el resum del processament...");
                            
                            var emailService = new EmailService(
                                configService.SmtpServer,
                                configService.SmtpPort,
                                configService.SmtpUsuari,
                                configService.SmtpPassword,
                                configService.SmtpUsarSSL,
                                configService.EmailFrom,
                                configService.EmailsDestinataris,
                                null  // NO passem logger per evitar que escrigui logs durant l'enviament
                            );

                            bool emailEnviat = false;

                            if (hiHaHagutError && errorGeneral != null)
                            {
                                // Enviar email d'error
                                emailEnviat = emailService.EnviarEmailError(
                                    "S'ha produït un error crític durant l'execució de la integració Modulab",
                                    errorGeneral,
                                    logFilePathTemp
                                );
                            }
                            else if (resum != null)
                            {
                                // Enviar email amb resum normal
                                emailEnviat = emailService.EnviarEmailResumProcessament(
                                    resum,
                                    logFilePathTemp
                                );
                            }
                            else
                            {
                                // Cas sense resum ni error (per exemple, no hi havia mostres)
                                emailEnviat = emailService.EnviarEmailAmbLog(
                                    "MultiR - Integració Modulab - Sense mostres a processar",
                                    "No s'han trobat mostres per processar en aquesta execució.",
                                    logFilePathTemp
                                );
                            }

                            if (emailEnviat)
                            {
                                Console.WriteLine("✅ Email enviat correctament");
                            }
                            else
                            {
                                Console.WriteLine("⚠️ No s'ha pogut enviar l'email");
                            }
                            
                            // Esborrar fitxer temporal si existeix i és diferent de l'original
                            if (logFilePathTemp != null && 
                                logFilePathTemp != logFilePathOriginal && 
                                System.IO.File.Exists(logFilePathTemp))
                            {
                                try
                                {
                                    System.IO.File.Delete(logFilePathTemp);
                                }
                                catch
                                {
                                    // Ignorar errors esborrant el temporal
                                }
                            }
                        }
                    }
                    catch (Exception exEmail)
                    {
                        loggerService.Error("❌ Error enviant email", exEmail);
                        Console.WriteLine($"⚠️ Error enviant email: {exEmail.Message}");
                    }
                }

                //Console.WriteLine("\nPrem qualsevol tecla per sortir...");
                //Console.ReadKey();
            }
        }

        /// <summary>
        /// Comprova les connexions a les bases de dades
        /// </summary>
        private static void TestConnexions(
            ModulabRepository modulabRepo,
            MultiRRepository multiRRepo,
            LoggerService logger)
        {
            try
            {
                Console.WriteLine("\n🔌 Comprovant connexions...");
                
                // Test Oracle
                var dataOracle = modulabRepo.GetCurrentDate();
                var tipusOracle = modulabRepo.GetDatabaseType();
                logger.Info($"✅ {tipusOracle} - Connexió correcta. Data: {dataOracle}");
                Console.WriteLine($"✅ {tipusOracle} - Connexió correcta. Data: {dataOracle}");

                // Test MySQL
                var dataMySQL = multiRRepo.GetCurrentDate();
                var tipusMySQL = multiRRepo.GetDatabaseType();
                logger.Info($"✅ {tipusMySQL} - Connexió correcta. Data: {dataMySQL}");
                Console.WriteLine($"✅ {tipusMySQL} - Connexió correcta. Data: {dataMySQL}");
            }
            catch (Exception ex)
            {
                logger.Error("❌ Error comprobant connexions", ex);
                Console.WriteLine($"❌ Error comprobant connexions: {ex.Message}");
            }
        }

        /// <summary>
        /// Mostra estadístiques de les mostres carregades
        /// </summary>
        private static void MostrarEstadistiques(
            ColeccioMostres mostres,
            LoggerService logger)
        {
            Console.WriteLine($"\n📊 ESTADÍSTIQUES:");
            Console.WriteLine($"   - Total mostres: {mostres.NombreTotalMostres}");
            Console.WriteLine($"   - Total registres: {mostres.NombreTotalResultats}");

            var valides = mostres.ObtenirMostresValides();
            var pendents = mostres.ObtenirMostresPendentsValidacio();

            Console.WriteLine($"   - Mostres valides: {valides.Count}");
            Console.WriteLine($"   - Pendents validació: {pendents.Count}");

            logger.Info($"📊 Estadístiques: {mostres.NombreTotalMostres} mostres, {valides.Count} validades, {pendents.Count} pendents de validació");

            if (mostres.NombreTotalMostres > 0)
            {
                var percentatge = (valides.Count * 100.0) / mostres.NombreTotalMostres;
                Console.WriteLine($"   - % Valides: {percentatge:F1}%");
                logger.Info($"📊 Estadístiques:  % Valides: {percentatge:F1}%");
            }
            

            // Mostrar exemples
            var exemples = mostres.ObtenirTotesLesMostres().Take(3);
            Console.WriteLine($"\n🔍 EXEMPLES DE MOSTRES (primers 3):");
            logger.Info($"📊 Exemples de mostres (les 3 primeres)");

            foreach (var mostra in exemples)
            {
                Console.WriteLine($"\n   🧪 {mostra.EtiquetaId} - Pacient: {mostra.PacientSap}");
                Console.WriteLine($"      - Registres: {mostra.NombreResultats}");
                Console.WriteLine($"      - Data: {mostra.DataPrimerResultat:dd/MM/yyyy HH:mm}");

                logger.Info($"  🧪 {mostra.EtiquetaId} - Pacient: {mostra.PacientSap} - Registres: {mostra.NombreResultats} - Data resultat: {mostra.DataPrimerResultat:dd/MM/yyyy HH:mm}");


                if (mostra.Microorganismes.Any())
                {
                    Console.WriteLine($"      - Microorganismes: {string.Join(", ", mostra.Microorganismes)}");
                    logger.Info($"  🧪 Microorganismes: {string.Join(", ", mostra.Microorganismes)}");
                }

                if (mostra.MecanismesResistencia.Any())
                {
                    Console.WriteLine($"      - Mecanismes de resistència: {string.Join(", ", mostra.MecanismesResistencia)}");
                    logger.Info($"  🧪 Mecanismes de resistència: {string.Join(", ", mostra.MecanismesResistencia)}");
                }
            }
        }

        /// <summary>
        /// Mostra el resum del processament
        /// </summary>
        private static void MostrarResumProcessament(
            Application.DTOs.ResumProcessamentDto resum,
            LoggerService logger)
        {
            Console.WriteLine($"\n📊 RESUM DEL PROCESSAMENT:");
            Console.WriteLine($"   Total processats: {resum.TotalProcessats}");
            Console.WriteLine($"   Noves incorporacions: {resum.NovesIncorporacions}");
            Console.WriteLine($"   Repetides: {resum.MostresRepetides}");
            Console.WriteLine($"   Errors: {resum.MostresAmbError}");
            Console.WriteLine($"   Durada: {resum.DuradaProcessament.TotalSeconds:F2}s");

            // logger.Info($"Processament finalitzat: {resum}");
        }
    }
}
