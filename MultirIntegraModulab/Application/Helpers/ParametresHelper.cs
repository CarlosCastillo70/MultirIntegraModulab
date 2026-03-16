using MultirIntegraModulab.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultirIntegraModulab.Application.Helpers
{
    /// <summary>
    /// Helper per gestionar paràmetres de l'aplicació amb fallback a App.config
    /// Implementa el patró: Primer BD, després App.config
    /// </summary>
    public class ParametresHelper
    {
        private readonly IMultiRRepository _repository;
        private readonly ILoggerService _logger;
        private readonly IConfigurationService _configurationService;
        
        // Cache per evitar logs repetits
        private readonly HashSet<string> _parametresJaLlegits = new HashSet<string>();
        private readonly object _lockCache = new object();

        public ParametresHelper(
            IMultiRRepository repository, 
            ILoggerService logger,
            IConfigurationService configurationService = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService; // Opcional per fallback
        }

        /// <summary>
        /// Comprova si un paràmetre ja ha estat llegit i registrat al log
        /// </summary>
        private bool EsPrimeraLectura(string categoria, string clau)
        {
            lock (_lockCache)
            {
                string clauCache = $"{categoria}.{clau}";
                
                if (_parametresJaLlegits.Contains(clauCache))
                {
                    return false;
                }
                
                _parametresJaLlegits.Add(clauCache);
                return true;
            }
        }

        /// <summary>
        /// Obté un paràmetre de tipus string
        /// Busca primer a BD, si no el troba, torna al valor per defecte
        /// </summary>
        public string ObtenirString(string categoria, string clau, string valorPerDefecte = null)
        {
            try
            {
                var valor = _repository.ObtenirParametre(categoria, clau);
                
                if (!string.IsNullOrEmpty(valor))
                {
                    // Només registrar al log la primera vegada
                    if (EsPrimeraLectura(categoria, clau))
                    {
                        _logger.Info($"?? Paràmetre {categoria}.{clau} = '{valor}' (BD)");
                    }
                    return valor;
                }
                
                if (valorPerDefecte != null && EsPrimeraLectura(categoria, clau))
                {
                    _logger.Debug($"?? Paràmetre {categoria}.{clau} = '{valorPerDefecte}' (defecte)");
                }
                
                return valorPerDefecte;
            }
            catch (Exception ex)
            {
                _logger.Warning($"?? Error llegint {categoria}.{clau} de BD: {ex.Message}. Utilitzant valor per defecte.");
                return valorPerDefecte;
            }
        }

        /// <summary>
        /// Obté un paràmetre de tipus int
        /// </summary>
        public int ObtenirInt(string categoria, string clau, int valorPerDefecte = 0)
        {
            try
            {
                var valor = _repository.ObtenirParametre(categoria, clau);
                
                if (!string.IsNullOrEmpty(valor) && int.TryParse(valor, out int resultat))
                {
                    // Només registrar al log la primera vegada
                    if (EsPrimeraLectura(categoria, clau))
                    {
                        _logger.Info($"?? Paràmetre {categoria}.{clau} = {resultat} (BD)");
                    }
                    return resultat;
                }
                
                if (EsPrimeraLectura(categoria, clau))
                {
                    _logger.Debug($"?? Paràmetre {categoria}.{clau} = {valorPerDefecte} (defecte)");
                }
                return valorPerDefecte;
            }
            catch (Exception ex)
            {
                _logger.Warning($"?? Error llegint {categoria}.{clau} de BD: {ex.Message}. Utilitzant valor per defecte.");
                return valorPerDefecte;
            }
        }

        /// <summary>
        /// Obté un paràmetre de tipus bool
        /// Accepta: 1/0, true/false, True/False, TRUE/FALSE
        /// </summary>
        public bool ObtenirBool(string categoria, string clau, bool valorPerDefecte = false)
        {
            try
            {
                var valor = _repository.ObtenirParametre(categoria, clau);
                
                if (string.IsNullOrEmpty(valor))
                {
                    if (EsPrimeraLectura(categoria, clau))
                    {
                        _logger.Debug($"?? Paràmetre {categoria}.{clau} = {valorPerDefecte} (defecte)");
                    }
                    return valorPerDefecte;
                }
                
                // Acceptar diferents formats
                var valorNormalitzat = valor.Trim().ToUpper();
                bool resultat = valorNormalitzat == "1" || 
                               valorNormalitzat == "TRUE" || 
                               valorNormalitzat == "YES" || 
                               valorNormalitzat == "SI" || 
                               valorNormalitzat == "SÍ";
                
                // Només registrar al log la primera vegada
                if (EsPrimeraLectura(categoria, clau))
                {
                    _logger.Info($"?? Paràmetre {categoria}.{clau} = {(resultat ? "Activat" : "Desactivat")} (BD)");
                }
                return resultat;
            }
            catch (Exception ex)
            {
                _logger.Warning($"?? Error llegint {categoria}.{clau} de BD: {ex.Message}. Utilitzant valor per defecte.");
                return valorPerDefecte;
            }
        }

        /// <summary>
        /// Obté una llista de valors d'una categoria (retorna les claus)
        /// </summary>
        public List<string> ObtenirLlista(string categoria)
        {
            try
            {
                var llista = _repository.ObtenirParametresPerCategoria(categoria);
                
                if (EsPrimeraLectura(categoria, "*"))
                {
                    _logger.Debug($"?? Categoria {categoria}: {llista.Count} paràmetre(s) trobat(s) (BD)");
                }
                return llista;
            }
            catch (Exception ex)
            {
                _logger.Warning($"?? Error llegint categoria {categoria} de BD: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Comprova si un valor existeix a una categoria (ja implementat al repositori)
        /// </summary>
        public bool ExisteixParametre(string categoria, string valor)
        {
            try
            {
                return _repository.ExisteixParametre(categoria, valor);
            }
            catch (Exception ex)
            {
                _logger.Warning($"?? Error comprovant {categoria}.{valor}: {ex.Message}");
                return false;
            }
        }
    }
}
