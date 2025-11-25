using System;
using System.Collections.Generic;
using System.Linq;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Col·lecció de mostres de laboratori
    /// Facilita la gestió i consulta de múltiples mostres
    /// </summary>
    public class ColeccioMostres
    {
        private readonly Dictionary<string, Mostra> _mostres;

        /// <summary>
        /// Constructor per defecte
        /// </summary>
        public ColeccioMostres()
        {
            _mostres = new Dictionary<string, Mostra>();
        }

        /// <summary>
        /// Afegeix un resultat a la col·lecció. 
        /// Si ja existeix una mostra amb la mateixa EtiquetaId, s'afegeix el resultat a aquesta mostra.
        /// Si no existeix, es crea una nova mostra.
        /// </summary>
        public void AfegirResultat(ResultatMostra resultat)
        {
            if (_mostres.ContainsKey(resultat.EtiquetaId))
            {
                _mostres[resultat.EtiquetaId].AfegirResultat(resultat);
            }
            else
            {
                var novaMostra = new Mostra(resultat.EtiquetaId, resultat.PacientSap);
                novaMostra.AfegirResultat(resultat);
                _mostres[resultat.EtiquetaId] = novaMostra;
            }
        }

        /// <summary>
        /// Afegeix un registre a la col·lecció (obsolet, utilitzeu AfegirResultat)
        /// </summary>
        [Obsolete("Utilitzeu AfegirResultat() en lloc d'aquest mètode")]
        public void AfegirRegistre(ResultatMostra registre)
        {
            AfegirResultat(registre);
        }

        /// <summary>
        /// Obté una mostra per EtiquetaId
        /// </summary>
        public Mostra ObtenirMostra(string etiquetaId)
        {
            _mostres.TryGetValue(etiquetaId, out var mostra);
            return mostra;
        }

        /// <summary>
        /// Obté totes les mostres
        /// </summary>
        public List<Mostra> ObtenirTotesLesMostres()
        {
            return _mostres.Values.ToList();
        }

        /// <summary>
        /// Obté mostres per pacient
        /// </summary>
        public List<Mostra> ObtenirMostresPerPacient(string pacientSap)
        {
            return _mostres.Values.Where(m => m.PacientSap == pacientSap).ToList();
        }

        /// <summary>
        /// Obté mostres per CIP de pacient
        /// </summary>
        public List<Mostra> ObtenirMostresPerCip(string cip)
        {
            return _mostres.Values.Where(m => m.Cip == cip).ToList();
        }

        /// <summary>
        /// Obté mostres per metge sol·licitant
        /// </summary>
        public List<Mostra> ObtenirMostresPerMetge(string nomMetge)
        {
            return _mostres.Values.Where(m => m.NomMetge == nomMetge).ToList();
        }

        /// <summary>
        /// Obté mostres per centre
        /// </summary>
        public List<Mostra> ObtenirMostresPerCentre(string centreDescripcio)
        {
            return _mostres.Values.Where(m => m.CentreDescripcio == centreDescripcio).ToList();
        }

        /// <summary>
        /// Obté mostres per servei
        /// </summary>
        public List<Mostra> ObtenirMostresPerServei(string serveiDescripcio)
        {
            return _mostres.Values.Where(m => m.Serveis.Any(s => s.ToLowerInvariant().Contains(serveiDescripcio.ToLowerInvariant()))).ToList();
        }

        /// <summary>
        /// Obté mostres per rang de dates de resultat
        /// </summary>
        public List<Mostra> ObtenirMostresPerDataResultat(DateTime dataInici, DateTime dataFi)
        {
            return _mostres.Values.Where(m => m.DataPrimerResultat >= dataInici && m.DataUltimResultat <= dataFi).ToList();
        }

        /// <summary>
        /// Obté mostres per rang de dates de petició
        /// </summary>
        public List<Mostra> ObtenirMostresPerDataPeticio(DateTime dataInici, DateTime dataFi)
        {
            return _mostres.Values.Where(m => m.DataPeticio.HasValue && 
                                                m.DataPeticio.Value >= dataInici && 
                                                m.DataPeticio.Value <= dataFi).ToList();
        }

        /// <summary>
        /// Obté mostres que contenen un microorganisme específic
        /// </summary>
        public List<Mostra> ObtenirMostresPerMicroorganisme(string microorganisme)
        {
            return _mostres.Values.Where(m => m.Microorganismes.Any(micro => micro.ToLowerInvariant().Contains(microorganisme.ToLowerInvariant()))).ToList();
        }

        /// <summary>
        /// Obté mostres que tenen un mecanisme de resistència específic
        /// </summary>
        public List<Mostra> ObtenirMostresPerMecanismeResistencia(string mecanismeId)
        {
            return _mostres.Values.Where(m => m.MecanismesResistencia.Contains(mecanismeId)).ToList();
        }

        /// <summary>
        /// Obté mostres que tenen una descripció de mecanisme de resistència específica
        /// </summary>
        public List<Mostra> ObtenirMostresPerDescripcioMecanismeResistencia(string descripcio)
        {
            return _mostres.Values.Where(m => m.DescripcionsMecanismesResistencia.Any(d => d.ToLowerInvariant().Contains(descripcio.ToLowerInvariant()))).ToList();
        }

        /// <summary>
        /// Obté mostres validades
        /// </summary>
        public List<Mostra> ObtenirMostresValides()
        {
            return _mostres.Values.Where(m => m.TotsResultatsValidats).ToList();
        }

        /// <summary>
        /// Obté mostres validats (alias per compatibilitat)
        /// </summary>
        public List<Mostra> ObtenirMostresValidades()
        {
            return _mostres.Values.Where(m => m.TotsResultatsValidats).ToList();
        }

        /// <summary>
        /// Obté mostres pendents de validació
        /// </summary>
        public List<Mostra> ObtenirMostresPendentsValidacio()
        {
            return _mostres.Values.Where(m => !m.TotsResultatsValidats).ToList();
        }

        /// <summary>
        /// Nombre total de mostres
        /// </summary>
        public int NombreTotalMostres => _mostres.Count;

        /// <summary>
        /// Nombre total de resultats (suma de tots els resultats de totes les mostres)
        /// </summary>
        public int NombreTotalResultats => _mostres.Values.Sum(m => m.NombreResultats);

        /// <summary>
        /// Nombre total de registres (obsolet, utilitzeu NombreTotalResultats)
        /// </summary>
        [Obsolete("Utilitzeu NombreTotalResultats en lloc d'aquesta propietat")]
        public int NombreTotalRegistres => NombreTotalResultats;

        /// <summary>
        /// Neteja totes les mostres
        /// </summary>
        public void Netejar()
        {
            _mostres.Clear();
        }

        /// <summary>
        /// Comprova si existeix una mostra amb una EtiquetaId específica
        /// </summary>
        public bool Existeix(string etiquetaId)
        {
            return _mostres.ContainsKey(etiquetaId);
        }

        /// <summary>
        /// Obté la data de resultat màxima de totes les mostres
        /// Útil per al sistema de sincronització optimitzat
        /// </summary>
        /// <returns>Data resultat màxima o null si no hi ha mostres</returns>
        public DateTime? ObtenirDataResultatMaxima()
        {
            if (_mostres.Count == 0)
                return null;

            return _mostres.Values
                .SelectMany(m => m.Resultats)
                .Where(r => r.DataResultat != default(DateTime))
                .Select(r => r.DataResultat)
                .DefaultIfEmpty()
                .Max();
        }

        /// <summary>
        /// Obté la data de validació màxima de totes les mostres
        /// Útil per al sistema de sincronització optimitzat
        /// </summary>
        /// <returns>Data validació màxima o null si no hi ha validacions</returns>
        public DateTime? ObtenirDataValidacioMaxima()
        {
            if (_mostres.Count == 0)
                return null;

            return _mostres.Values
                .SelectMany(m => m.Resultats)
                .Where(r => r.DataValidacio.HasValue)
                .Select(r => r.DataValidacio.Value)
                .DefaultIfEmpty()
                .Max();
        }
    }
}
