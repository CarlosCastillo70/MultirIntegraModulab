using System;
using System.Collections.Generic;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Enums;

namespace MultirIntegraModulab.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implementació del repositori per accedir a dades de MultiR (MySQL)
    /// Aquesta classe adapta MultiRDbService a la interfície del domini
    /// </summary>
    public class MultiRRepository : IMultiRRepository
    {
        private readonly MultiRDbService _multiRDbService;
        private readonly ILoggerService _logger;

        public MultiRRepository(MultiRDbService multiRDbService, ILoggerService logger)
        {
            _multiRDbService = multiRDbService ?? throw new ArgumentNullException(nameof(multiRDbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Mètodes del sistema
        public DateTime GetCurrentDate() => (DateTime)_multiRDbService.GetCurrentDate();
        public string GetDatabaseType() => _multiRDbService.GetDatabaseType();
        public int GetTableRecordCount(string tableName) => _multiRDbService.GetTableRecordCount(tableName);

        // Pacients
        public bool ExisteixPacient(string pacientSap) => 
            _multiRDbService.ExisteixPacient(pacientSap);

        public bool InserirPacient(DadesPacient dadesPacient) => 
            _multiRDbService.InserirPacient(dadesPacient);

        // Diagnòstics
        public int ComprovarDiagnosticExisteix(string pacientSap, string microorganisme, string mecanisme, string tipusMecanisme) =>
            _multiRDbService.ComprovarDiagnosticExisteix(pacientSap, microorganisme, mecanisme, tipusMecanisme);

        public int CrearDiagnosticPacient(string pacientSap, string microorganisme, string mecanisme, string tipusMecanisme) =>
            _multiRDbService.CrearDiagnosticPacient(pacientSap, microorganisme, mecanisme, tipusMecanisme);

        public List<int> ObtenirDiagnosticsPositiusPacientPerTipusMostra(string pacientSap, string tipusMostra, string etiquetaExcloure = null, string microorganisme = null, string mecanisme = null) =>
            _multiRDbService.ObtenirDiagnosticsPositiusPacientPerTipusMostra(pacientSap, tipusMostra, etiquetaExcloure, microorganisme, mecanisme);

        public List<int> ObtenirDiagnosticsPositiusPacientPerTipusMostraIEquivalents(string pacientSap, string tipusMostra, string etiquetaExcloure = null, string microorganisme = null, string mecanisme = null) =>
            _multiRDbService.ObtenirDiagnosticsPositiusPacientPerTipusMostraIEquivalents(pacientSap, tipusMostra, etiquetaExcloure, microorganisme, mecanisme);


        public List<int> ObtenirDiagnosticsPositiusPacientAlgunTipusMostra(string pacientSap, string etiqueta = null) =>
            _multiRDbService.ObtenirDiagnosticsPositiusPacientAlgunTipusMostra(pacientSap, etiqueta);

        //public List<int> ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents(string pacientSap, string tipusMostra, string etiqueta = null) =>
        //    _multiRDbService.ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents(pacientSap, tipusMostra, etiqueta);

        public DiagnosticInfo ObtenirInformDiagnostic(int diagnosticId) =>
            _multiRDbService.ObtenirInformDiagnostic(diagnosticId);

        public bool DiagnosticTeMostraAmbEtiqueta(int diagnosticId, string etiqueta, string tipusMostra) =>
            _multiRDbService.DiagnosticTeMostraAmbEtiqueta(diagnosticId, etiqueta, tipusMostra);

        // Mostres diagnòstic
        public int ComprovarMostraDiagnosticExisteix(string pacientSap, DateTime? dataMostra, string tipusMostra, string valoracio = null, string etiqueta = null) => 
            _multiRDbService.ComprovarMostraDiagnosticExisteix(pacientSap, dataMostra, tipusMostra, valoracio, etiqueta);

        public int ComprovarMostraDiagnosticPerEtiqueta(string pacientSap, string tipusMostra, string valoracio, string etiqueta) =>
            _multiRDbService.ComprovarMostraDiagnosticPerEtiqueta(pacientSap, tipusMostra, valoracio, etiqueta);

        public int CrearMostraDiagnostic(string pacientSap, DateTime? dataMostra, string tipusMostra, 
            string tipusProva, string etiqueta, DateTime? dataResultat, DateTime? dataValidacio, 
            string mecanismeId, bool? esMicroorganismeEspecial, string microorganismeMecanismeCaptat) => 
            _multiRDbService.CrearMostraDiagnostic(pacientSap, dataMostra, tipusMostra, tipusProva, etiqueta, 
                dataResultat, dataValidacio, mecanismeId, esMicroorganismeEspecial, microorganismeMecanismeCaptat);

        public bool ActualitzarMicroorganismeMecanismeCaptat(int mostraDiagnosticId, string nouMicroorganismeMecanisme) =>
            _multiRDbService.ActualitzarMicroorganismeMecanismeCaptat(mostraDiagnosticId, nouMicroorganismeMecanisme);

        public bool EsborrarDadesMostra(string etiquetaId) => 
            _multiRDbService.EsborrarDadesMostra(etiquetaId);

        public MostraDiagnosticExistent ObtenirMostraDiagnostic(string etiquetaId) =>
            _multiRDbService.ObtenirMostraDiagnostic(etiquetaId);

        public ResultatComparacioMostres CompararMostres(MostraDiagnosticExistent mostraExistent, Mostra mostraEntrant) =>
            _multiRDbService.CompararMostres(mostraExistent, mostraEntrant);

        public List<CombinacioMicroorganismeMecanisme> ObtenirCombinacionsMicroorganismeMecanisme(string etiquetaId) =>
            _multiRDbService.ObtenirCombinacionsMicroorganismeMecanisme(etiquetaId);

        public List<CombinacioMicroorganismeMecanisme> ObtenirCombinacionsMostraEntrant(Mostra mostra) =>
            _multiRDbService.ObtenirCombinacionsMostraEntrant(mostra);

        // Microorganismes
        public bool? EsMicroorganismeEspecial(string microorganismeDescripcio) => 
            _multiRDbService.EsMicroorganismeEspecial(microorganismeDescripcio);

        public Microorganisme ObtenirMicroorganisme(string descripcio) => 
            _multiRDbService.ObtenirMicroorganisme(descripcio);

        public List<Microorganisme> ObtenirMicroorganismesEspecials() => 
            _multiRDbService.ObtenirMicroorganismesEspecials();

        public void CarregarMicroorganismesEspecials() => 
            _multiRDbService.CarregarMicroorganismesEspecials();

        public void NetejarCacheMicroorganismes() => 
            _multiRDbService.NetejarCacheMicroorganismes();

        public string ObtenirEstadistiquesCache() => 
            _multiRDbService.ObtenirEstadistiquesCache();

        public bool ComprovarICrearMicroorganisme(string microorganismeDescripcio) => 
            _multiRDbService.ComprovarICrearMicroorganisme(microorganismeDescripcio);

        public TipusMicroorganisme ObtenirTipusMicroorganisme(string microorganismeDescripcio) =>
            _multiRDbService.ObtenirTipusMicroorganisme(microorganismeDescripcio);

        // Mecanismes de resistència
        public EstatMecanisme ComprovarExistenciaMecanisme(string mecanismeCodi) => 
            _multiRDbService.ComprovarExistenciaMecanisme(mecanismeCodi);

        public bool CrearMecanisme(string mecanismeCodi, string mecanismeDescripcio) => 
            _multiRDbService.CrearMecanisme(mecanismeCodi, mecanismeDescripcio);

        // Resultats
        public int ComprovarResultatExisteix(string etiquetaId) => 
            _multiRDbService.ComprovarResultatExisteix(etiquetaId);

        public EstatResultat ObtenirEstatResultat(string etiquetaId) => 
            _multiRDbService.ObtenirEstatResultat(etiquetaId);

        public TipusEstatResultat ClassificarEstatResultat(string etiquetaId, DateTime? dataResultatOracle, DateTime? dataValidacioOracle) => 
            _multiRDbService.ClassificarEstatResultat(etiquetaId, dataResultatOracle, dataValidacioOracle);

        public bool ActualitzarResultatAmbNovesDates(string etiquetaId, DateTime? dataResultat, DateTime? dataValidacio) => 
            _multiRDbService.ActualitzarResultatAmbNovesDates(etiquetaId, dataResultat, dataValidacio);

        public bool ActualitzarResultatAntic(string etiquetaId, DateTime dataResultat, DateTime? dataValidacio) => 
            _multiRDbService.ActualitzarResultatAntic(etiquetaId, dataResultat, dataValidacio);

        public bool ActualitzarDataValidacio(string etiquetaId, DateTime? dataValidacio) => 
            _multiRDbService.ActualitzarDataValidacio(etiquetaId, dataValidacio);

        // Mostres i microorganismes
        public bool InserirMostraMicroorganisme(string etiquetaId, string microorganismeDescripcio) => 
            _multiRDbService.InserirMostraMicroorganisme(etiquetaId, microorganismeDescripcio);

        public bool ComprovarMostraMicroorganismeExisteix(int diagnosticId, int mostraDiagnosticId) => 
            _multiRDbService.ComprovarMostraMicroorganismeExisteix(diagnosticId, mostraDiagnosticId);

        public bool CrearMostraMicroorganisme(int diagnosticId, int mostraDiagnosticId) => 
            _multiRDbService.CrearMostraMicroorganisme(diagnosticId, mostraDiagnosticId);

        public bool EsCombinacioNoIncorporar(string microorganisme, string mecanisme) => 
            _multiRDbService.EsCombinacioNoIncorporar(microorganisme, mecanisme);

        public bool ActualitzarDataDiagnosticPacientsDiagnostics(
            string pacientSap, 
            string microorganismeCodi, 
            string mecanismeId, 
            string tipusMecanisme) =>
            _multiRDbService.ActualitzarDataDiagnosticPacientsDiagnostics(
                pacientSap, 
                microorganismeCodi, 
                mecanismeId, 
                tipusMecanisme);

        public bool ActualitzarDataDiagnosticPacientsDiagnosticsMostra(
            string pacientSap, 
            string microorganismeCodi, 
            string mecanismeId, 
            string tipusMecanisme) =>
            _multiRDbService.ActualitzarDataDiagnosticPacientsDiagnosticsMostra(
                pacientSap, 
                microorganismeCodi, 
                mecanismeId, 
                tipusMecanisme);

        // Tipus de mostra
        public bool ExisteixTipusMostraMactiu(string codiMostra) =>
            _multiRDbService.ExisteixTipusMostraMactiu(codiMostra);

        public bool CrearTipusMostraM(string codiMostra) =>
            _multiRDbService.CrearTipusMostraM(codiMostra);

        public int? ObtenirComportamentTipusMostra(string codiMostra) =>
            _multiRDbService.ObtenirComportamentTipusMostra(codiMostra);

        public bool PacientTePositiusAlgunTipusMostra(string pacientSap) =>
            _multiRDbService.PacientTePositiusAlgunTipusMostra(pacientSap);

        public bool PacientTePositiusVigentsTipusMostraIEquivalents(
            string pacientSap, 
            string tipusMostra, 
            string etiquetaExcloure = null,
            DateTime? dataResultatExcloure = null)
        {
            return _multiRDbService.PacientTePositiusVigentsTipusMostraIEquivalents(
                pacientSap, 
                tipusMostra, 
                etiquetaExcloure,
                dataResultatExcloure);
        }

        // Tipus de prova
        public bool ExisteixTipusProvaActiu(string codiProva) =>
            _multiRDbService.ExisteixTipusProvaActiu(codiProva);

        public bool CrearTipusProva(string codiProva) =>
            _multiRDbService.CrearTipusProva(codiProva);

        public bool TipusProvaPermitIncorporarVirusRespiratori(string codiProva) =>
            _multiRDbService.TipusProvaPermitIncorporarVirusRespiratori(codiProva);

        public bool TipusProvaEsMDO(string codiProva, string shortDescription1) =>
            _multiRDbService.TipusProvaEsMDO(codiProva, shortDescription1);

        #region Paràmetres d'Aplicació

        public bool ExisteixParametre(string categoria, string valor) =>
            _multiRDbService.ExisteixParametre(categoria, valor);

        public string ObtenirParametre(string categoria, string clau) =>
            _multiRDbService.ObtenirParametre(categoria, clau);

        public List<string> ObtenirParametresPerCategoria(string categoria) =>
            _multiRDbService.ObtenirParametresPerCategoria(categoria);

        public List<string> ObtenirValorsPerClau(string clau) =>
            _multiRDbService.ObtenirValorsPerClau(clau);

        #endregion

        // Integració
        public bool InserirIntegracioResultats(string etiquetaId, ResultatMostra registre, string mecanismeId, 
            string estat, string observacions, bool incorporaModulab) => 
            _multiRDbService.InserirIntegracioResultats(etiquetaId, registre, mecanismeId, estat, observacions, incorporaModulab);

        public bool InserirAuditoriaIntegracioModulab(Mostra mostra, string codiResultat, ResultatMostra resultatMostra = null, MecanismeResistenciaInfo mecanisme = null) => 
            _multiRDbService.InserirAuditoriaIntegracioModulab(mostra, codiResultat, resultatMostra, mecanisme);

        // Historial
        public EstadistiquesHistorial ObtenirEstadistiquesHistorial() => 
            _multiRDbService.ObtenirEstadistiquesHistorial();

        public int ComprovarHistorialExisteix(string etiquetaId) => 
            _multiRDbService.ComprovarHistorialExisteix(etiquetaId);

        public List<RegistreHistorialMostra> ObtenirHistorialMostra(string etiquetaId) => 
            _multiRDbService.ObtenirHistorialMostra(etiquetaId);

        public bool GuardarHistorialMostra(
            string etiquetaId, 
            string tipusCanvi, 
            string combinacionsAnteriors = null,
            DateTime? dataResultatAnterior = null,
            DateTime? dataValidacioAnterior = null,
            string combinacionsNoves = null,
            DateTime? dataResultatNova = null,
            DateTime? dataValidacioNova = null,
            string npat = null,
            string tipusProvaAnterior = null,
            string tipusProvaNou = null) => 
            _multiRDbService.GuardarHistorialMostra(
                etiquetaId, 
                tipusCanvi, 
                combinacionsAnteriors, 
                dataResultatAnterior, 
                dataValidacioAnterior, 
                combinacionsNoves, 
                dataResultatNova, 
                dataValidacioNova,
                npat,
                tipusProvaAnterior,
                tipusProvaNou);

        public bool GuardarHistorialAutomaticMostra(Mostra mostra, TipusIncorporacio tipusIncorporacio, string observacions = null) => 
            _multiRDbService.GuardarHistorialAutomaticMostra(mostra, tipusIncorporacio, observacions);

        public int NetejarHistorialAntic(int diesRetencio = 90) => 
            _multiRDbService.NetejarHistorialAntic(diesRetencio);

        #region Control de Sincronització

        public DadesSincronitzacio ObtenirUltimaSincronitzacio() =>
            _multiRDbService.ObtenirUltimaSincronitzacio();

        public int GuardarDadesSincronitzacio(DadesSincronitzacio dades) =>
            _multiRDbService.GuardarDadesSincronitzacio(dades);

        public bool ActualitzarEstatSincronitzacio(int id, string estat, string observacions = null) =>
            _multiRDbService.ActualitzarEstatSincronitzacio(id, estat, observacions);

        public int NetejarHistorialSincronitzacio(int diesRetencio = 90) =>
            _multiRDbService.NetejarHistorialSincronitzacio(diesRetencio);

        #endregion

        #region Vigència de Diagnòstics

        public bool MarcarDiagnosticNoVigent(int diagnosticId, string responsable) =>
            _multiRDbService.MarcarDiagnosticNoVigent(diagnosticId, responsable);

        public bool ReactivarDiagnostic(int diagnosticId, string responsable) =>
            _multiRDbService.ReactivarDiagnostic(diagnosticId, responsable);

        /// <summary>
        /// Obté els diagnòstics actius (vigents) d'un pacient amb el darrer positiu associat
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient (npat)</param>
        /// <param name="tipusMicroorganisme">Tipus de microorganisme per filtrar (Multiresistent, VirusRespiratori o null per tots)</param>
        /// <returns>Llista de diagnòstics actius amb informació del darrer positiu</returns>
        public List<DiagnosticActiuPacient> ObtenirDiagnosticsActiusPacient(string pacientSap, TipusMicroorganisme? tipusMicroorganisme = null) =>
            _multiRDbService.ObtenirDiagnosticsActiusPacient(pacientSap, tipusMicroorganisme);

        public string ConfeccionarNotaCursClinic(string pacientSap) =>
            _multiRDbService.ConfeccionarNotaCursClinic(pacientSap);

        public bool AfegirNotaCursClinicSiCal(string pacientSap, bool sShanAfegitPositius) =>
            _multiRDbService.AfegirNotaCursClinicSiCal(pacientSap, sShanAfegitPositius);

        public bool InserirNotaCursClinic(string npat, string nota, string tipus = "M") =>
            _multiRDbService.InserirNotaCursClinic(npat, nota, tipus);

        public string ConfeccionarNotaCursClinicVirusRespiratori(string pacientSap) =>
            _multiRDbService.ConfeccionarNotaCursClinicVirusRespiratori(pacientSap);

        public bool AfegirNotaCursClinicVirusRespiratoriSiCal(string pacientSap, bool sShanAfegitPositius) =>
            _multiRDbService.AfegirNotaCursClinicVirusRespiratoriSiCal(pacientSap, sShanAfegitPositius);


        List<int> IMultiRRepository.ObtenirDiagnosticsPositiusVigentsTipusMostra(string pacientSap, string tipusMostra, string etiqueta)
        {
            return _multiRDbService.ObtenirDiagnosticsPositiusVigentsTipusMostra(pacientSap, tipusMostra, etiqueta);
        }

        int IMultiRRepository.ObtenirIdMostraDiagnosticPerDiagnosticIEtiqueta(int diagnosticId, string etiqueta, string tipusMostra)
        {
            return _multiRDbService.ObtenirIdMostraDiagnosticPerDiagnosticIEtiqueta(diagnosticId, etiqueta, tipusMostra);
        }

        List<int> IMultiRRepository.ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents(string pacientSap, string tipusMostra, string etiqueta)
        {
            return _multiRDbService.ObtenirDiagnosticsPositiusPacientPerTipusMostraIEquivalents(pacientSap, tipusMostra, etiqueta);
        }

        // Seguiments de pacients
        public bool ActualitzarQuantitatTargetes(string npat, string tipusMostra) =>
            _multiRDbService.ActualitzarQuantitatTargetes(npat, tipusMostra);

        public bool ActualitzarDataUltimaMostra(string npat, string tipusMostra) =>
            _multiRDbService.ActualitzarDataUltimaMostra(npat, tipusMostra);

        #endregion
    }
}
