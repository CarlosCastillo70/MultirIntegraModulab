using System;
using System.Collections.Generic;
using System.Linq;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Representa una mostra completa de laboratori, 
    /// que pot contenir múltiples resultats amb la mateixa ETIQUETA_ID
    /// </summary>
    public class Mostra
    {
        /// <summary>
        /// Identificador de la mostra (comú a tots els resultats d'aquesta mostra)
        /// </summary>
        public string EtiquetaId { get; set; }

        /// <summary>
        /// Identificador del pacient
        /// </summary>
        public string PacientSap { get; set; }

        /// <summary>
        /// Llista de resultats que pertanyen a aquesta mostra
        /// </summary>
        public List<ResultatMostra> Resultats { get; set; }

        /// <summary>
        /// Constructor per defecte
        /// </summary>
        public Mostra()
        {
            Resultats = new List<ResultatMostra>();
        }

        /// <summary>
        /// Constructor amb etiqueta i pacient
        /// </summary>
        public Mostra(string etiquetaId, string pacientSap)
        {
            EtiquetaId = etiquetaId;
            PacientSap = pacientSap;
            Resultats = new List<ResultatMostra>();
        }

        /// <summary>
        /// Afegeix un resultat a aquesta mostra
        /// </summary>
        public void AfegirResultat(ResultatMostra resultat)
        {
            if (resultat.EtiquetaId != EtiquetaId)
            {
                throw new ArgumentException($"El resultat amb EtiquetaId {resultat.EtiquetaId} no coincideix amb l'EtiquetaId de la mostra {EtiquetaId}");
            }
            Resultats.Add(resultat);
        }

        /// <summary>
        /// Afegeix un registre a aquesta mostra (obsolet, utilitzeu AfegirResulttat)
        /// </summary>
        [Obsolete("Utilitzeu AfegirResultat() en lloc d'aquest mètode")]
        public void AfegirRegistre(ResultatMostra registre)
        {
            AfegirResultat(registre);
        }

        /// <summary>
        /// Nombre de resultats en aquesta mostra
        /// </summary>
        public int NombreResultats => Resultats.Count;

        /// <summary>
        /// Nombre de registres en aquesta mostra (obsolet, utilitzeu NombreResultats)
        /// </summary>
        [Obsolete("Utilitzeu NombreResultats en lloc d'aquesta propietat")]
        public int NombreRegistres => NombreResultats;

        /// <summary>
        /// Data del primer resultat (més antic)
        /// </summary>
        public DateTime? DataPrimerResultat => Resultats.Any() ? Resultats.Min(r => r.DataResultat) : (DateTime?)null;

        /// <summary>
        /// Data de l'últim resultat (més recent)
        /// </summary>
        public DateTime? DataUltimResultat => Resultats.Any() ? Resultats.Max(r => r.DataResultat) : (DateTime?)null;

        /// <summary>
        /// Data de la petició (assumint que tots els resultats tenen la mateixa data de petició)
        /// </summary>
        public DateTime? DataPeticio => Resultats.FirstOrDefault()?.DataPeticioTrunc;

        /// <summary>
        /// CIP del pacient (assumint que tots els resultats tenen el mateix CIP)
        /// </summary>
        public string Cip => Resultats.FirstOrDefault()?.Cip;

        /// <summary>
        /// Nom del metge sol·licitant (primer resultat)
        /// </summary>
        public string NomMetge => Resultats.FirstOrDefault()?.NomMetge;

        /// <summary>
        /// Centre on s'ha fet la sol·licitud (primer resultat)
        /// </summary>
        public string CentreDescripcio => Resultats.FirstOrDefault()?.CentreDescripcio;

        /// <summary>
        /// Obté tots els serveis únics d'aquesta mostra
        /// </summary>
        public List<string> Serveis => Resultats.Where(r => !string.IsNullOrEmpty(r.ServeiDescripcio))
                                               .Select(r => r.ServeiDescripcio).Distinct().ToList();

        /// <summary>
        /// Indica si tots els resultats han estat validats
        /// </summary>
        public bool TotsResultatsValidats => Resultats.Any() && Resultats.All(r => r.EstaValidada);

        /// <summary>
        /// Indica si tots els registres han estat validats (obsolet, utilitzeu TotsResultatsValidats)
        /// </summary>
        [Obsolete("Utilitzeu TotsResultatsValidats en lloc d'aquesta propietat")]
        public bool TotsRegistresValidats => TotsResultatsValidats;

        /// <summary>
        /// Indica si algun dels resultats ha estat validat
        /// </summary>
        public bool AlgunResultatValidat => Resultats.Any(r => r.EstaValidada);

        /// <summary>
        /// Indica si algun dels registres ha estat validat (obsolet, utilitzeu AlgunResultatValidat)
        /// </summary>
        [Obsolete("Utilitzeu AlgunResultatValidat en lloc d'aquesta propietat")]
        public bool AlgunRegistreValidat => AlgunResultatValidat;

        /// <summary>
        /// Obté tots els microorganismes únics d'aquesta mostra
        /// </summary>
        public List<string> Microorganismes => Resultats.Where(r => !string.IsNullOrEmpty(r.AillamentDescripcio))
                                                        .Select(r => r.AillamentDescripcio).Distinct().ToList();

        /// <summary>
        /// Obté tots els tipus de prova únics d'aquesta mostra
        /// </summary>
        public List<string> TipusProves => Resultats.Where(r => !string.IsNullOrEmpty(r.ProvaDescripcio))
                                                   .Select(r => r.ProvaDescripcio).Distinct().ToList();

        /// <summary>
        /// Obté tots els mecanismes de resistència únics d'aquesta mostra
        /// </summary>
        public List<string> MecanismesResistencia
        {
            get
            {
                var mecanismes = new List<string>();
                foreach (var resultat in Resultats)
                {
                    if (!string.IsNullOrEmpty(resultat.MecanismeResistencia1Id)) mecanismes.Add(resultat.MecanismeResistencia1Id);
                    if (!string.IsNullOrEmpty(resultat.MecanismeResistencia2Id)) mecanismes.Add(resultat.MecanismeResistencia2Id);
                    if (!string.IsNullOrEmpty(resultat.MecanismeResistencia3Id)) mecanismes.Add(resultat.MecanismeResistencia3Id);
                    if (!string.IsNullOrEmpty(resultat.MecanismeResistencia4Id)) mecanismes.Add(resultat.MecanismeResistencia4Id);
                    if (!string.IsNullOrEmpty(resultat.MecanismeResistencia5Id)) mecanismes.Add(resultat.MecanismeResistencia5Id);
                }
                return mecanismes.Distinct().ToList();
            }
        }

        /// <summary>
        /// Obté totes les descripcions de mecanismes de resistència úniques d'aquesta mostra
        /// </summary>
        public List<string> DescripcionsMecanismesResistencia
        {
            get
            {
                var descripcions = new List<string>();
                foreach (var resultat in Resultats)
                {
                    if (!string.IsNullOrEmpty(resultat.MecanismeResistenciaDescrip)) descripcions.Add(resultat.MecanismeResistenciaDescrip);
                    if (!string.IsNullOrEmpty(resultat.MecanismeResistenciaDescrip2)) descripcions.Add(resultat.MecanismeResistenciaDescrip2);
                    if (!string.IsNullOrEmpty(resultat.MecanismeResistenciaDescrip3)) descripcions.Add(resultat.MecanismeResistenciaDescrip3);
                    if (!string.IsNullOrEmpty(resultat.MecanismeResistenciaDescrip4)) descripcions.Add(resultat.MecanismeResistenciaDescrip4);
                    if (!string.IsNullOrEmpty(resultat.MecanismeResistenciaDescrip5)) descripcions.Add(resultat.MecanismeResistenciaDescrip5);
                }
                return descripcions.Distinct().ToList();
            }
        }

        public override string ToString()
        {
            return $"Mostra - Etiqueta: {EtiquetaId}, Pacient: {PacientSap}, Metge: {NomMetge}, Centre: {CentreDescripcio}, Resultats: {NombreResultats}, Validat: {(TotsResultatsValidats ? "Sí" : "No")}";
        }
    }
}
