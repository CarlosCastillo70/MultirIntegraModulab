using System;
using System.Collections.Generic;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Enums;

namespace MultirIntegraModulab.Domain.Interfaces
{
    /// <summary>
    /// Port (interfície) per accedir a les dades de MultiR (MySQL)
    /// Seguint el principi de Dependency Inversion (SOLID)
    /// </summary>
    public interface IMultiRRepository
    {
        /// <summary>
        /// Obté la data actual del sistema MySQL
        /// </summary>
        DateTime GetCurrentDate();

        /// <summary>
        /// Obté el tipus de base de dades
        /// </summary>
        string GetDatabaseType();

        /// <summary>
        /// Obté el nombre de registres d'una taula
        /// </summary>
        int GetTableRecordCount(string tableName);

        // Pacients
        /// <summary>
        /// Comprova si un pacient existeix a la base de dades
        /// </summary>
        bool ExisteixPacient(string pacientSap);

        /// <summary>
        /// Insereix un nou pacient a la base de dades
        /// </summary>
        bool InserirPacient(DadesPacient dadesPacient);

        // Diagnòstics
        /// <summary>
        /// Comprova si existeix un diagnòstic per un pacient amb microorganisme i mecanisme
        /// </summary>
        /// <returns>ID del diagnòstic si existeix, 0 si no existeix</returns>
        int ComprovarDiagnosticExisteix(string pacientSap, string microorganisme, string mecanisme, string tipusMecanisme);

        /// <summary>
        /// Crea un nou diagnòstic per a un pacient
        /// </summary>
        /// <returns>ID del nou diagnòstic creat, 0 si ha fallat</returns>
        int CrearDiagnosticPacient(string pacientSap, string microorganisme, string mecanisme, string tipusMecanisme);

        /// <summary>
        /// Obtingué els IDs dels diagnòstics positius d'un pacient per un tipus de mostra específic,
        /// excloent opcionalment una etiqueta concreta
        /// Retorna només els diagnòstics amb mecanisme de resistència o microorganisme especial
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra (MOSTRA_DESCRIPCIO)</param>
        /// <param name="etiquetaExcloure">Etiqueta a excloure de la cerca (opcional)</param>
        /// <param name="microorganisme">Microorganisme per filtrar (opcional)</param>
        /// <param name="mecanisme">Mecanisme de resistència per filtrar (opcional)</param>
        /// <returns>Llista d'IDs de diagnòstics positius. Retorna llista buida si no n'hi ha</returns>
        List<int> ObtenirDiagnosticsPositiusPacientPerTipusMostra(string pacientSap, string tipusMostra, string etiquetaExcloure = null, string microorganisme = null, string mecanisme = null);

        /// <summary>
        /// Obtingué els IDs dels diagnòstics positius d'un pacient per un tipus de mostra específic
        /// i els seus tipus de mostra equivalents, excloent opcionalment una etiqueta concreta.
        /// Similar a ObtenirDiagnosticsPositiusPacientPerTipusMostra però també cerca pels tipus equivalents.
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra (MOSTRA_DESCRIPCIO)</param>
        /// <param name="etiquetaExcloure">Etiqueta a excloure de la cerca (opcional)</param>
        /// <param name="microorganisme">Microorganisme per filtrar (opcional)</param>
        /// <param name="mecanisme">Mecanisme de resistència per filtrar (opcional)</param>
        /// <returns>Llista d'IDs de diagnòstics positius. Retorna llista buida si no n'hi ha</returns>
        List<int> ObtenirDiagnosticsPositiusPacientPerTipusMostraIEquivalents(string pacientSap, string tipusMostra, string etiquetaExcloure = null, string microorganisme = null, string mecanisme = null);

        /// <summary>
        /// Obté els IDs de tots els diagnòstics positius d'un pacient per qualsevol tipus de mostra
        /// Utilitzat per la Comprovació 1 de mostres negatives (comportament = 1)
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="etiqueta">Etiqueta a excloure de la cerca (opcional)</param>
        /// <returns>Llista d'IDs de diagnòstics positius. Retorna llista buida si no n'hi ha</returns>
        List<int> ObtenirDiagnosticsPositiusPacientAlgunTipusMostra(string pacientSap, string etiqueta = null);

