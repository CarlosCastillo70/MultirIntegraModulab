using System;
using System.Collections.Generic;
using System.Linq;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Application.Helpers;

namespace MultirIntegraModulab.Infrastructure.Configuration
{
    /// <summary>
    /// Servei de configuració híbrid que llegeix paràmetres funcionals de BD
    /// i paràmetres tècnics d'App.config
    /// 
    /// PARÀMETRES A BD (CONFIG_GENERAL):
    /// - DIES_VIGENCIA_POSITIUS_DEFAULT
    /// - EMAIL_FROM
    /// - EMAIL_RESUM_CARREGA
    /// - HABILITAR_NOTIFICACIONS_EMAIL
    /// </summary>
    public class ConfigurationServiceHibrid : ConfigurationService
    {
        private readonly ParametresHelper _parametresHelper;

        public ConfigurationServiceHibrid(IMultiRRepository repository, ILoggerService logger) 
            : base()
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            
            _parametresHelper = new ParametresHelper(repository, logger, this);
        }

        #region Configuració de Cache

        /// <summary>
        /// MIGRAT A BD: Dies de vigència de positius per defecte
        /// Utilitzat per Comprovació 2 (negatius) quan tipus_mostra no té dies_vigencia_positiu definit
        /// </summary>
        public override int MinutsVigenciaCache
        {
            get
            {
                // Aquest paràmetre es manté a App.config (tècnic)
                return base.MinutsVigenciaCache;
            }
        }

        #endregion

        #region Configuració de Manteniment

        /// <summary>
        /// MIGRAT A BD: Dies de vigència de positius per defecte
        /// Criteri epidemiològic/clínic que pot variar segons protocols
        /// </summary>
        public override int DiesRetencioHistorial
        {
            get
            {
                // Guard contra crida durant construcció abans que _parametresHelper s'inicialitzi
                if (_parametresHelper == null)
                {
                    return base.DiesRetencioHistorial;
                }

                // Llegir de BD primer, si no hi és, utilitzar el valor d'App.config
                int valorBD = _parametresHelper.ObtenirInt(
                    "CONFIG_GENERAL", 
                    "DIES_VIGENCIA_POSITIUS_DEFAULT", 
                    -1);  // -1 indica que no existeix a BD
                
                if (valorBD > 0)
                {
                    return valorBD;
                }
                
                // Fallback a App.config
                return base.DiesRetencioHistorial;
            }
        }

        #endregion

        #region Configuració d'Email

        /// <summary>
        /// MIGRAT A BD: Email remitent per notificacions del sistema
        /// Pot variar segons organització/departament
        /// </summary>
        public override string EmailFrom
        {
            get
            {
                // Guard contra crida durant construcció abans que _parametresHelper s'inicialitzi
                if (_parametresHelper == null)
                {
                    return base.EmailFrom;
                }

                // Llegir de BD primer amb el nou paràmetre EMAIL_FROM
                string valorBD = _parametresHelper.ObtenirString(
                    "CONFIG_GENERAL", 
                    "EMAIL_FROM", 
                    null);
                
                if (!string.IsNullOrEmpty(valorBD))
                {
                    return valorBD;
                }
                
                // Fallback a App.config
                return base.EmailFrom;
            }
        }

        /// <summary>
        /// MIGRAT A BD: Emails destinataris per notificacions de resum de càrrega
        /// Pot variar segons organització/departament
        /// Format a BD: emails separats per punt i coma (;)
        /// </summary>
        public override List<string> EmailsDestinataris
        {
            get
            {
                // Guard contra crida durant construcció abans que _parametresHelper s'inicialitzi
                if (_parametresHelper == null)
                {
                    return base.EmailsDestinataris;
                }

                // Llegir de BD primer amb el paràmetre EMAIL_RESUM_CARREGA
                string valorBD = _parametresHelper.ObtenirString(
                    "CONFIG_GENERAL", 
                    "EMAIL_RESUM_CARREGA", 
                    null);
                
                if (!string.IsNullOrEmpty(valorBD))
                {
                    // Dividir per ; i retornar com a llista
                    return valorBD.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(e => e.Trim())
                                  .Where(e => !string.IsNullOrWhiteSpace(e))
                                  .ToList();
                }
                
                // Fallback a App.config
                return base.EmailsDestinataris;
            }
        }

        /// <summary>
        /// MIGRAT A BD: Habilitar enviar emails automàtics
        /// Decisió organitzativa que pot canviar
        /// </summary>
        public override bool EnviarEmailLog
        {
            get
            {
                // Guard contra crida durant construcció abans que _parametresHelper s'inicialitzi
                if (_parametresHelper == null)
                {
                    return base.EnviarEmailLog;
                }

                // Llegir de BD primer
                // Utilitzem ObtenirBool directament (ja gestiona la conversió internament)
                try
                {
                    // Comprovar primer si el paràmetre existeix a BD
                    string valorBD = _parametresHelper.ObtenirString(
                        "CONFIG_GENERAL", 
                        "HABILITAR_NOTIFICACIONS_EMAIL", 
                        null);
                    
                    if (!string.IsNullOrEmpty(valorBD))
                    {
                        // El paràmetre existeix, obtenir-lo com a bool
                        // Això NO generarà log duplicat perquè ParametresHelper té cache
                        return _parametresHelper.ObtenirBool(
                            "CONFIG_GENERAL", 
                            "HABILITAR_NOTIFICACIONS_EMAIL", 
                            false);
                    }
                }
                catch
                {
                    // Si hi ha error, continuar amb fallback
                }
                
                // Fallback a App.config
                return base.EnviarEmailLog;
            }
        }

        /// <summary>
        /// Paràmetres SMTP es mantenen a App.config (credencials sensibles)
        /// </summary>
        public override string SmtpServer => base.SmtpServer;
        public override int SmtpPort => base.SmtpPort;
        public override string SmtpUsuari => base.SmtpUsuari;
        public override string SmtpPassword => base.SmtpPassword;
        public override bool SmtpUsarSSL => base.SmtpUsarSSL;
        public override bool EmailNomesEnErrors => base.EmailNomesEnErrors;

        #endregion

        /// <summary>
        /// Obté un resum de la configuració incloent paràmetres de BD
        /// </summary>
        public override string ObtenirResumConfiguracio()
        {
            var resum = base.ObtenirResumConfiguracio();
            
            resum += "\n\n=== PARÀMETRES DE BASE DE DADES ===\n";
            resum += $"Dies vigència positius (BD):      {DiesRetencioHistorial} dies\n";
            resum += $"Email remitent (BD):               {EmailFrom}\n";
            resum += $"Emails destinataris (BD):          {string.Join("; ", EmailsDestinataris)}\n";
            resum += $"Habilitar emails (BD):             {(EnviarEmailLog ? "Activat" : "Desactivat")}\n";
            
            return resum;
        }
    }
}
