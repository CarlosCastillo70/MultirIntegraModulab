using System;
using System.Collections.Generic;
using MultirIntegraModulab.Domain.Entities;

namespace MultirIntegraModulab.Domain.Enums
{
    /// <summary>
    /// Estat d'un mecanisme de resistència
    /// </summary>
    public class EstatMecanisme
    {
        public string MecanismeCodi { get; set; }
        public bool Existeix { get; set; }
        public bool? IncorporaModulab { get; set; }

        public override string ToString()
        {
            return $"Mecanisme {MecanismeCodi}: Existeix={Existeix}, IncorporaModulab={IncorporaModulab}";
        }
    }

    /// <summary>
    /// Estat actual d'un resultat a la base de dades MySQL
    /// </summary>
    public class EstatResultat
    {
        public string EtiquetaId { get; set; }
        public DateTime? DataResultat { get; set; }
        public DateTime? DataValidacio { get; set; }
    }

    /// <summary>
    /// Tipus d'estat d'un resultat segons la comparació Oracle vs MySQL
    /// </summary>
    public enum TipusEstatResultat
    {
        Nova = 1,
        Antiga = 2,
        Repetida = 3,
        Desvalidada = 4,
        Validada = 5,
        Revalidada = 6,
        Canviada = 7
    }

    /// <summary>
    /// Informació sobre un mecanisme de resistència
    /// </summary>
    public class MecanismeResistenciaInfo
    {
        public string Id { get; set; }
        public string Descripcio { get; set; }
    }

    /// <summary>
    /// Combinació única de microorganisme i mecanisme de resistència
    /// </summary>
    public class CombinacioMicroorganismeMecanisme
    {
        public string Microorganisme { get; set; }
        public string MecanismeResistencia { get; set; }

        public CombinacioMicroorganismeMecanisme(string microorganisme, string mecanismeResistencia)
        {
            Microorganisme = microorganisme ?? "";
            MecanismeResistencia = mecanismeResistencia ?? "";
        }

        public override bool Equals(object obj)
        {
            if (obj is CombinacioMicroorganismeMecanisme other)
            {
                return Microorganisme == other.Microorganisme && 
                       MecanismeResistencia == other.MecanismeResistencia;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (Microorganisme + "|" + MecanismeResistencia).GetHashCode();
        }

        public override string ToString()
        {
            return $"{Microorganisme} ? {MecanismeResistencia}";
        }
    }

    /// <summary>
    /// Registre d'historial d'una mostra (adaptat a la nova estructura de la taula)
    /// </summary>
    public class RegistreHistorialMostra
    {
        public int Id { get; set; }
        public string EtiquetaOriginal { get; set; }
        public int Versio { get; set; }
        public string TipusCanvi { get; set; }
        public string CombinacionsAnteriors { get; set; }
        public DateTime? DataResultatAnterior { get; set; }
        public DateTime? DataValidacioAnterior { get; set; }
        public string CombinacionsNoves { get; set; }
        public DateTime? DataResultatNova { get; set; }
        public DateTime? DataValidacioNova { get; set; }
        public DateTime? DataCanvi { get; set; }
        public string ProcesOrigen { get; set; }
        
        // Propietats per compatibilitat amb l'antiga estructura
        public string EstatAbansCanvi { get; set; }
        public string PacientSap { get; set; }
        public string Microorganisme { get; set; }
        public string MecanismeResistencia { get; set; }
        public DateTime? DataResultatOriginal { get; set; }
        public DateTime? DataValidacioOriginal { get; set; }
        public string EstatIntegracioOriginal { get; set; }
        public string Observacions { get; set; }
        public DateTime DataCreacio { get; set; }

        /// <summary>
        /// Obté les combinacions anteriors deserialitzades
        /// </summary>
        public HashSet<CombinacioMicroorganismeMecanisme> ObtenirCombinacionsAnteriors()
        {
            return DeserialitzarCombinacions(CombinacionsAnteriors);
        }

        /// <summary>
        /// Obté les combinacions noves deserialitzades
        /// </summary>
        public HashSet<CombinacioMicroorganismeMecanisme> ObtenirCombinacionsNoves()
        {
            return DeserialitzarCombinacions(CombinacionsNoves);
        }

        /// <summary>
        /// Deserialitza text de combinacions
        /// </summary>
        private HashSet<CombinacioMicroorganismeMecanisme> DeserialitzarCombinacions(string combinacionsText)
        {
            var combinacions = new HashSet<CombinacioMicroorganismeMecanisme>();

            if (string.IsNullOrWhiteSpace(combinacionsText))
            {
                return combinacions;
            }

            try
            {
                var parts = combinacionsText.Split(';');
                foreach (var part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part)) continue;

                    var elements = part.Split('|');
                    if (elements.Length == 2)
                    {
                        combinacions.Add(new CombinacioMicroorganismeMecanisme(elements[0], elements[1]));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error deserialitzant combinacions: {ex.Message}");
            }

            return combinacions;
        }

        public override string ToString()
        {
            return $"Historial {Id}: {EtiquetaOriginal} v{Versio} - {TipusCanvi} ({DataCanvi:dd/MM/yyyy HH:mm})";
        }
    }