        /// <summary>
        /// Obté els IDs dels diagnòstics positius vigents d'un pacient per un tipus de mostra 
        /// específic
        /// Utilitzat per la Comprovació 2 de mostres negatives
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra (MOSTRA_DESCRIPCIO)</param>
        /// <param name="etiqueta">Etiqueta a excloure de la cerca (opcional)</param>
        /// <returns>Llista d'IDs de diagnòstics positius vigents. Retorna llista buida si no n'hi ha</returns>
        List<int> ObtenirDiagnosticsPositiusVigentsTipusMostra(string pacientSap, string tipusMostra, string etiqueta = null);

        /// <summary>
        /// Obté els IDs dels diagnòstics positius vigents d'un pacient per un tipus de mostra 
        /// específic i els seus equivalents
        /// Utilitzat per la Comprovació 2 de mostres negatives
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra (MOSTRA_DESCRIPCIO)</param>
        /// <param name="etiqueta">Etiqueta a excloure de la cerca (opcional)</param>
        /// <returns>Llista d'IDs de diagnòstics positius vigents. Retorna llista buida si no n'hi ha</returns>
        List<int> ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents(string pacientSap, string tipusMostra, string etiqueta = null);

        /// <summary>
        /// Obté informació d'un diagnòstic concret
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <returns>Informació del diagnòstic o null si no existeix</returns>
        DiagnosticInfo ObtenirInformDiagnostic(int diagnosticId);

        /// <summary>
        /// Comprova si un diagnòstic té alguna mostra associada amb una etiqueta específica
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <param name="etiqueta">Etiqueta de la mostra a comprovar</param>
        /// <param name="tipusMostra">Tipus de mostra per filtrar</param>
        /// <returns>True si el diagnòstic té alguna mostra amb aquesta etiqueta, False en cas contrari</returns>
        bool DiagnosticTeMostraAmbEtiqueta(int diagnosticId, string etiqueta, string tipusMostra);

        /// <summary>
        /// Obté l'ID de la mostra diagnòstic associada a un diagnòstic amb una etiqueta i tipus específics
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <param name="etiqueta">Etiqueta de la mostra</param>
        /// <param name="tipusMostra">Tipus de mostra</param>
        /// <returns>ID de la mostra diagnòstic, o 0 si no existeix</returns>
        int ObtenirIdMostraDiagnosticPerDiagnosticIEtiqueta(int diagnosticId, string etiqueta, string tipusMostra);

        // Mostres diagnòstic
        /// <summary>
        /// Comprova si existeix una mostra diagnòstic
        /// </summary>
        /// <param name="pacientSap">SAP del pacient</param>
        /// <param name="dataMostra">Data de la mostra</param>
        /// <param name="tipusMostra">Tipus de mostra</param>
        /// <param name="valoracio">Valoració de la mostra (opcional). Si té valor, es filtra per aquesta valoració</param>
        /// <param name="etiqueta">Etiqueta de la mostra (opcional). Si té valor, es filtra per aquesta etiqueta</param>
        /// <returns>ID de la mostra si existeix, 0 si no existeix</returns>
        int ComprovarMostraDiagnosticExisteix(string pacientSap, DateTime? dataMostra, string tipusMostra, string valoracio = null, string etiqueta = null);

        /// <summary>
        /// Comprova si existeix una mostra diagnòstic amb una etiqueta específica
        /// </summary>
        /// <param name="pacientSap">SAP del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra</param>
        /// <param name="valoracio">Valoració de la mostra (1=negatiu, 2=positiu)</param>
        /// <param name="etiqueta">Etiqueta específica de la mostra</param>
        /// <returns>ID de la mostra si existeix, 0 si no existeix</returns>
        int ComprovarMostraDiagnosticPerEtiqueta(string pacientSap, string tipusMostra, string valoracio, string etiqueta);

        /// <summary>
        /// Crea una nova mostra diagnòstic
        /// </summary>
        /// <returns>ID de la nova mostra creada, 0 si ha fallat</returns>
        int CrearMostraDiagnostic(string pacientSap, DateTime? dataMostra, string tipusMostra, 
            string tipusProva, string etiqueta, DateTime? dataResultat, DateTime? dataValidacio, 
            string mecanismeId, bool? esMicroorganismeEspecial, string microorganismeMecanismeCaptat);

