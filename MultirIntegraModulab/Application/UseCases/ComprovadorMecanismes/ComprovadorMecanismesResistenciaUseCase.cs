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

        public ResultatComprovacioMecanismes()
        {
            MecanismesCreats = new List<string>();
            MecanismesNoIncorporats = new List<string>();
            CombinacionsNoIncorporar = new List<string>();
            MecanismesExistents = new Dictionary<string, bool>();
            Exitosa = true;
            ContinuarProcessament = true;
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
                foreach (var resultatMostra in mostra.Resultats)
                {
                    ComprovarMecanismesRegistre(resultatMostra, mostra, resultat);
                    
                    // Si es detecta una combinació no incorporar, aturar immediatament
                    if (!resultat.ContinuarProcessament)
                    {
                        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}❌ Mostra {mostra.EtiquetaId} no es processarà per {resultat.Missatge}");
                        return resultat;
                    }
                }

                if (resultat.MecanismesCreats.Any())
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Creats {resultat.MecanismesCreats.Count} mecanismes nous");
                }

                if (resultat.MecanismesNoIncorporats.Any())
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Eliminats {resultat.MecanismesNoIncorporats.Count} mecanisme(s) marcats com NO INCORPORAR");
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
            ResultatComprovacioMecanismes resultat)
        {
            // Obtenir tots els mecanismes de resistència del registre
            var mecanismes = ObtenirMecanismesRegistre(registre);

            if (!mecanismes.Any())
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Registre amb microorganisme '{registre.AillamentDescripcio}' SENSE mecanismes de resistència");
                return;
            }

            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Registre amb microorganisme '{registre.AillamentDescripcio}' SI que té {mecanismes.Count} mecanisme(s) de resistència");

            // Llista per guardar els mecanismes que s'han d'eliminar del registre
            var mecanismesAEliminar = new List<int>(); // Posicions (1-5) dels mecanismes a eliminar

            // Comprovar cada mecanisme
            for (int i = 0; i < mecanismes.Count; i++)
            {
                var mecanisme = mecanismes[i];
                int posicio = i + 1; // Posició del mecanisme (1-5)
                
                bool eliminat = ComprovarMecanisme(mecanisme, registre, mostra, resultat, posicio);
                
                if (eliminat)
                {
                    mecanismesAEliminar.Add(posicio);
                }
                
                // Si es detecta combinació prohibida (CNI), aturar tot el processament
                if (!resultat.ContinuarProcessament)
                {
                    return;
                }
            }

            // Eliminar els mecanismes marcats com NO INCORPORAR del registre
            if (mecanismesAEliminar.Any())
            {
                EliminarMecanismesDelRegistre(registre, mecanismesAEliminar);
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
        /// <returns>True si el mecanisme s'ha d'eliminar (no incorporar), False en cas contrari</returns>
        private bool ComprovarMecanisme(
            (string id, string descripcio) mecanisme, 
            ResultatMostra resultatMostra, 
            Mostra mostra,
            ResultatComprovacioMecanismes resultat,
            int posicio)
        {
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
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Mecanisme : '{mecanisme.id} - {mecanisme.descripcio}' JA existeix");
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
                    
                    // Guardar auditoria (però continuar processant)
                    _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "MNI", resultatMostra, mecanismeInfo);
                    
                    // Retornar true per indicar que s'ha d'eliminar aquest mecanisme del registre
                    return true;
                }
            }

            // 2. Comprovar si la combinació microorganisme-mecanisme està marcada com "No Incorporar"
            if (!string.IsNullOrWhiteSpace(resultatMostra.AillamentDescripcio))
            {
                bool esNoIncorporar = _multiRRepository.EsCombinacioNoIncorporar(
                    resultatMostra.AillamentDescripcio, 
                    mecanisme.id);
                
                if (esNoIncorporar)
                {
                    // Es una combinació marcada com a no incorporar (CNI)
                    // En aquest cas SÍ que s'atura tot el processament

                    string combinacio = $"{resultatMostra.AillamentDescripcio} + {mecanisme.id}";
                    
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Combinació marcada com NO INCORPORAR: {combinacio}");
                    
                    resultat.CombinacionsNoIncorporar.Add(combinacio);
                    resultat.ContinuarProcessament = false;
                    resultat.Exitosa = false;
                    resultat.Missatge = $"Combinació {combinacio} marcada com NO INCORPORAR";
                    
                    // Guardar auditoria
                    _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "CNI", resultatMostra);
                    
                    // No retornar aquí, deixar que el codi que crida gestioni l'aturada
                }
                else
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Combinació microorganisme '{resultatMostra.AillamentDescripcio}' i mecanisme '{mecanisme.id} - {mecanisme.descripcio}' NO està marcada com a NO INCORPORAR");
                }
            }

            // Retornar false = aquest mecanisme NO s'ha d'eliminar
            return false;
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
