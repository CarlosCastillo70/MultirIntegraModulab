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

            // 1.1 Configurar els serveis d'infraestructura
            var configService = new ConfigurationService();
            var loggerService = new LoggerService();

            loggerService.MarcarIniciExecucio();
            loggerService.Info("=== Iniciant aplicació d' integració de dades de Modulab a MultiR ===");

            Application.DTOs.ResumProcessamentDto resum = null;
            bool hiHaHagutError = false;
            Exception errorGeneral = null;
            
            try
            {
                // 1.2 Validar i mostrar configuració
                configService.ValidarConfiguracio();
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

                // 1.6 Configurar servei d'aplicació
                var processamentService = new ProcessamentMostresService(
                    modulabRepository,
                    multiRRepository,
                    pacientWebService,
                    loggerService,
                    configService
                );

                // ===========================================================
                // FASE 2: TEST DE CONNEXIONS
                // ===========================================================
                
                loggerService.Info("👀 Comprovant connexions a bases de dades...");
                TestConnexions(modulabRepository, multiRRepository, loggerService);

                // ===========================================================
                // FASE 3: CÀRREGA DE DADES
                // ===========================================================
                
                int limitRegistres = configService.EntornProduccion ? 0 : configService.LimitResultatsProves;
                
                if (limitRegistres > 0)
                {
                    loggerService.Info($"⚠️ Mode PROVES: Procés limitat a {limitRegistres} resultats");
                }

                ColeccioMostres mostres;
                
                if (configService.CarregaIncremental)
                {
                    // ===========================================================
                    // MODE 1: CÀRREGA INCREMENTAL OPTIMITZADA
                    // ===========================================================
                    
                    loggerService.Info($"🔍 Carregant mostres amb càrrega incremental");

                    // 1. Obtenir última sincronització exitosa
                    var ultimaSincronitzacio = multiRRepository.ObtenirUltimaSincronitzacio();
                    
                    if (ultimaSincronitzacio != null)
                    {
                        // 2. Carregar amb filtres incrementals
                        mostres = modulabRepository.CarregarResultatsIncremental(
                            ultimaSincronitzacio, 
                            limitRegistres);
                    }
                    else
                    {
                        loggerService.Info("ℹ️ Primera execució - carregant mostres dels últims 7 dies");
                        
                        // 3. Primera càrrega (últims 7 dies per defecte)
                        mostres = modulabRepository.CarregarResultatsDiesEndarrera(7, limitRegistres);
                    }
                }
                else
                {
                    // ===========================================================
                    // MODE 2: CÀRREGA PER DIES ENRERE (CLÀSSICA)
                    // ===========================================================
                    
                    loggerService.Info($"🔍 Carregant mostres dels últims {configService.DiesEndarreraCarrega} dies...");
                    Console.WriteLine($"\n🔍 Carregant mostres dels últims {configService.DiesEndarreraCarrega} dies (mode clàssic)...");
                    
                    mostres = modulabRepository.CarregarResultatsDiesEndarrera(
                        configService.DiesEndarreraCarrega, 
                        limitRegistres);
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
                    if (configService.CarregaIncremental && resum.MostresAmbError == 0)
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
                                DiesRevisioSeguretat = 7, // Finestra de seguretat per validacions tardanes
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
                    else if (!configService.CarregaIncremental)
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
                            // Assegurar que tots els logs s'han escrit abans d'adjuntar el fitxer
                            loggerService.FlushLogs();
                            
                            loggerService.Info("📧 Enviant email amb el resum del processament");
                            
                            var emailService = new EmailService(
                                configService.SmtpServer,
                                configService.SmtpPort,
                                configService.SmtpUsuari,
                                configService.SmtpPassword,
                                configService.SmtpUsarSSL,
                                configService.EmailFrom,
                                configService.EmailsDestinataris,
                                loggerService
                            );

                            // Obtenir la ruta del log d'avui
                            string logFilePath = loggerService.ObtenirRutaLogAvui();

                            bool emailEnviat = false;

                            if (hiHaHagutError && errorGeneral != null)
                            {
                                // Enviar email d'error
                                emailEnviat = emailService.EnviarEmailError(
                                    "S'ha produït un error crític durant l'execució de la integració Modulab",
                                    errorGeneral,
                                    logFilePath
                                );
                            }
                            else if (resum != null)
                            {
                                // Enviar email amb resum normal
                                emailEnviat = emailService.EnviarEmailResumProcessament(
                                    resum,
                                    logFilePath
                                );
                            }
                            else
                            {
                                // Cas sense resum ni error (per exemple, no hi havia mostres)
                                emailEnviat = emailService.EnviarEmailAmbLog(
                                    "MultiR - Integració Modulab - Sense mostres a processar",
                                    "No s'han trobat mostres per processar en aquesta execució.",
                                    logFilePath
                                );
                            }

                            if (emailEnviat)
                            {
                                loggerService.Info("✅ Email enviat correctament");
                            }
                            else
                            {
                                loggerService.Error("⚠️ No s'ha pogut enviar l'email (revisa el log per més detalls)");
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
            Console.WriteLine($"   - Total registres: {mostres.NombreTotalRegistres}");

            var valides = mostres.ObtenirMostresValides();
            var pendents = mostres.ObtenirMostresPendentsValidacio();

            Console.WriteLine($"   - Mostres valides: {valides.Count}");
            Console.WriteLine($"   - Pendents validació: {pendents.Count}");

            logger.Info($"📊 Estadístiques: {mostres.NombreTotalMostres} mostres, {valides.Count} validades, {pendents.Count} pendents de validació");

            if (mostres.NombreTotalMostres > 0)
            {
                var percentatge = (valides.Count * 100.0) / mostres.NombreTotalMostres;
                Console.WriteLine($"   - % Validades: {percentatge:F1}%");
                logger.Info($"📊 Estadístiques:  % Validades: {percentatge:F1}%");
            }
            

            // Mostrar exemples
            var exemples = mostres.ObtenirTotesLesMostres().Take(3);
            Console.WriteLine($"\n🔍 EXEMPLES DE MOSTRES (primers 3):");
            logger.Info($"📊 Exemples de mostres (les 3 primeres)");

            foreach (var mostra in exemples)
            {
                Console.WriteLine($"\n   🧪 {mostra.EtiquetaId} - Pacient: {mostra.PacientSap}");
                Console.WriteLine($"      - Registres: {mostra.NombreRegistres}");
                Console.WriteLine($"      - Data: {mostra.DataPrimerResultat:dd/MM/yyyy HH:mm}");

                logger.Info($"  🧪 {mostra.EtiquetaId} - Pacient: {mostra.PacientSap} - Registres: {mostra.NombreRegistres} - Data resultat: {mostra.DataPrimerResultat:dd/MM/yyyy HH:mm}");


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
