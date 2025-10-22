using System;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Exemple d'ús del sistema de tractament de mostres
    /// </summary>
    public class ExempleUsTractament
    {
        public static async void Executar()
        {
            try
            {
                // Configurar serveis
                string oracleConnStr = "Data Source=...; User Id=...; Password=...;";
                string mysqlConnStr = "Server=...; Database=...; Uid=...; Pwd=...;";

                var oracleService = new ModulabDbService(oracleConnStr);
                var mysqlService = new MultiRDbService(mysqlConnStr);
                
                Console.WriteLine("🚀 Iniciant processament de mostres de Modulab...");

                // Validar connexions
                if (!mysqlService.ValidarConnexio())
                {
                    Console.WriteLine("❌ No es pot connectar a MySQL. Sortint...");
                    return;
                }

                // Carregar mostres d'Oracle (últims 2 dies)
                var coleccioMostres = oracleService.CarregarResultatsDeMostres(
                    diesEndarrera: 2, 
                    mysqlService: mysqlService);

                if (coleccioMostres.NombreTotalResultats == 0)
                {
                    Console.WriteLine("⚠️ No s'han trobat mostres per processar.");
                    return;
                }

                Console.WriteLine($"📋 Mostres carregades: {coleccioMostres.NombreTotalResultats}");
                Console.WriteLine($"📋 Total resultats: {coleccioMostres.NombreTotalRegistres}");

                // Processar mostres
                var tractament = new TractamentResultats(mysqlService);
                var resum = await tractament.ProcessarMostres(coleccioMostres);

                Console.WriteLine($"\n✅ Processament completat!");
                Console.WriteLine(resum.ToString());

                // Mostrar estadístiques addicionals si cal
                if (resum.MostresAmbError > 0)
                {
                    Console.WriteLine($"\n⚠️ S'han produït {resum.MostresAmbError} errors durant el processament.");
                    Console.WriteLine("   Revisa els logs per més detalls.");
                }

                if (resum.MostresRepetides > 0)
                {
                    Console.WriteLine($"\n💡 {resum.MostresRepetides} mostres repetides s'han saltat (normal en execucions freqüents).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error crític en el processament: {ex.Message}");
                Console.WriteLine($"Stacktrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Exemple per processar només un pacient específic
        /// </summary>
        public static async void ProcessarPacientEspecific(string pacientSap)
        {
            var oracleService = new ModulabDbService("...");
            var mysqlService = new MultiRDbService("...");
            
            Console.WriteLine($"🔍 Processant mostres per pacient: {pacientSap}");

            var coleccio = oracleService.CarregarResultatsDeMostresPerPacient(pacientSap, diesEndarrera: 30);
            var tractament = new TractamentResultats(mysqlService);
            var resum = await tractament.ProcessarMostres(coleccio);

            Console.WriteLine($"Resultat per pacient {pacientSap}: {resum}");

            // Mostrar estadístiques del processament
            if (resum.MostresAmbError > 0)
            {
                Console.WriteLine($"Errors en el processament: {resum.MostresAmbError}");
            }

            if (resum.MostresRepetides > 0)
            {
                Console.WriteLine($"Mostres repetides: {resum.MostresRepetides}");
            }
        }

        /// <summary>
        /// Exemple bàsic d'ús del TractamentResultats
        /// </summary>
        public static async void ProcessarResultatsModulab()
        {
            var oracleService = new ModulabDbService("...");
            var mysqlService = new MultiRDbService("...");
            
            Console.WriteLine("🚀 Iniciant processament de resultats de Modulab");

            try
            {
                // Carregar resultats dels últims 2 dies amb límit de 100 registres per proves
                var coleccio = oracleService.CarregarResultatsDeMostres(diesEndarrera: 2, mysqlService, limitRegistres: 100);
                
                Console.WriteLine($"📊 Carregats {coleccio.NombreTotalResultats} resultats amb {coleccio.NombreTotalRegistres} registres");

                // Processar els resultats
                var tractament = new TractamentResultats(mysqlService);
                var resum = await tractament.ProcessarMostres(coleccio);

                Console.WriteLine($"✅ Processament completat amb {resum.MostresAmbError} errors.");

                // Mostrar estadístiques addicionals si cal
                if (resum.MostresAmbError > 0)
                {
                    Console.WriteLine($"\n⚠️ S'han produït {resum.MostresAmbError} errors durant el processament.");
                    Console.WriteLine("   Revisa els logs per més detalls.");
                }

                if (resum.MostresRepetides > 0)
                {
                    Console.WriteLine($"\n💡 {resum.MostresRepetides} mostres repetides s'han saltat (normal en execucions freqüents).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error en el processament de resultats: {ex.Message}");
            }
        }
    }
}