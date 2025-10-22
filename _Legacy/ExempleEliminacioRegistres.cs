// FITXER COMENTAT TEMPORALMENT PER COMPILACIÓ
/*
using System;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Exemple d'ús de les funcions d'eliminació de registres per etiqueta
    /// Demostra com esborrar i gestionar registres de pacients_diagnostics_mostra
    /// </summary>
    public static class ExempleEliminacioRegistres
    {
        /// <summary>
        /// Exemple complet de gestió d'eliminació de registres
        /// </summary>
        public static void ExempleCompletEliminacio()
        {
            Console.WriteLine("??? EXEMPLE D'ELIMINACIÓ DE REGISTRES PER ETIQUETA\n");

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

                // Etiqueta d'exemple per fer proves
                string etiquetaTest = "400816071";

                // 1. Mostrar informació inicial de l'etiqueta
                MostrarInformacioEtiqueta(mysqlService, etiquetaTest);

                // 2. Exemple d'esborrat simple
                ExempleEsborratSimple(mysqlService, etiquetaTest);

                // 3. Exemple d'esborrat amb confirmació
                ExempleEsborratAmbConfirmacio(mysqlService, etiquetaTest);

                // 4. Exemple de restauració
                ExempleRestauracio(mysqlService, etiquetaTest);

                // 5. Gestió d'errors
                ExempleGestioErrors(mysqlService);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error en l'exemple d'eliminació: {ex.Message}");
                Console.WriteLine($"Stacktrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Mostra informació detallada sobre una etiqueta
        /// </summary>
        private static void MostrarInformacioEtiqueta(MultiRDbService mysqlService, string etiquetaId)
        {
            Console.WriteLine($"?? INFORMACIÓ DE L'ETIQUETA: {etiquetaId}");

            var info = mysqlService.ObtenirInformacioRegistresEtiqueta(etiquetaId);

            Console.WriteLine($"   • Total registres: {info.TotalRegistres}");
            Console.WriteLine($"   • Registres actius: {info.RegistresActius}");
            Console.WriteLine($"   • Registres esborrats: {info.RegistresEsborrats}");

            if (info.PrimerRegistre.HasValue)
            {
                Console.WriteLine($"   • Primer registre: {info.PrimerRegistre.Value:dd/MM/yyyy HH:mm}");
            }

            if (info.UltimRegistre.HasValue)
            {
                Console.WriteLine($"   • Últim registre: {info.UltimRegistre.Value:dd/MM/yyyy HH:mm}");
            }

            if (info.UltimaEliminacio.HasValue)
            {
                Console.WriteLine($"   • Última eliminació: {info.UltimaEliminacio.Value:dd/MM/yyyy HH:mm}");
            }

            Console.WriteLine($"   • Estat: {info}");
            Console.WriteLine();
        }

        /// <summary>
        /// Exemple d'esborrat simple de registres
        /// </summary>
        private static void ExempleEsborratSimple(MultiRDbService mysqlService, string etiquetaId)
        {
            Console.WriteLine("??? EXEMPLE D'ESBORRAT SIMPLE:");

            // Comprovar registres actius abans de l'esborrat
            int registresActius = mysqlService.ComprovarRegistresActiusPerEtiqueta(etiquetaId);
            Console.WriteLine($"   • Registres actius abans de l'esborrat: {registresActius}");

            if (registresActius > 0)
            {
                // Esborrar registres
                int registresEsborrats = mysqlService.EsborrarRegistresPerEtiqueta(etiquetaId);
                Console.WriteLine($"   • Registres esborrats: {registresEsborrats}");

                // Verificar estat després de l'esborrat
                int registresActiusDesprés = mysqlService.ComprovarRegistresActiusPerEtiqueta(etiquetaId);
                Console.WriteLine($"   • Registres actius després de l'esborrat: {registresActiusDesprés}");
            }
            else
            {
                Console.WriteLine("   • No hi ha registres actius per esborrar");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Exemple d'esborrat amb confirmació prèvia
        /// </summary>
        private static void ExempleEsborratAmbConfirmacio(MultiRDbService mysqlService, string etiquetaId)
        {
            Console.WriteLine("?? EXEMPLE D'ESBORRAT AMB CONFIRMACIÓ:");

            // Esborrat amb confirmació (forçant l'esborrat per l'exemple)
            int registresEsborrats = mysqlService.EsborrarRegistresAmbConfirmacio(etiquetaId, forcarEsborrat: true);

            if (registresEsborrats > 0)
            {
                Console.WriteLine($"   ? Esborrat completat: {registresEsborrats} registres afectats");
            }
            else
            {
                Console.WriteLine("   ?? No s'ha esborrat cap registre");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Exemple de restauració de registres esborrats
        /// </summary>
        private static void ExempleRestauracio(MultiRDbService mysqlService, string etiquetaId)
        {
            Console.WriteLine("?? EXEMPLE DE RESTAURACIÓ:");

            // Mostrar estat abans de la restauració
            var infoAbans = mysqlService.ObtenirInformacioRegistresEtiqueta(etiquetaId);
            Console.WriteLine($"   • Abans de restaurar: {infoAbans.RegistresEsborrats} registres esborrats");

            if (infoAbans.RegistresEsborrats > 0)
            {
                // Restaurar registres
                int registresRestaurats = mysqlService.RestaurarRegistresPerEtiqueta(etiquetaId);
                Console.WriteLine($"   • Registres restaurats: {registresRestaurats}");

                // Verificar estat després de la restauració
                var infoDesprés = mysqlService.ObtenirInformacioRegistresEtiqueta(etiquetaId);
                Console.WriteLine($"   • Després de restaurar: {infoDesprés.RegistresActius} registres actius");
            }
            else
            {
                Console.WriteLine("   • No hi ha registres esborrats per restaurar");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Exemple de gestió d'errors i casos extrems
        /// </summary>
        private static void ExempleGestioErrors(MultiRDbService mysqlService)
        {
            Console.WriteLine("?? EXEMPLE DE GESTIÓ D'ERRORS:");

            // Test amb etiqueta buida
            Console.WriteLine("   • Test amb etiqueta buida:");
            int resultat1 = mysqlService.EsborrarRegistresPerEtiqueta("");
            Console.WriteLine($"     Resultat: {resultat1} registres esborrats");

            // Test amb etiqueta null
            Console.WriteLine("   • Test amb etiqueta null:");
            int resultat2 = mysqlService.EsborrarRegistresPerEtiqueta(null);
            Console.WriteLine($"     Resultat: {resultat2} registres esborrats");

            // Test amb etiqueta inexistent
            Console.WriteLine("   • Test amb etiqueta inexistent:");
            int resultat3 = mysqlService.EsborrarRegistresPerEtiqueta("ETIQUETA_INEXISTENT_999999");
            Console.WriteLine($"     Resultat: {resultat3} registres esborrats");

            Console.WriteLine();
        }

        /// <summary>
        /// Exemple d'ús pràctic en un escenari real
        /// </summary>
        public static void ExempleEscenariReal(string etiquetaId, bool confirmarEsborrat = true)
        {
            Console.WriteLine($"?? ESCENARI REAL: Eliminació de l'etiqueta {etiquetaId}");

            var mysqlService = new MultiRDbService("connection_string");

            try
            {
                // 1. Obtenir informació prèvia
                var info = mysqlService.ObtenirInformacioRegistresEtiqueta(etiquetaId);
                
                if (!info.TeRegistresActius)
                {
                    Console.WriteLine("? No hi ha registres actius per aquesta etiqueta");
                    return;
                }

                Console.WriteLine($"?? Estat actual: {info}");

                // 2. Guardar historial abans de l'esborrat (si cal)
                bool historialGuardat = mysqlService.GuardarHistorialMostra(
                    etiquetaId, 
                    "ELIMINACIO_MANUAL", 
                    $"Eliminació manual de {info.RegistresActius} registres per l'etiqueta {etiquetaId}"
                );

                if (historialGuardat)
                {
                    Console.WriteLine("? Historial guardat abans de l'eliminació");
                }

                // 3. Procedir amb l'esborrat
                int registresEsborrats = mysqlService.EsborrarRegistresAmbConfirmacio(
                    etiquetaId, 
                    forcarEsborrat: !confirmarEsborrat
                );

                if (registresEsborrats > 0)
                {
                    Console.WriteLine($"? Eliminació completada: {registresEsborrats} registres esborrats");
                    
                    // 4. Verificar estat final
                    var infoFinal = mysqlService.ObtenirInformacioRegistresEtiqueta(etiquetaId);
                    Console.WriteLine($"?? Estat final: {infoFinal}");
                }
                else
                {
                    Console.WriteLine("? No s'ha pogut completar l'eliminació");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error en l'escenari real: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Test de rendiment per eliminar múltiples etiquetes
        /// </summary>
        public static void TestRendimentEliminacioMultiple(string[] etiquetes)
        {
            Console.WriteLine($"? TEST DE RENDIMENT: Eliminació de {etiquetes.Length} etiquetes");

            var mysqlService = new MultiRDbService("connection_string");
            var iniciTest = DateTime.Now;
            int totalRegistresEsborrats = 0;

            foreach (var etiqueta in etiquetes)
            {
                try
                {
                    int registres = mysqlService.EsborrarRegistresPerEtiqueta(etiqueta);
                    totalRegistresEsborrats += registres;
                    Console.WriteLine($"   ? {etiqueta}: {registres} registres");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ? {etiqueta}: Error - {ex.Message}");
                }
            }

            var duracio = DateTime.Now - iniciTest;
            Console.WriteLine($"\n?? Resum del test:");
            Console.WriteLine($"   • Etiquetes processades: {etiquetes.Length}");
            Console.WriteLine($"   • Total registres esborrats: {totalRegistresEsborrats}");
            Console.WriteLine($"   • Duració: {duracio.TotalSeconds:F2} segons");
            Console.WriteLine($"   • Mitjana per etiqueta: {duracio.TotalMilliseconds / etiquetes.Length:F1} ms");

            Console.WriteLine();
        }
    }
}
*/