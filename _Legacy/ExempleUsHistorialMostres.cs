using System;
using System.Collections.Generic;
using System.Linq;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Exemple d'ús de la funcionalitat d'historial de mostres
    /// Demostra com consultar i gestionar l'historial de canvis de les mostres
    /// </summary>
    public static class ExempleUsHistorialMostres
    {
        /// <summary>
        /// Exemple complet de com utilitzar l'historial de mostres
        /// </summary>
        public static async void ExempleCompletHistorial()
        {
            Console.WriteLine("?? EXEMPLE D'ÚS DE L'HISTORIAL DE MOSTRES\n");

            try
            {
                // Configurar el servei MySQL
                string connectionString = "Server=your_server;Database=your_db;Uid=your_user;Pwd=your_password;";
                var mysqlService = new MultiRDbService(connectionString);

                // Validar connexió
                if (!mysqlService.ValidarConnexio())
                {
                    Console.WriteLine("? Error de connexió amb la base de dades");
                    return;
                }

                Console.WriteLine("? Connexió establerta amb la base de dades\n");

                // 1. Mostrar estadístiques generals de l'historial
                MostrarEstadistiquesHistorial(mysqlService);

                // 2. Consultar historial d'una mostra específica
                string etiquetaTest = "400816071"; // Etiqueta d'exemple
                ConsultarHistorialMostra(mysqlService, etiquetaTest);

                // 3. Exemple de processament que genera historial
                await ExempleProcessamentAmbHistorial(mysqlService);

                // 4. Consultar canvis recents
                ConsultarCanvisRecents(mysqlService);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error en l'exemple d'historial: {ex.Message}");
                Console.WriteLine($"Stacktrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Mostra les estadístiques generals de l'historial
        /// </summary>
        private static void MostrarEstadistiquesHistorial(MultiRDbService mysqlService)
        {
            Console.WriteLine("?? ESTADÍSTIQUES GENERALS DE L'HISTORIAL:");

            var estadistiques = mysqlService.ObtenirEstadistiquesHistorial();

            if (estadistiques.TotalRegistresHistorial == 0)
            {
                Console.WriteLine("   • No hi ha registres d'historial encara");
                Console.WriteLine("   • L'historial es genera automàticament quan es detecten canvis en mostres validades/revalidades/desvalidades\n");
                return;
            }

            Console.WriteLine($"   • Total registres d'historial: {estadistiques.TotalRegistresHistorial}");

            if (estadistiques.RegistresPerTipus.Any())
            {
                Console.WriteLine("   • Distribució per tipus de canvi:");
                foreach (var tipus in estadistiques.RegistresPerTipus.OrderByDescending(x => x.Value))
                {
                    string descripcio = ObtenirDescripcioTipusCanvi(tipus.Key);
                    Console.WriteLine($"     - {tipus.Key}: {tipus.Value} registres ({descripcio})");
                }
            }

            if (estadistiques.PrimerRegistre.HasValue && estadistiques.UltimRegistre.HasValue)
            {
                Console.WriteLine($"   • Període: del {estadistiques.PrimerRegistre.Value:dd/MM/yyyy} al {estadistiques.UltimRegistre.Value:dd/MM/yyyy}");
                
                var diferenciaDies = (estadistiques.UltimRegistre.Value - estadistiques.PrimerRegistre.Value).TotalDays;
                if (diferenciaDies > 0)
                {
                    var mitjanaPerDia = estadistiques.TotalRegistresHistorial / diferenciaDies;
                    Console.WriteLine($"   • Mitjana: {mitjanaPerDia:F1} canvis per dia");
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Consulta l'historial d'una mostra específica
        /// </summary>
        private static void ConsultarHistorialMostra(MultiRDbService mysqlService, string etiquetaId)
        {
            Console.WriteLine($"?? HISTORIAL DE LA MOSTRA: {etiquetaId}");

            // Comprovar si existeix historial per aquesta mostra
            int countHistorial = mysqlService.ComprovarHistorialExisteix(etiquetaId);

            if (countHistorial == 0)
            {
                Console.WriteLine($"   • No hi ha historial per la mostra {etiquetaId}");
                Console.WriteLine("   • Això indica que la mostra no ha tingut canvis significatius o és nova\n");
                return;
            }

            Console.WriteLine($"   • Trobats {countHistorial} registres d'historial per {etiquetaId}");

            // Obtenir l'historial complet
            var historial = mysqlService.ObtenirHistorialMostra(etiquetaId);

            if (historial.Any())
            {
                Console.WriteLine("   • Cronologia de canvis (més recent primer):");

                foreach (var registre in historial.Take(5)) // Mostrar només els últims 5
                {
                    Console.WriteLine($"     - {registre.DataCanvi?.ToString("dd/MM/yyyy HH:mm")}: {registre.TipusCanvi}");
                    Console.WriteLine($"       Estat anterior: {registre.EstatAbansCanvi}");
                    
                    if (!string.IsNullOrEmpty(registre.Microorganisme))
                    {
                        Console.WriteLine($"       Microorganisme: {registre.Microorganisme}");
                    }
                    
                    if (!string.IsNullOrEmpty(registre.MecanismeResistencia))
                    {
                        Console.WriteLine($"       Mecanisme: {registre.MecanismeResistencia}");
                    }
                    
                    if (!string.IsNullOrEmpty(registre.Observacions))
                    {
                        Console.WriteLine($"       Observacions: {registre.Observacions}");
                    }
                    
                    Console.WriteLine();
                }

                if (historial.Count > 5)
                {
                    Console.WriteLine($"   • ... i {historial.Count - 5} registres més antics");
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Exemple de processament que pot generar historial
        /// </summary>
        private static async System.Threading.Tasks.Task ExempleProcessamentAmbHistorial(MultiRDbService mysqlService)
        {
            Console.WriteLine("?? EXEMPLE DE PROCESSAMENT AMB HISTORIAL:");

            try
            {
                // Simular una col·lecció de resultats amb mostres que poden tenir canvis
                var coleccioResultats = new ColeccioResultatsMostres();

                // Afegir algunes mostres d'exemple (en un cas real vindrien d'Oracle)
                // Aquestes mostres simulen casos que podrien generar historial

                Console.WriteLine("   ? Simulant processament de mostres...");

                // Executar el tractament
                var tractament = new TractamentResultats(mysqlService);
                var resum = await tractament.ProcessarMostres(coleccioResultats);

                Console.WriteLine($"\n   ? Processament completat:");
                Console.WriteLine($"     - Total mostres: {resum.TotalProcessats}");
                Console.WriteLine($"     - Mostres amb canvis historiats: {resum.MostresAmbCanvis}");

                if (resum.MostresAmbCanvis > 0)
                {
                    Console.WriteLine($"   ? {resum.MostresAmbCanvis} mostres han generat registres d'historial");
                    Console.WriteLine("   ? Aquestes mostres tenien canvis en les combinacions microorganisme-mecanisme");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ? Error en l'exemple de processament: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Consulta els canvis més recents de l'historial
        /// </summary>
        private static void ConsultarCanvisRecents(MultiRDbService mysqlService)
        {
            Console.WriteLine("?? CANVIS RECENTS (ÚLTIMES 24 HORES):");

            try
            {
                // En un cas real, aquí faríem una consulta SQL per obtenir canvis recents
                // Per simplicitat, utilitzem les estadístiques generals

                var estadistiques = mysqlService.ObtenirEstadistiquesHistorial();

                if (estadistiques.UltimRegistre.HasValue)
                {
                    var horasDesdeUltimCanvi = (DateTime.Now - estadistiques.UltimRegistre.Value).TotalHours;

                    if (horasDesdeUltimCanvi <= 24)
                    {
                        Console.WriteLine($"   • Últim canvi registrat: {estadistiques.UltimRegistre.Value:dd/MM/yyyy HH:mm}");
                        Console.WriteLine($"   • Fa {horasDesdeUltimCanvi:F1} hores");

                        if (horasDesdeUltimCanvi <= 1)
                        {
                            Console.WriteLine("   • ? Activitat recent detectada");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   • No hi ha canvis en les últimes 24 hores");
                        Console.WriteLine($"   • Últim canvi: {estadistiques.UltimRegistre.Value:dd/MM/yyyy}");
                    }
                }
                else
                {
                    Console.WriteLine("   • No hi ha registres d'historial");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ? Error consultant canvis recents: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Obté la descripció d'un tipus de canvi
        /// </summary>
        private static string ObtenirDescripcioTipusCanvi(string tipusCanvi)
        {
            switch (tipusCanvi?.ToUpper())
            {
                case "DESVALIDADA_CANVI":
                    return "mostra desvalidada amb canvis";
                case "VALIDADA_CANVI":
                    return "mostra validada amb canvis";
                case "REVALIDADA_CANVI":
                    return "mostra revalidada amb canvis";
                default:
                    return "tipus de canvi desconegut";
            }
        }

        /// <summary>
        /// Utilitat per netejar l'historial més antic si cal (manteniment)
        /// </summary>
        public static void ExempleMantenimentHistorial(MultiRDbService mysqlService, int diesRetencio = 90)
        {
            Console.WriteLine($"?? MANTENIMENT DE L'HISTORIAL (retenció: {diesRetencio} dies):");

            try
            {
                // En una implementació real, aquí s'executaria una consulta per esborrar
                // registres d'historial més antics de X dies
                
                Console.WriteLine("   • NOTA: Implementació de neteja pendent");
                Console.WriteLine($"   • Es recomanaria esborrar registres anteriors a {DateTime.Now.AddDays(-diesRetencio):dd/MM/yyyy}");
                Console.WriteLine("   • SQL recomanada:");
                Console.WriteLine($"     DELETE FROM pacients_diagnostics_mostra_historial");
                Console.WriteLine($"     WHERE data_canvi < DATE_SUB(NOW(), INTERVAL {diesRetencio} DAY);");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ? Error en manteniment d'historial: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Exemple específic per testejar la funcionalitat d'historial
        /// </summary>
        public static void TestFuncionalitat()
        {
            Console.WriteLine("?? TEST DE FUNCIONALITAT D'HISTORIAL\n");

            var mysqlService = new MultiRDbService("connection_string_test");

            // Test 1: Estadístiques d'historial
            Console.WriteLine("Test 1: Estadístiques");
            var stats = mysqlService.ObtenirEstadistiquesHistorial();
            Console.WriteLine($"   • Total registres: {stats.TotalRegistresHistorial}");

            // Test 2: Historial d'una mostra
            Console.WriteLine("\nTest 2: Historial de mostra específica");
            string etiqueta = "400816071";
            int count = mysqlService.ComprovarHistorialExisteix(etiqueta);
            Console.WriteLine($"   • Historial per {etiqueta}: {count} registres");

            // Test 3: Simulació de guardada d'historial
            Console.WriteLine("\nTest 3: Simulació de guardada");
            bool guardat = mysqlService.GuardarHistorialMostra(etiqueta, "TEST_CANVI", "Test de funcionalitat");
            Console.WriteLine($"   • Guardada simulada: {(guardat ? "Exitosa" : "Error")}");

            Console.WriteLine("\n? Tests completats");
        }
    }
}