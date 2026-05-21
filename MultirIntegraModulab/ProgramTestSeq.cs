using System;
using MultirIntegraModulab.Infrastructure.ExternalServices.Logger;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Programa de prova per generar logs amb diferents nivells a Seq
    /// Això permet configurar els signals a Seq
    /// </summary>
    class ProgramTestSeq
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Test de Logs per Seq - Configuració de Signals ===");
            Console.WriteLine();

            using (var logger = new LoggerService())
            {
                logger.MarcarIniciExecucio();
                
                Console.WriteLine("?? Generant logs amb diferents nivells...");
                Console.WriteLine();
                
                // Generar logs de prova
                logger.GenerarLogsDeProva();
                
                Console.WriteLine();
                Console.WriteLine("? Logs generats correctament!");
                Console.WriteLine();
                Console.WriteLine("?? Ara ves a Seq (http://localhost:5341) i:");
                Console.WriteLine("   1. Fes clic a 'Signals' al menú lateral");
                Console.WriteLine("   2. Haurien d'aparèixer automàticament signals per:");
                Console.WriteLine("      - Warnings (??)");
                Console.WriteLine("      - Errors (?)");
                Console.WriteLine("      - Fatal (??)");
                Console.WriteLine();
                Console.WriteLine("?? Si no apareixen, fes clic a 'New Signal' i crea'ls manualment:");
                Console.WriteLine("   - Warning: @Level = 'Warning'");
                Console.WriteLine("   - Error: @Level = 'Error'");
                Console.WriteLine("   - Fatal: @Level = 'Fatal'");
                Console.WriteLine();
                
                logger.MarcarFinalExecucio();
            }

            Console.WriteLine("Prem qualsevol tecla per sortir...");
            Console.ReadKey();
        }
    }
}
