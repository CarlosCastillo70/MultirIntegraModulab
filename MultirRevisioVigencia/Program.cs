using System;
using System;
using MultirRevisioVigencia.Application.UseCases;
using MultirRevisioVigencia.Domain.Interfaces;
using MultirRevisioVigencia.Infrastructure.Configuration;
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

                // 2. Inicialitzar logger amb Serilog
                logger = new SerilogLoggerService(configuracio.RutaFitxerLog);
                logger.Info("=======================================================");
                logger.Info("  MULTIR - REVISIÓ DE VIGÈNCIA DE DIAGNÒSTICS");
                logger.Info("=======================================================");
                logger.Info($"Inici: {dataInici:dd/MM/yyyy HH:mm:ss}");
                logger.Info($"Entorn: {(configuracio.EsProducció ? "PRODUCCIÓ" : "PREPRODUCCIÓ")}");
                logger.Info("");

                // 3. Inicialitzar servei de base de dades
                var dbService = new MultiRDbService(configuracio.ConnectionStringMySQL, logger);

                // 4. Validar connexió

                // 4. Validar connexió
                if (!dbService.ValidarConnexio())
                {
                    logger.Error("❌ No s'ha pogut establir connexió amb la base de dades MySQL");
                    Environment.Exit(1);
                }

                logger.Info("✅ Connexió amb MySQL establerta correctament");

                // 5. Executar revisió de vigència
                var useCase = new RevisarVigenciaDiagnosticsUseCase(dbService, logger);
                var resum = useCase.Executar(configuracio.PacientsAProcessar, configuracio.LimitDiagnosticsAProcessar);

                // 6. Mostrar resum
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
                Console.WriteLine($"  - Per mostres negatives:       {resum.MarcatsPerMostresNegatives}");
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
                logger.Info($"  - Per mostres negatives:       {resum.MarcatsPerMostresNegatives}");
                logger.Info($"Diagnòstics amb error:           {resum.Errors}");
                logger.Info($"Durada:                          {durada.TotalSeconds:F2} segons");
                logger.Info("=======================================================");

                // 8. Finalitzar
                Console.WriteLine();
                Console.WriteLine("✅ Procés finalitzat correctament");
                logger.Info("");
                logger.Info("✅ Procés finalitzat correctament");
                
                // Fer flush i tancar el logger abans de sortir
                if (logger is IDisposable disposableLogger)
                {
                    disposableLogger.Dispose();
                }
                
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

                // Fer flush i tancar el logger abans de sortir
                if (logger is IDisposable disposableLogger)
                {
                    disposableLogger.Dispose();
                }

                Environment.Exit(1);
            }
        }
    }
}