    /// <summary>
    /// Estadístiques de l'historial de mostres
    /// </summary>
    public class EstadistiquesHistorial
    {
        public int TotalRegistresHistorial { get; set; }
        public Dictionary<string, int> RegistresPerTipus { get; set; }
        public DateTime? PrimerRegistre { get; set; }
        public DateTime? UltimRegistre { get; set; }

        public EstadistiquesHistorial()
        {
            RegistresPerTipus = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// Informació detallada sobre els registres d'una etiqueta
    /// </summary>
    public class InformacioRegistresEtiqueta
    {
        public string EtiquetaId { get; set; }
        public int TotalRegistres { get; set; }
        public int RegistresActius { get; set; }
        public int RegistresEsborrats { get; set; }
        public DateTime? PrimerRegistre { get; set; }
        public DateTime? UltimRegistre { get; set; }
        public DateTime? UltimaEliminacio { get; set; }
    }

    /// <summary>
    /// Resum del tractament de mostres processades
    /// </summary>
    public class ResumTractament
    {
        public int NovesIncorporacions { get; set; }
        public int MostresAntiques { get; set; }
        public int MostresRepetides { get; set; }
        public int MostresDesvalidates { get; set; }
        public int MostresValides { get; set; }
        public int MostresRevalides { get; set; }
        public int MostresAmbCanvis { get; set; }
        public int MostresInvalides { get; set; }
        public int ResultatsInvalids { get; set; }
        public int MostresAmbError { get; set; }
        
        public int TotalProcessats => NovesIncorporacions + MostresAntiques + MostresRepetides + 
                                      MostresDesvalidates + MostresValides + MostresRevalides + 
                                      MostresAmbCanvis + MostresInvalides + MostresAmbError;
    }

    /// <summary>
    /// Context de processament per una mostra
    /// </summary>
    public class ContextProcessament
    {
        public Mostra Mostra { get; set; }
        public MultiRDbService MultiRService { get; set; }
        public TipusIncorporacio TipusIncorporacio { get; set; }
        public Dictionary<string, object> DadesAddicionals { get; set; }

        public ContextProcessament(Mostra mostra, MultiRDbService multiRService)
        {
            Mostra = mostra;
            MultiRService = multiRService;
            DadesAddicionals = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Resultat d'una acció de processament
    /// </summary>
    public class ResultatAccio
    {
        public bool Exitosa { get; set; }
        public bool ContinuarProcessament { get; set; }
        public string Missatge { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>
    /// Tipus d'incorporació d'una mostra
    /// </summary>
    public enum TipusIncorporacio
    {
        Nova,
        Antiga,
        Repetida,
        Desvalidada,
        Validada,
        Revalidada
    }

    /// <summary>
    /// Tipus de microorganisme segons la seva naturalesa
    /// </summary>
    public enum TipusMicroorganisme
    {
        /// <summary>
        /// Microorganisme multiresistent (MMR)
        /// Camp tipus = 'M' a la taula microorganismes
        /// Pot tenir mecanismes de resistència (1-5)
        /// </summary>
        Multiresistent = 0,
        
        /// <summary>
        /// Virus respiratori (VR)
        /// Camp tipus = 'R' a la taula microorganismes
        /// No té mecanismes de resistència
        /// Sempre s'incorpora
        /// </summary>
        VirusRespiratori = 1,
        
        /// <summary>
        /// Mostra mixta (MMR + VR)
        /// Conté tant microorganismes multiresistents com virus respiratoris
        /// Es processa en dues parts: primer MMR, després VR
        /// </summary>
        Mixt = 2
    }

    /// <summary>
    /// Tipus de mostra segons els seus resultats
    /// </summary>
    public enum TipusMostra
    {
        UnSolResultatPositiu,
        UnSolResultatNegatiu,
        MultiplesResultatsTotsPositius,
        MultiplesResultatsTotsNegatius,
        MultiplesResultatsPositiusINegatius
    }

    /// <summary>
    /// Classificació d'una mostra amb els seus resultats positius i negatius
    /// </summary>
    public class ClassificacioMostra
    {
        public string EtiquetaId { get; set; }
        public int NombreRegistres { get; set; }
        public TipusMostra TipusMostra { get; set; }
        public List<ResultatClassificat> ResultatsPositius { get; set; }
        public List<ResultatClassificat> ResultatsNegatius { get; set; }

        public ClassificacioMostra()
        {
            ResultatsPositius = new List<ResultatClassificat>();
            ResultatsNegatius = new List<ResultatClassificat>();
        }
    }

    /// <summary>
    /// Resultat classificat com a positiu o negatiu
    /// </summary>
    public class ResultatClassificat
    {
        public ResultatMostra Registre { get; set; }
        public bool TeMicroorganisme { get; set; }
        public string MicroorganismeDescripcio { get; set; }
        public bool? EsMicroorganismeEspecial { get; set; }
        public bool TeMecanismesResistencia { get; set; }
        public List<string> MecanismesResistencia { get; set; }
        public bool EsPositiu { get; set; }
        public string MotiuPositiu { get; set; }
        public string MotiuNegatiu { get; set; }

        public ResultatClassificat()
        {
            MecanismesResistencia = new List<string>();
        }
    }
}