        /// <summary>
        /// Actualitza el camp microorganisme_mecanisme_captat d'una mostra diagnòstic existent.
        /// Si el camp ja té valor, concatena el nou valor amb una coma.
        /// </summary>
        /// <param name="mostraDiagnosticId">ID de la mostra diagnòstic</param>
        /// <param name="nouMicroorganismeMecanisme">Nou valor a afegir</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        bool ActualitzarMicroorganismeMecanismeCaptat(int mostraDiagnosticId, string nouMicroorganismeMecanisme);

        /// <summary>
        /// Obté les dades completes d'una mostra diagnòstic existent
        /// </summary>
        /// <param name="etiquetaId">Etiqueta de la mostra</param>
        /// <returns>Dades de la mostra diagnòstic o null si no existeix</returns>
        MostraDiagnosticExistent ObtenirMostraDiagnostic(string etiquetaId);

        /// <summary>
        /// Compara una mostra entrant amb una mostra existent per detectar canvis
        /// </summary>
        /// <param name="mostraExistent">Mostra existent a la base de dades</param>
        /// <param name="mostraEntrant">Mostra que està entrant</param>
        /// <returns>Resultat de la comparació amb detall dels canvis</returns>
        ResultatComparacioMostres CompararMostres(MostraDiagnosticExistent mostraExistent, Mostra mostraEntrant);

        /// <summary>
        /// Esborra les dades d'una mostra (soft delete)
        /// </summary>
        bool EsborrarDadesMostra(string etiquetaId);

        /// <summary>
        /// Obté les combinacions de microorganisme + mecanismes d'una mostra existent a la BD
        /// NOMÉS obtenim diagnòstics positius: amb mecanisme de resistència
        /// </summary>
        /// <param name="etiquetaId">Etiqueta de la mostra</param>
        /// <returns>Llista de combinacions microorganisme + mecanismes</returns>
        List<CombinacioMicroorganismeMecanisme> ObtenirCombinacionsMicroorganismeMecanisme(string etiquetaId);

        /// <summary>
        /// Obté les combinacions de microorganisme + mecanismes d'una mostraentrant
        /// NOMÉS retorna les combinacions POSITIVES: amb mecanisme de resistència o microorganisme especial
        /// </summary>
        /// <param name="mostra">Mostra entrant</param>
        /// <returns>Llista de combinacions microorganisme + mecanismes</returns>
        List<CombinacioMicroorganismeMecanisme> ObtenirCombinacionsMostraEntrant(Mostra mostra);

        // Microorganismes
        bool? EsMicroorganismeEspecial(string microorganismeDescripcio);
        Microorganisme ObtenirMicroorganisme(string descripcio);
        List<Microorganisme> ObtenirMicroorganismesEspecials();
        void CarregarMicroorganismesEspecials();
        void NetejarCacheMicroorganismes();
        string ObtenirEstadistiquesCache();
        bool ComprovarICrearMicroorganisme(string microorganismeDescripcio);

        // Mecanismes de resistència
        EstatMecanisme ComprovarExistenciaMecanisme(string mecanismeCodi);
        bool CrearMecanisme(string mecanismeCodi, string mecanismeDescripcio);

        // Resultats
        int ComprovarResultatExisteix(string etiquetaId);
        EstatResultat ObtenirEstatResultat(string etiquetaId);
        TipusEstatResultat ClassificarEstatResultat(string etiquetaId, DateTime? dataResultatOracle, DateTime? dataValidacioOracle);
        bool ActualitzarResultatAmbNovesDates(string etiquetaId, DateTime? dataResultat, DateTime? dataValidacio);
        bool ActualitzarResultatAntic(string etiquetaId, DateTime dataResultat, DateTime? dataValidacio);
        bool ActualitzarDataValidacio(string etiquetaId, DateTime? dataValidacio);

        // Mostres i microorganismes
        bool InserirMostraMicroorganisme(string etiquetaId, string microorganismeDescripcio);
        bool ComprovarMostraMicroorganismeExisteix(int diagnosticId, int mostraDiagnosticId);
        bool CrearMostraMicroorganisme(int diagnosticId, int mostraDiagnosticId);
        bool EsCombinacioNoIncorporar(string microorganisme, string mecanisme);

