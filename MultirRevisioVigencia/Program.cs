using System;
using MultirRevisioVigencia.Application.UseCases;
using MultirRevisioVigencia.Domain.Interfaces;
using MultirRevisioVigencia.Infrastructure.Configuration;
using MultirRevisioVigencia.Infrastructure.ExternalServices.Email;
using MultirRevisioVigencia.Infrastructure.Logging;
using MultirRevisioVigencia.Infrastructure.Persistence.LegacyServices;

namespace MultirRevisioVigencia
{
    /// <summary>
    /// Programa principal per a la revisió automàtica de vigència de diagnòstics
    /// S'executa diàriament per marcar com a no vigents els diagnòstics que han superat el seu període de vigència
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            DateTime dataInici = DateTime.Now;
            ILoggerService logger = null;
            EmailService emailService = null;

            try
            {
                Console.WriteLine("=======================================================");
                Console.WriteLine("  MULTIR - REVISIÓ DE VIGÈNCIA DE DIAGNÒSTICS");
                Console.WriteLine("=======================================================");
                Console.WriteLine($"Inici: {dataInici:dd/MM/yyyy HH:mm:ss}");
                Console.WriteLine();

                // 1. Carregar configuració
                var configuracio = ConfiguracioManager.CarregarConfiguracio();
                if (configuracio == null)
                {
                    Console.WriteLine("❌ Error carregant configuració");
                    Environment.Exit(1);
                }

                // 2. Inicialitzar logger
                logger = new FileLoggerService(configuracio.RutaFitxerLog);
                logger.Info("=======================================================");
                logger.Info("  MULTIR - REVISIÓ DE VIGÈNCIA DE DIAGNÒSTICS");
                logger.Info("=======================================================");
                logger.Info($"Inici: {dataInici:dd/MM/yyyy HH:mm:ss}");
                logger.Info($"Entorn: {(configuracio.EsProducció ? "PRODUCCIÓ" : "PREPRODUCCIÓ")}");
                logger.Info("");

                // 3. Inicialitzar servei d'email
                emailService = new EmailService(
                    configuracio.SmtpServer,
                    configuracio.SmtpPort,
                    configuracio.SmtpUsuari,
                    configuracio.SmtpPassword,
                    configuracio.UsarSSL,
                    configuracio.EmailFrom,
                    configuracio.EmailsDestinataris,
                    logger
                );

                // 4. Inicialitzar servei de base de dades
                var dbService = new MultiRDbService(configuracio.ConnectionStringMySQL, logger);

                // 5. Validar connexió
                if (!dbService.ValidarConnexio())
                {
                    logger.Error("❌ No s'ha pogut establir connexió amb la base de dades MySQL");
                    Environment.Exit(1);
                }

                logger.Info("✅ Connexió amb MySQL establerta correctament");

                // 6. Executar revisió de vigència
                var useCase = new RevisarVigenciaDiagnosticsUseCase(dbService, logger);
                var resum = useCase.Executar(configuracio.PacientsAProcessar, configuracio.LimitDiagnosticsAProcessar);

                // 7. Mostrar resum
                DateTime dataFi = DateTime.Now;
                TimeSpan durada = dataFi - dataInici;

                Console.WriteLine();
                Console.WriteLine("=======================================================");
                Console.WriteLine("  RESUM DE LA REVISIÓ");
                Console.WriteLine("=======================================================");
                Console.WriteLine($"Total diagnòstics revisats:      {resum.TotalRevisats}");
                Console.WriteLine($"Diagnòstics marcats no vigents:  {resum.MarcatsNoVigents}");
                Console.WriteLine($"  - Per èxitus del pacient:      {resum.MarcatsPerExitus}");
                Console.WriteLine($"  - Per superar vigència:        {resum.MarcatsPerVigencia}");
                Console.WriteLine($"Diagnòstics amb error:           {resum.Errors}");
                Console.WriteLine($"Durada:                          {durada.TotalSeconds:F2} segons");
                Console.WriteLine("=======================================================");

                logger.Info("");
                logger.Info("=======================================================");
                logger.Info("  RESUM DE LA REVISIÓ");
                logger.Info("=======================================================");
                logger.Info($"Total diagnòstics revisats:      {resum.TotalRevisats}");
                logger.Info($"Diagnòstics marcats no vigents:  {resum.MarcatsNoVigents}");
                logger.Info($"  - Per èxitus del pacient:      {resum.MarcatsPerExitus}");
                logger.Info($"  - Per superar vigència:        {resum.MarcatsPerVigencia}");
                logger.Info($"Diagnòstics amb error:           {resum.Errors}");
                logger.Info($"Durada:                          {durada.TotalSeconds:F2} segons");
                logger.Info("=======================================================");

                // 8. Enviar email de resum
                if (resum.MarcatsNoVigents > 0 || resum.Errors > 0)
                {
                    emailService.EnviarEmailResumRevisio(resum, configuracio.RutaFitxerLog);
                }
                else
                {
                    logger.Info("");
                    logger.Info("ℹ️ No s'envia email (no hi ha diagnòstics marcats ni errors)");
                }

                // 9. Finalitzar
                Console.WriteLine();
                Console.WriteLine("✅ Procés finalitzat correctament");
                logger.Info("");
                logger.Info("✅ Procés finalitzat correctament");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"❌ ERROR CRÍTIC: {ex.Message}");
                Console.WriteLine(ex.StackTrace);

                if (logger != null)
                {
                    logger.Error($"❌ ERROR CRÍTIC: {ex.Message}", ex);
                }

                if (emailService != null && logger != null)
                {
                    emailService.EnviarEmailError("Error crític en la revisió de vigència", ex, logger.GetLogFilePath());
                }

                Environment.Exit(1);
            }
        }
    }
}
