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

        public List<int> ObtenirDiagnosticsPositiusPacientAlgunTipusMostra(string pacientSap, string etiqueta = null) =>
            _multiRDbService.ObtenirDiagnosticsPositiusPacientAlgunTipusMostra(pacientSap, etiqueta);

        public List<int> ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents(string pacientSap, string tipusMostra, string etiqueta = null) =>
            _multiRDbService.ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents(pacientSap, tipusMostra, etiqueta);

        public DiagnosticInfo ObtenirInformDiagnostic(int diagnosticId) =>
            _multiRDbService.ObtenirInformDiagnostic(diagnosticId);

        // Mostres diagnòstic
        public int ComprovarMostraDiagnosticExisteix(string pacientSap, DateTime? dataMostra, string tipusMostra, string valoracio = null) => 
            _multiRDbService.ComprovarMostraDiagnosticExisteix(pacientSap, dataMostra, tipusMostra, valoracio);

        public int CrearMostraDiagnostic(string pacientSap, DateTime? dataMostra, string tipusMostra, 
            string tipusProva, string etiqueta, DateTime? dataResultat, DateTime? dataValidacio, 
            string mecanismeId, bool? esMicroorganismeEspecial) => 
            _multiRDbService.CrearMostraDiagnostic(pacientSap, dataMostra, tipusMostra, tipusProva, etiqueta, 
                dataResultat, dataValidacio, mecanismeId, esMicroorganismeEspecial);

        public bool EsborrarDadesMostra(string etiquetaId) => 
            _multiRDbService.EsborrarDadesMostra(etiquetaId);

        public MostraDiagnosticExistent ObtenirMostraDiagnostic(string etiquetaId) =>
            _multiRDbService.ObtenirMostraDiagnostic(etiquetaId);

        public ResultatComparacioMostres CompararMostres(MostraDiagnosticExistent mostraExistent, Mostra mostraEntrant) =>
            _multiRDbService.CompararMostres(mostraExistent, mostraEntrant);

        public List<MultiRDbService.CombinacioMicroorganismeMecanisme> ObtenirCombinacionsMicroorganismeMecanisme(string etiquetaId) =>
            _multiRDbService.ObtenirCombinacionsMicroorganismeMecanisme(etiquetaId);

        public List<MultiRDbService.CombinacioMicroorganismeMecanisme> ObtenirCombinacionsMostraEntrant(Mostra mostra) =>
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

        public bool PacientTePositiusVigentsTipusMostraIEquivalents(string pacientSap, string tipusMostra) =>
            _multiRDbService.PacientTePositiusVigentsTipusMostraIEquivalents(pacientSap, tipusMostra);

        public bool PacientTePositiusVigentsTipusMostraIEquivalents(string pacientSap, string tipusMostra, string etiquetaExcloure = null) =>
            _multiRDbService.PacientTePositiusVigentsTipusMostraIEquivalents(pacientSap, tipusMostra, etiquetaExcloure);

        // Tipus de prova
        public bool ExisteixTipusProvaActiu(string codiProva) =>
            _multiRDbService.ExisteixTipusProvaActiu(codiProva);

        public bool CrearTipusProva(string codiProva) =>
            _multiRDbService.CrearTipusProva(codiProva);

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
            DateTime? dataValidacioNova = null) => 
            _multiRDbService.GuardarHistorialMostra(
                etiquetaId, 
                tipusCanvi, 
                combinacionsAnteriors, 
                dataResultatAnterior, 
                dataValidacioAnterior, 
                combinacionsNoves, 
                dataResultatNova, 
                dataValidacioNova);

        public bool GuardarHistorialAutomaticMostra(Mostra mostra, TipusIncorporacio tipusIncorporacio, string observacions = null) => 
            _multiRDbService.GuardarHistorialAutomaticMostra(mostra, tipusIncorporacio, observacions);

        public int NetejarHistorialAntic(int diesRetencio = 90) => 
            _multiRDbService.NetejarHistorialAntic(diesRetencio);
    }
}