        /// <summary>
        /// Actualitza la data_diagnostic amb la data de mostra més antiga
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="microorganismeCodi">Codi del microorganisme</param>
        /// <param name="mecanismeId">ID del mecanisme de resistència</param>
        /// <param name="tipusMecanisme">Tipus/descripció del mecanisme</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        bool ActualitzarDataDiagnosticPacientsDiagnostics(
            string pacientSap, 
            string microorganismeCodi, 
            string mecanismeId, 
            string tipusMecanisme);

        /// <summary>
        /// Actualitza la data_diagnostic de pacients_diagnostics_mostra amb la data de mostra més antiga
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="microorganismeCodi">Codi del microorganisme</param>
        /// <param name="mecanismeId">ID del mecanisme de resistència</param>
        /// <param name="tipusMecanisme">Tipus/descripció del mecanisme</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        bool ActualitzarDataDiagnosticPacientsDiagnosticsMostra(
            string pacientSap, 
            string microorganismeCodi, 
            string mecanismeId, 
            string tipusMecanisme);

        // Tipus de mostra
        /// <summary>
        /// Comprova si existeix un tipus de mostra a la taula tipusmostra_m
        /// </summary>
        /// <param name="codiMostra">Codi de la mostra (MOSTRA_DESCRIPCIO de Modulab)</param>
        /// <returns>True si existeix i està actiu, False en cas contrari</returns>
        bool ExisteixTipusMostraMactiu(string codiMostra);

        /// <summary>
        /// Crea un nou tipus de mostra a la taula tipusmostra_m
        /// </summary>
        /// <param name="codiMostra">Codi de la mostra (MOSTRA_DESCRIPCIO de Modulab)</param>
        /// <returns>True si s'ha creat correctament, False en cas contrari</returns>
        bool CrearTipusMostraM(string codiMostra);

        /// <summary>
        /// Obté el comportament d'un tipus de mostra
        /// </summary>
        /// <param name="codiMostra">Codi de la mostra (MOSTRA_DESCRIPCIO de Modulab)</param>
        /// <returns>El valor de comportament (0, 1, etc.) o null si no existeix o no està actiu</returns>
        int? ObtenirComportamentTipusMostra(string codiMostra);

        /// <summary>
        /// Comprova si el pacient té algun diagnòstic positiu (per qualsevol tipus de mostra)
        /// Un diagnòstic és positiu si té valoració = '2'
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <returns>True si el pacient té almenys un diagnòstic positiu, False en cas contrari</returns>
        bool PacientTePositiusAlgunTipusMostra(string pacientSap);

        /// <summary>
        /// Comprova si el pacient té algun diagnòstic positiu vigent per un tipus de mostra específic
        /// i els seus tipus de mostra equivalents.
        /// Un positiu és vigent si no ha superat els dies_vigencia_positiu del tipus de mostra.
        /// </summary>
        /// <param name="pacientSap">Identificador del pacient</param>
        /// <param name="tipusMostra">Tipus de mostra (MOSTRA_DESCRIPCIO)</param>
        /// <param name="etiquetaExcloure">Etiqueta a excloure de la cerca (opcional)</param>
        /// <param name="dataResultatExcloure">Data resultat a excloure - filtra mostres amb data resultat diferent (opcional)</param>
        /// <returns>True si el pacient té almenys un positiu vigent per aquest tipus o equivalents</returns>
        bool PacientTePositiusVigentsTipusMostraIEquivalents(
            string pacientSap, 
            string tipusMostra, 
            string etiquetaExcloure = null,
            DateTime? dataResultatExcloure = null);

        // Tipus de prova
        /// <summary>
        /// Comprova si existeix un tipus de prova a la taula tipusprova
        /// </summary>
        /// <param name="codiProva">Codi de la prova (PROVA_DESCRIPCIO de Modulab)</param>
        /// <returns>True si existeix i està actiu, False en cas contrari</returns>
        bool ExisteixTipusProvaActiu(string codiProva);

        /// <summary>
        /// Crea un nou tipus de prova a la taula tipusprova
        /// </summary>
        /// <param name="codiProva">Codi de la prova (PROVA_DESCRIPCIO de Modulab)</param>
        /// <returns>True si s'ha creat correctament, False en cas contrari</returns>
        bool CrearTipusProva(string codiProva);

        // Integració
        bool InserirIntegracioResultats(string etiquetaId, ResultatMostra registre, string mecanismeId, 
            string estat, string observacions, bool incorporaModulab);
        bool InserirAuditoriaIntegracioModulab(Mostra mostra, string codiResultat, ResultatMostra resultatMostra = null, MecanismeResistenciaInfo mecanisme = null);

        // Historial
        EstadistiquesHistorial ObtenirEstadistiquesHistorial();
        int ComprovarHistorialExisteix(string etiquetaId);
        List<RegistreHistorialMostra> ObtenirHistorialMostra(string etiquetaId);
        
        /// <summary>
        /// Guarda un registre d'historial per una mostra amb tota la informació
        /// </summary>
        /// <param name="etiquetaId">Identificador de l'etiqueta</param>
        /// <param name="tipusCanvi">Tipus de canvi realitzat (VALIDADA_AMB_CANVIS, REVALIDADA_AMB_CANVIS, DESVALIDADA_AMB_CANVIS)</param>
        /// <param name="combinacionsAnteriors">Combinacions microorganisme+mecanisme anteriors (JSON o text)</param>
        /// <param name="dataResultatAnterior">Data resultat anterior</param>
        /// <param name="dataValidacioAnterior">Data validació anterior</param>
        /// <param name="combinacionsNoves">Combinacions microorganisme+mecanisme noves (JSON o text)</param>
        /// <param name="dataResultatNova">Data resultat nova</param>
        /// <param name="dataValidacioNova">Data validació nova</param>
        /// <returns>True si s'ha guardat correctament</returns>
        bool GuardarHistorialMostra(
            string etiquetaId, 
            string tipusCanvi, 
            string combinacionsAnteriors = null,
            DateTime? dataResultatAnterior = null,
            DateTime? dataValidacioAnterior = null,
            string combinacionsNoves = null,
            DateTime? dataResultatNova = null,
            DateTime? dataValidacioNova = null);
        
        bool GuardarHistorialAutomaticMostra(Mostra mostra, TipusIncorporacio tipusIncorporacio, string observacions = null);
        int NetejarHistorialAntic(int diesRetencio = 90);

        #region Control de Sincronització

        /// <summary>
        /// Obté les dades de l'última sincronització exitosa
        /// </summary>
        /// <returns>Dades de sincronització o null si és la primera execució</returns>
        DadesSincronitzacio ObtenirUltimaSincronitzacio();

        /// <summary>
        /// Guarda les dades d'una nova sincronització
        /// </summary>
        /// <param name="dades">Dades de sincronització a guardar</param>
        /// <returns>ID del registre creat</returns>
        int GuardarDadesSincronitzacio(DadesSincronitzacio dades);

        /// <summary>
        /// Actualitza l'estat d'una sincronització
        /// </summary>
        /// <param name="id">ID de la sincronització</param>
        /// <param name="estat">Nou estat (OK, ERROR, PARCIAL)</param>
        /// <param name="observacions">Observacions adicionals</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        bool ActualitzarEstatSincronitzacio(int id, string estat, string observacions = null);

        /// <summary>
        /// Neteja registres de sincronització antics
        /// </summary>
        /// <param name="diesRetencio">Nombre de dies a mantenir (per defecte 90)</param>
        /// <returns>Nombre de registres esborrats</returns>
        int NetejarHistorialSincronitzacio(int diesRetencio = 90);

        #endregion

        #region Vigència de Diagnòstics

        /// <summary>
        /// Marca un diagnòstic com a no vigent
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <param name="responsable">Usuari que marca com a no vigent</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        bool MarcarDiagnosticNoVigent(int diagnosticId, string responsable);

        /// <summary>
        /// Reactiva un diagnòstic (el torna a marcar com a vigent)
        /// </summary>
        /// <param name="diagnosticId">ID del diagnòstic</param>
        /// <param name="responsable">Usuari que reactiva</param>
        /// <returns>True si s'ha actualitzat correctament</returns>
        bool ReactivarDiagnostic(int diagnosticId, string responsable);

        #endregion
    }
}
