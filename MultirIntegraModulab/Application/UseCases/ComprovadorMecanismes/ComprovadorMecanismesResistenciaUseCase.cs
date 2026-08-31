using System;
using System.Collections.Generic;
using System.Linq;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Domain.Enums;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab.Application.UseCases.ComprovadorMecanismes
{
    /// <summary>
    /// Resultat de la comprovació d'un mecanisme individual
    /// </summary>
    public class ResultatComprovacioMecanisme
    {
        /// <summary>
        /// Indica si el mecanisme s'ha eliminat perquè està marcat com NO INCORPORAR (MNI)
        /// </summary>
        public bool EliminatMNI { get; set; }
        
        /// <summary>
        /// Indica si és una combinació microorganisme+mecanisme prohibida (CNI)
        /// </summary>
        public bool EsCombinacioCNI { get; set; }
        
        public ResultatComprovacioMecanisme()
        {
            EliminatMNI = false;
            EsCombinacioCNI = false;
        }
    }
    
    /// <summary>
    /// Resultat de la comprovació de mecanismes de resistència
    /// </summary>
    public class ResultatComprovacioMecanismes
    {
        public bool Exitosa { get; set; }
        public bool ContinuarProcessament { get; set; }
        public string Missatge { get; set; }
        public List<string> MecanismesCreats { get; set; }
        public List<string> MecanismesNoIncorporats { get; set; }
        public List<string> CombinacionsNoIncorporar { get; set; }
        public Dictionary<string, bool> MecanismesExistents { get; set; }
        
        /// <summary>
        /// Indica si s'han eliminat mecanismes NO INCORPORAR i cal reclassificar la mostra
        /// </summary>
        public bool CalReclassificar { get; set; }
        
        /// <summary>
        /// Llista de resultats que s'han de descartar (combinació NO INCORPORAR)
        /// Conté els índexs dels resultats a eliminar de la col·lecció
        /// </summary>
        public List<int> ResultatsADescartar { get; set; }

        public ResultatComprovacioMecanismes()
        {
            MecanismesCreats = new List<string>();
            MecanismesNoIncorporats = new List<string>();
            CombinacionsNoIncorporar = new List<string>();
            MecanismesExistents = new Dictionary<string, bool>();
            ResultatsADescartar = new List<int>();
            Exitosa = true;
            ContinuarProcessament = true;
            CalReclassificar = false;
        }
    }

    /// <summary>
    /// Use Case per comprovar mecanismes de resistència
    /// Verifica l'existència dels mecanismes i comprova combinacions prohibides
    /// </summary>
    public class ComprovadorMecanismesResistenciaUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly ILoggerService _logger;

        public ComprovadorMecanismesResistenciaUseCase(
            IMultiRRepository multiRRepository,
            ILoggerService logger)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executa la comprovació de mecanismes de resistència per una mostra
        /// </summary>
        /// <param name="mostra">Mostra amb els mecanismes a comprovar</param>
        /// <returns>Resultat de la comprovació</returns>
        public ResultatComprovacioMecanismes Executar(Mostra mostra)
        {
            if (mostra == null)
            {
                _logger.Warning("Intentant comprovar mecanismes amb mostra null");
                throw new ArgumentNullException(nameof(mostra));
            }

            _logger.Info($"🔎 Comprovant mecanismes de resistència");

            var resultat = new ResultatComprovacioMecanismes();

            try
            {
                // Comprovar cada resultat de la mostra
                for (int i = 0; i < mostra.Resultats.Count; i++)
                {
                    var resultatMostra = mostra.Resultats[i];

                    ComprovarMecanismesRegistre(resultatMostra, mostra, resultat, i);

                    if (!resultat.ContinuarProcessament)
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ S'atura el processament de la mostra {mostra.EtiquetaId} per error crític en la comprovació de mecanismes");
                        return resultat;
                    }

                    // Nota: Ni CNI ni MNI aturen el bucle; continuem comprovant tots els resultats de la mostra
                }

                if (resultat.MecanismesCreats.Any())
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Creats {resultat.MecanismesCreats.Count} mecanismes nous");
                }

                if (resultat.MecanismesNoIncorporats.Any())
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Eliminats {resultat.MecanismesNoIncorporats.Count} per mecanisme(s) marcats com NO INCORPORAR");
                }
                
                if (resultat.ResultatsADescartar.Any())
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Descartats {resultat.ResultatsADescartar.Count} resultat(s) amb combinació NO INCORPORAR (CNI)");
                    
                    // Eliminar els resultats descartats de la col·lecció (en ordre invers per no afectar els índexs)
                    foreach (var index in resultat.ResultatsADescartar.OrderByDescending(x => x))
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}🗑️ Eliminant resultat {index + 1} de {mostra.Resultats.Count} de la col·lecció (combinació CNI)");
                        mostra.Resultats.RemoveAt(index);
                    }
                    
                    // Marcar que cal reclassificar després d'eliminar resultats
                    resultat.CalReclassificar = true;
                }

                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Comprovació de mecanismes de la mostra, completada");
                resultat.Missatge = "Tots els mecanismes comprovats correctament";
                
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error comprovant mecanismes per mostra {mostra.EtiquetaId}", ex);
                resultat.Exitosa = false;
                resultat.ContinuarProcessament = false;
                resultat.Missatge = ex.Message;
                return resultat;
            }
        }

        /// <summary>
        /// Comprova els mecanismes d'un registre
        /// </summary>
        private void ComprovarMecanismesRegistre(
            ResultatMostra registre, 
            Mostra mostra,
            ResultatComprovacioMecanismes resultat,
            int indexResultat)
        {
            // Obtenir tots els mecanismes de resistència del registre
            var mecanismes = ObtenirMecanismesRegistre(registre);

            if (!mecanismes.Any())
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Resultat amb microorganisme '{registre.AillamentDescripcio}' SENSE mecanismes de resistència");
                return;
            }

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Resultat amb microorganisme '{registre.AillamentDescripcio}' SI que té {mecanismes.Count} mecanisme(s) de resistència");

            // Llista per guardar els mecanismes que s'han d'eliminar del registre
            var mecanismesAEliminar = new List<int>(); // Posicions (1-5) dels mecanismes a eliminar
            bool resultatTeCombinacioCNI = false;

            // Comprovar cada mecanisme
            for (int i = 0; i < mecanismes.Count; i++)
            {
                var mecanisme = mecanismes[i];
                int posicio = i + 1; // Posició del mecanisme (1-5)
                
                var resultatComprovacio = ComprovarMecanisme(mecanisme, registre, mostra, resultat, posicio);
                
                if (resultatComprovacio.EliminatMNI)
                {
                    mecanismesAEliminar.Add(posicio);
                }
                
                // Si es detecta combinació prohibida (CNI), marcar aquest resultat per descartar
                if (resultatComprovacio.EsCombinacioCNI)
                {
                    resultatTeCombinacioCNI = true;
                    // No aturem el bucle, continuem comprovant altres mecanismes per auditoria completa
                }
            }
            
            // Si el resultat té alguna combinació CNI, marcar-lo per descartar
            if (resultatTeCombinacioCNI)
            {
                resultat.ResultatsADescartar.Add(indexResultat);
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}📝 Resultat {indexResultat + 1} marcat per descartar (combinació CNI)");
                return; // No processar més aquest resultat
            }

            // Eliminar els mecanismes marcats com NO INCORPORAR del registre (només MNI, no CNI)
            if (mecanismesAEliminar.Any())
            {
                EliminarMecanismesDelRegistre(registre, mecanismesAEliminar);
                
                // Marcar que cal reclassificar la mostra
                resultat.CalReclassificar = true;
                
                // Després d'eliminar els mecanismes NO INCORPORAR, comprovar si encara queden mecanismes vàlids
                var mecanismesRestants = ObtenirMecanismesRegistre(registre);
                
                if (!mecanismesRestants.Any())
                {
                    // Si no queden mecanismes i el microorganisme NO és especial, 
                    // aquest resultat esdevé efectivament un negatiu
                    bool esMicroorganismeEspecial = registre.EsMicroorganismeEspecial ?? false;
                    
                    if (!esMicroorganismeEspecial)
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚠️ Després d'eliminar mecanismes NO INCORPORAR, el registre no té cap mecanisme vàlid i el microorganisme NO és especial");
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Aquest resultat es tractarà com a NEGATIU després de reclassificar");
                    }
                    else
                    {
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}⚡ Després d'eliminar mecanismes NO INCORPORAR, el registre no té cap mecanisme però el microorganisme ÉS ESPECIAL");
                        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Aquest resultat es tractarà com a POSITIU (microorganisme especial) després de reclassificar");
                    }
                }
                else
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}✔️ Després d'eliminar {mecanismesAEliminar.Count} mecanisme(s) NO INCORPORAR, encara queden {mecanismesRestants.Count} mecanisme(s) vàlid(s) per processar");
                }
            }
        }

        /// <summary>
        /// Obté la llista de mecanismes d'un registre
        /// </summary>
        private List<(string id, string descripcio)> ObtenirMecanismesRegistre(ResultatMostra registre)
        {
            var mecanismes = new List<(string id, string descripcio)>();
            
            if (!string.IsNullOrWhiteSpace(registre.MecanismeResistencia1Id))
                mecanismes.Add((registre.MecanismeResistencia1Id, registre.MecanismeResistenciaDescrip ?? ""));
            
            if (!string.IsNullOrWhiteSpace(registre.MecanismeResistencia2Id))
                mecanismes.Add((registre.MecanismeResistencia2Id, registre.MecanismeResistenciaDescrip2 ?? ""));
            
            if (!string.IsNullOrWhiteSpace(registre.MecanismeResistencia3Id))
                mecanismes.Add((registre.MecanismeResistencia3Id, registre.MecanismeResistenciaDescrip3 ?? ""));
            
            if (!string.IsNullOrWhiteSpace(registre.MecanismeResistencia4Id))
                mecanismes.Add((registre.MecanismeResistencia4Id, registre.MecanismeResistenciaDescrip4 ?? ""));
            
            if (!string.IsNullOrWhiteSpace(registre.MecanismeResistencia5Id))
                mecanismes.Add((registre.MecanismeResistencia5Id, registre.MecanismeResistenciaDescrip5 ?? ""));

            return mecanismes;
        }

        /// <summary>
        /// Comprova un mecanisme individual
        /// </summary>
        /// <returns>Resultat de la comprovació del mecanisme</returns>
        private ResultatComprovacioMecanisme ComprovarMecanisme(
            (string id, string descripcio) mecanisme, 
            ResultatMostra resultatMostra, 
            Mostra mostra,
            ResultatComprovacioMecanismes resultat,
            int posicio)
        {
            var resultatMecanisme = new ResultatComprovacioMecanisme();
            
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Comprovant existencia del mecanisme: '{mecanisme.id} {mecanisme.descripcio}' i combinacions microorganisme / mecanisme, a no incorporar");

            // 1. Comprovar si el mecanisme existeix
            var estatMecanisme = _multiRRepository.ComprovarExistenciaMecanisme(mecanisme.id);
            
            if (!estatMecanisme.Existeix)
            {
                // No existeix. Crear-lo
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Creant mecanisme nou: {mecanisme.id} - {mecanisme.descripcio}");
                
                bool creatCorrectament = _multiRRepository.CrearMecanisme(mecanisme.id, mecanisme.descripcio);
                
                if (creatCorrectament)
                {
                    resultat.MecanismesCreats.Add(mecanisme.id);
                    resultat.MecanismesExistents[mecanisme.id] = false; // Era nou
                }
                else
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}❌ No s'ha pogut crear el mecanisme {mecanisme.id}");
                }
            }
            else
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✓ Mecanisme : '{mecanisme.id} - {mecanisme.descripcio}' JA existeix");
                resultat.MecanismesExistents[mecanisme.id] = true; // Ja existia
                
                // 1.1. Comprovar si incorpora_modulab és 0 (no s'ha d'incorporar)
                if (estatMecanisme.IncorporaModulab.HasValue && !estatMecanisme.IncorporaModulab.Value)
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Mecanisme de resistència {mecanisme.id} marcat com NO INCORPORAR ");
                    
                    // Afegir a la llista de mecanismes no incorporats
                    resultat.MecanismesNoIncorporats.Add(mecanisme.id);

                    // Crear informació del mecanisme per a l'auditoria
                    var mecanismeInfo = new MecanismeResistenciaInfo
                    {
                        Id = mecanisme.id,
                        Descripcio = mecanisme.descripcio
                    };

                    // Guardar auditoria
                    _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "MNI", resultatMostra, mecanismeInfo);

                    // Marcar que s'ha d'eliminar aquest mecanisme del registre
                    // No s'atura el processament global: els altres resultats de la mostra es continuaran comprovant
                    resultatMecanisme.EliminatMNI = true;
                }
            }

            // 2. Comprovar si la combinació microorganisme-mecanisme està marcada com "No Incorporar"
            if (!string.IsNullOrWhiteSpace(resultatMostra.AillamentDescripcio))
            {
                bool esNoIncorporar = _multiRRepository.EsCombinacioNoIncorporar(
                    resultatMostra.AillamentDescripcio, 
                    mecanisme.descripcio);
                
                if (esNoIncorporar)
                {
                    // Es una combinació marcada com a no incorporar (CNI)
                    // Aquest resultat NO es processarà (ni com a positiu ni com a negatiu)

                    string combinacio = $"{resultatMostra.AillamentDescripcio} + {mecanisme.id}";
                    
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Combinació marcada com NO INCORPORAR: {combinacio}");
                    
                    resultat.CombinacionsNoIncorporar.Add(combinacio);
                    
                    // Guardar auditoria
                    _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "CNI", resultatMostra);
                    
                    // Marcar que és una combinació CNI
                    resultatMecanisme.EsCombinacioCNI = true;
                }
            }

            return resultatMecanisme;
        }

        /// <summary>
        /// Elimina els mecanismes marcats del registre establint-los a null
        /// </summary>
        private void EliminarMecanismesDelRegistre(ResultatMostra registre, List<int> posicions)
        {
            foreach (int posicio in posicions)
            {
                switch (posicio)
                {
                    case 1:
                        registre.MecanismeResistencia1Id = null;
                        registre.MecanismeResistenciaDescrip = null;
                        break;
                    case 2:
                        registre.MecanismeResistencia2Id = null;
                        registre.MecanismeResistenciaDescrip2 = null;
                        break;
                    case 3:
                        registre.MecanismeResistencia3Id = null;
                        registre.MecanismeResistenciaDescrip3 = null;
                        break;
                    case 4:
                        registre.MecanismeResistencia4Id = null;
                        registre.MecanismeResistenciaDescrip4 = null;
                        break;
                    case 5:
                        registre.MecanismeResistencia5Id = null;
                        registre.MecanismeResistenciaDescrip5 = null;
                        break;
                }

                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}🗑️ Eliminat mecanisme {posicio} del registre (marcat com NO INCORPORAR)");
            }
        }
    }
}
