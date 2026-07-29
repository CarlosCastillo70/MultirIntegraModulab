using System;
using System.Collections.Generic;
using System.Linq;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Domain.Enums;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab.Application.UseCases.ComprovadorMicroorganismes
{
    /// <summary>
    /// Resultat de la comprovació de microorganismes
    /// </summary>
    public class ResultatComprovacioMicroorganismes
    {
        public bool Exitosa { get; set; }
        public bool ContinuarProcessament { get; set; }
        public string Missatge { get; set; }
        public Dictionary<string, bool> MicroorganismesEspecials { get; set; }
        public List<string> MicroorganismesNoCreats { get; set; }
        public List<string> MicroorganismesNoIncorporats { get; set; }

        public ResultatComprovacioMicroorganismes()
        {
            MicroorganismesEspecials = new Dictionary<string, bool>();
            MicroorganismesNoCreats = new List<string>();
            MicroorganismesNoIncorporats = new List<string>();
        }
    }

    /// <summary>
    /// Use Case per comprovar i crear microorganismes
    /// Verifica l'existència dels microorganismes a la BD i els crea si cal
    /// </summary>
    public class ComprovadorMicroorganismesUseCase
    {
        private readonly IMultiRRepository _multiRRepository;
        private readonly ILoggerService _logger;

        public ComprovadorMicroorganismesUseCase(
            IMultiRRepository multiRRepository,
            ILoggerService logger)
        {
            _multiRRepository = multiRRepository ?? throw new ArgumentNullException(nameof(multiRRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executa la comprovació de microorganismes per una mostra
        /// </summary>
        /// <param name="mostra">Mostra amb els microorganismes a comprovar</param>
        /// <returns>Resultat de la comprovació</returns>
        public ResultatComprovacioMicroorganismes Executar(Mostra mostra)
        {
            if (mostra == null)
            {
                _logger.Warning("Intentant comprovar microorganismes amb mostra null");
                throw new ArgumentNullException(nameof(mostra));
            }

            _logger.Info($"🔎 Comprovant microorganismes");

            var resultat = new ResultatComprovacioMicroorganismes
            {
                Exitosa = true,
                ContinuarProcessament = true
            };

            try
            {
                // Obtenir tots els microorganismes únics de la mostra
                var microorganismes = ObtenirMicroorganismesUnics(mostra);

                if (!microorganismes.Any())
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ No hi ha microorganismes a la mostra {mostra.EtiquetaId}");
                    resultat.Missatge = "No hi ha microorganismes a comprovar";
                    return resultat;
                }

                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Trobats {microorganismes.Count} microorganismes únics a comprovar");

                // Comprovar cada microorganisme
                foreach (var microorganisme in microorganismes)
                {
                    ComprovarMicroorganisme(microorganisme, resultat);
                }

                if (resultat.MicroorganismesNoIncorporats.Any())
                {
                    resultat.ContinuarProcessament = false;
                    resultat.Missatge = $"Microorganisme(s) marcat(s) com NO INCORPORAR: {string.Join(", ", resultat.MicroorganismesNoIncorporats.Distinct())}";
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ {resultat.Missatge}");
                    return resultat;
                }

                if (resultat.MicroorganismesNoCreats.Any())
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}No s'han pogut crear {resultat.MicroorganismesNoCreats.Count} microorganismes");
                    resultat.Missatge = $"{resultat.MicroorganismesNoCreats.Count} microorganismes no creats";
                }
                else
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Comprovació de microorganismes de la mostra, completada");
                    resultat.Missatge = "Tots els microorganismes comprovats correctament";
                }

                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error comprovant microorganismes per mostra {mostra.EtiquetaId}", ex);
                resultat.Exitosa = false;
                resultat.ContinuarProcessament = false;
                resultat.Missatge = ex.Message;
                return resultat;
            }
        }

        /// <summary>
        /// Obté tots els microorganismes únics d'una mostra
        /// </summary>
        private HashSet<string> ObtenirMicroorganismesUnics(Mostra mostra)
        {
            var microorganismes = new HashSet<string>();
            
            foreach (var resultat in mostra.Resultats)
            {
                if (!string.IsNullOrWhiteSpace(resultat.AillamentDescripcio))
                {
                    microorganismes.Add(resultat.AillamentDescripcio.Trim());
                }
            }

            return microorganismes;
        }
        
        /// <summary>
        /// Comprova un microorganisme individual
        /// </summary>
        private void ComprovarMicroorganisme(
            string microorganisme, 
            ResultatComprovacioMicroorganismes resultat)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Comprovant microorganisme: '{microorganisme}'");
            
            try
            {
                // Comprovar si existeix i crear-lo si cal
                bool existeixOCreat = _multiRRepository.ComprovarICrearMicroorganisme(microorganisme);
                
                if (!existeixOCreat)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}No s'ha pogut comprovar/crear el microorganisme: {microorganisme}");
                    resultat.MicroorganismesNoCreats.Add(microorganisme);
                    return;
                }

                // Comprovar si el microorganisme està marcat com NO INCORPORAR
                var microorganismeBd = _multiRRepository.ObtenirMicroorganisme(microorganisme);
                if (microorganismeBd != null && !microorganismeBd.IncorporaModulab)
                {
                    _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ Microorganisme {microorganisme} marcat com NO INCORPORAR");
                    resultat.MicroorganismesNoIncorporats.Add(microorganisme);
                    resultat.ContinuarProcessament = false;
                    return;
                }

                // Comprovar si és especial
                var esEspecial = _multiRRepository.EsMicroorganismeEspecial(microorganisme);

                if (esEspecial.HasValue)
                {
                    string tipus = esEspecial.Value ? "ESPECIAL" : "normal";
                    string alerta = esEspecial.Value ? "⚡ " : " ";
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}{alerta}Microorganisme {microorganisme}: '{tipus}'");
                    resultat.MicroorganismesEspecials[microorganisme] = esEspecial.Value;
                }
                else
                {
                    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Microorganisme {microorganisme}: desconegut (nou a la base de dades)");
                    resultat.MicroorganismesEspecials[microorganisme] = false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Error comprovant microorganisme {microorganisme}", ex);
                resultat.MicroorganismesNoCreats.Add(microorganisme);
            }
        }
    }
}
