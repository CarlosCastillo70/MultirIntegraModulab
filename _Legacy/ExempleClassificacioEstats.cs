using System;
using System.Collections.Generic;
using MultirIntegraModulab.Domain.Enums;

namespace MultirIntegraModulab
{
    /// <summary>
    /// Exemple de la nova lògica de classificació detallada d'estats de resultats
    /// </summary>
    public class ExempleClassificacioEstats
    {
        /// <summary>
        /// Demostra els diferents tipus d'estat que pot tenir un resultat
        /// </summary>
        public static void DemostrarClassificacioEstats(MultiRDbService mysqlService)
        {
            Console.WriteLine("?? DEMOSTRACIÓ DE CLASSIFICACIÓ D'ESTATS DE RESULTATS\n");

            // Casos d'exemple per demostrar cada tipus d'estat
            var casosTest = new List<CasTest>
            {
                new CasTest
                {
                    EtiquetaId = "400000001",
                    Descripcio = "Nova incorporació",
                    DataResultatOracle = DateTime.Now.AddDays(-1),
                    DataValidacioOracle = DateTime.Now,
                    EstatEsperat = TipusEstatResultat.Nova
                },
                new CasTest
                {
                    EtiquetaId = "400000002", 
                    Descripcio = "Resultat antic sense dates",
                    DataResultatOracle = DateTime.Now.AddDays(-1),
                    DataValidacioOracle = null,
                    EstatEsperat = TipusEstatResultat.Antiga
                },
                new CasTest
                {
                    EtiquetaId = "400000003",
                    Descripcio = "Resultat repetit (dates idèntiques)",
                    DataResultatOracle = new DateTime(2024, 1, 15, 10, 30, 0),
                    DataValidacioOracle = new DateTime(2024, 1, 15, 14, 45, 0),
                    EstatEsperat = TipusEstatResultat.Repetida
                },
                new CasTest
                {
                    EtiquetaId = "400000004",
                    Descripcio = "Validació posterior",
                    DataResultatOracle = new DateTime(2024, 1, 15, 10, 30, 0),
                    DataValidacioOracle = new DateTime(2024, 1, 16, 9, 15, 0),
                    EstatEsperat = TipusEstatResultat.Validada
                }
            };

            foreach (var cas in casosTest)
            {
                Console.WriteLine($"?? Test: {cas.Descripcio}");
                Console.WriteLine($"   - Etiqueta: {cas.EtiquetaId}");
                Console.WriteLine($"   - Oracle - Data resultat: {cas.DataResultatOracle?.ToString("dd/MM/yyyy HH:mm") ?? "NULL"}");
                Console.WriteLine($"   - Oracle - Data validació: {cas.DataValidacioOracle?.ToString("dd/MM/yyyy HH:mm") ?? "NULL"}");
                
                try
                {
                    // Classificar l'estat
                    var tipusEstat = mysqlService.ClassificarEstatResultat(
                        cas.EtiquetaId,
                        cas.DataResultatOracle,
                        cas.DataValidacioOracle);

                    Console.WriteLine($"   - Estat detectat: {tipusEstat}");
                    Console.WriteLine($"   - Resultat: {(tipusEstat == cas.EstatEsperat ? "? CORRECTE" : "? INCORRECTE")}");
                    
                    // Mostrar què significa cada estat
                    MostrarSignificatEstat(tipusEstat);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   - ? Error: {ex.Message}");
                }
                
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Explica què significa cada tipus d'estat
        /// </summary>
        private static void MostrarSignificatEstat(TipusEstatResultat tipus)
        {
            var descripcions = new Dictionary<TipusEstatResultat, string>
            {
                [TipusEstatResultat.Nova] = "No existeix a MySQL - primera vegada que arriba",
                [TipusEstatResultat.Antiga] = "Existeix a MySQL però sense dates - cal actualitzar",
                [TipusEstatResultat.Repetida] = "Dates idèntiques - no cal fer res",
                [TipusEstatResultat.Desvalidada] = "MySQL validat però Oracle no - possible problema",
                [TipusEstatResultat.Validada] = "Ara està validat a Oracle - cal actualitzar MySQL",
                [TipusEstatResultat.Revalidada] = "Dates de validació diferents - cal actualitzar",
                [TipusEstatResultat.Canviada] = "Altres canvis detectats - cal revisar"
            };

            if (descripcions.ContainsKey(tipus))
            {
                Console.WriteLine($"   - Significat: {descripcions[tipus]}");
            }
        }

        /// <summary>
        /// Mostra un resum de tots els possibles estats
        /// </summary>
        public static void MostrarResumEstats()
        {
            Console.WriteLine("?? RESUM DELS ESTATS POSSIBLES:\n");

            Console.WriteLine("?? NOVA");
            Console.WriteLine("   - No existeix cap registre per l'etiqueta a pacients_diagnostics_mostra");
            Console.WriteLine("   - Acció: Inserir nou registre\n");

            Console.WriteLine("??? ANTIGA");
            Console.WriteLine("   - Existeixen registres però no tenen ni data_resultat ni data_validacio");
            Console.WriteLine("   - Acció: UPDATE amb dates d'Oracle\n");

            Console.WriteLine("?? REPETIDA");
            Console.WriteLine("   - Les data_resultat i data_validacio coincideixen exactament");
            Console.WriteLine("   - Acció: Cap (saltar processament)\n");

            Console.WriteLine("? DESVALIDADA");
            Console.WriteLine("   - MySQL té data_resultat i data_validacio, però Oracle no té data_validacio");
            Console.WriteLine("   - Acció: Revisar - possible inconsistència\n");

            Console.WriteLine("? VALIDADA");
            Console.WriteLine("   - MySQL no té data_validacio però Oracle sí");
            Console.WriteLine("   - Acció: UPDATE data_validacio i estat_integracio_m = 'V'\n");

            Console.WriteLine("?? REVALIDADA");
            Console.WriteLine("   - Ambdós tenen data_validacio però són diferents");
            Console.WriteLine("   - Acció: UPDATE amb nova data_validacio d'Oracle\n");

            Console.WriteLine("?? CANVIADA");
            Console.WriteLine("   - Altres canvis que no encaixen en les categories anteriors");
            Console.WriteLine("   - Acció: Revisar i decidir actualització");
        }

        /// <summary>
        /// Test complet amb diferents escenaris reals
        /// </summary>
        public static void TestCompletClassificacio(MultiRDbService mysqlService)
        {
            Console.WriteLine("?? TEST COMPLET DE CLASSIFICACIÓ D'ESTATS\n");

            var etiquetesTest = new string[] 
            { 
                "400816071", // L'etiqueta del debugger
                "400000001", 
                "400000002", 
                "400000003" 
            };

            foreach (var etiqueta in etiquetesTest)
            {
                Console.WriteLine($"?? Analitzant etiqueta: {etiqueta}");

                try
                {
                    // Comprovar si existeix
                    int count = mysqlService.ComprovarResultatExisteix(etiqueta);
                    Console.WriteLine($"   • Registres existents: {count}");

                    if (count > 0)
                    {
                        // Obtenir estat actual
                        var estat = mysqlService.ObtenirEstatResultat(etiqueta);
                        if (estat != null)
                        {
                            Console.WriteLine($"   • Estat MySQL: {estat}");

                            // Simular dades d'Oracle per fer la classificació
                            DateTime? dataResultatOracle = DateTime.Now.AddDays(-1);
                            DateTime? dataValidacioOracle = DateTime.Now;

                            var tipusEstat = mysqlService.ClassificarEstatResultat(
                                etiqueta, 
                                dataResultatOracle, 
                                dataValidacioOracle);

                            Console.WriteLine($"   • Classificació: {tipusEstat}");
                            MostrarSignificatEstat(tipusEstat);
                        }
                        else
                        {
                            Console.WriteLine($"   • ?? Error obtenint estat de MySQL");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   • Estat: NOVA (no existeix a MySQL)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   • ? Error: {ex.Message}");
                }

                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Cas de test per la classificació d'estats
    /// </summary>
    public class CasTest
    {
        public string EtiquetaId { get; set; }
        public string Descripcio { get; set; }
        public DateTime? DataResultatOracle { get; set; }
        public DateTime? DataValidacioOracle { get; set; }
        public TipusEstatResultat EstatEsperat { get; set; }
    }
}