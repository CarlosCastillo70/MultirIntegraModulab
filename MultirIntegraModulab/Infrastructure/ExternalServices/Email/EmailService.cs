using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using MultirIntegraModulab.Domain.Interfaces;

namespace MultirIntegraModulab.Infrastructure.ExternalServices.Email
{
    /// <summary>
    /// Servei per enviar emails amb logs i notificacions
    /// </summary>
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsuari;
        private readonly string _smtpPassword;
        private readonly bool _usarSSL;
        private readonly string _emailFrom;
        private readonly List<string> _emailsDestinataris;
        private readonly ILoggerService _logger;
        private readonly bool _utilitzarAutenticacio;

        /// <summary>
        /// Constructor del servei d'email
        /// </summary>
        public EmailService(
            string smtpServer,
            int smtpPort,
            string smtpUsuari,
            string smtpPassword,
            bool usarSSL,
            string emailFrom,
            List<string> emailsDestinataris,
            ILoggerService logger)
        {
            _smtpServer = smtpServer ?? throw new ArgumentNullException(nameof(smtpServer));
            _smtpPort = smtpPort;
            _smtpUsuari = smtpUsuari;
            _smtpPassword = smtpPassword;
            _usarSSL = usarSSL;
            _emailFrom = emailFrom ?? throw new ArgumentNullException(nameof(emailFrom));
            _emailsDestinataris = emailsDestinataris ?? throw new ArgumentNullException(nameof(emailsDestinataris));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (!_emailsDestinataris.Any())
            {
                throw new ArgumentException("La llista de destinataris no pot estar buida", nameof(emailsDestinataris));
            }

            // Determinar si s'utilitza autenticació
            // Només utilitzem autenticació si tant l'usuari com la contrasenya tenen valors vàlids
            _utilitzarAutenticacio = !string.IsNullOrWhiteSpace(_smtpUsuari) && 
                                     !EsValorPerDefecte(_smtpUsuari) &&
                                     !string.IsNullOrWhiteSpace(_smtpPassword) &&
                                     !EsValorPerDefecte(_smtpPassword);

            if (_utilitzarAutenticacio)
            {
                _logger.Info("📧 EmailService configurat amb autenticació SMTP");
            }
            else
            {
                _logger.Info("📧 EmailService configurat sense autenticació (connexió anònima)");
            }
        }

        /// <summary>
        /// Comprova si un valor és un valor per defecte o d'exemple
        /// </summary>
        private bool EsValorPerDefecte(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return true;

            var valorsExemple = new[]
            {
                "usuari@exemple.com",
                "example@example.com",
                "PASSWORD_SMTP",
                "password",
                "changeme"
            };

            return valorsExemple.Any(ve => valor.IndexOf(ve, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Envia un email amb el fitxer de log com a adjunt
        /// </summary>
        /// <param name="subject">Assumpte de l'email</param>
        /// <param name="body">Cos de l'email</param>
        /// <param name="logFilePath">Ruta al fitxer de log (opcional)</param>
        /// <returns>True si s'ha enviat correctament</returns>
        public bool EnviarEmailAmbLog(string subject, string body, string logFilePath = null)
        {
            try
            {
                _logger.Info($"📧 Preparant enviament d'email: '{subject}'");

                using (var message = new MailMessage())
                {
                    // Configurar remitent i destinataris
                    message.From = new MailAddress(_emailFrom);
                    
                    foreach (var destinatari in _emailsDestinataris)
                    {
                        message.To.Add(new MailAddress(destinatari));
                    }

                    // Configurar contingut
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = false;
                    message.BodyEncoding = Encoding.UTF8;
                    message.SubjectEncoding = Encoding.UTF8;

                    // Afegir adjunt de log si existeix (amb retry logic)
                    if (!string.IsNullOrWhiteSpace(logFilePath) && File.Exists(logFilePath))
                    {
                        bool adjuntAfegit = false;
                        int intents = 0;
                        int maxIntents = 3;
                        
                        while (!adjuntAfegit && intents < maxIntents)
                        {
                            try
                            {
                                intents++;
                                var attachment = new Attachment(logFilePath);
                                message.Attachments.Add(attachment);
                                adjuntAfegit = true;
                                _logger.Info($"📎 Adjuntant fitxer de log: {Path.GetFileName(logFilePath)}");
                            }
                            catch (IOException ioEx) when (intents < maxIntents)
                            {
                                _logger.Warning($"⚠️ Intent {intents}/{maxIntents} - Fitxer de log encara està bloquejat. Reintentant...");
                                System.Threading.Thread.Sleep(500); // Esperar mig segon abans de reintentar
                            }
                        }
                        
                        if (!adjuntAfegit)
                        {
                            _logger.Warning($"⚠️ No s'ha pogut adjuntar el fitxer de log després de {maxIntents} intents. S'enviarà l'email sense adjunt.");
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(logFilePath))
                    {
                        _logger.Warning($"⚠️ No s'ha trobat el fitxer de log: {logFilePath}");
                    }

                    // Configurar client SMTP
                    using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        // Només configurar credencials si s'utilitza autenticació
                        if (_utilitzarAutenticacio)
                        {
                            smtpClient.Credentials = new NetworkCredential(_smtpUsuari, _smtpPassword);
                            _logger.Info($"🔐 Utilitzant autenticació SMTP amb usuari: {_smtpUsuari}");
                        }
                        else
                        {
                            smtpClient.UseDefaultCredentials = false;
                            _logger.Info($"🔓 Connexió SMTP anònima (sense autenticació)");
                        }

                        smtpClient.EnableSsl = _usarSSL;
                        smtpClient.Timeout = 30000; // 30 segons

                        // Enviar email
                        _logger.Info($"📤 Enviant email a {_emailsDestinataris.Count} destinatari(s) via {_smtpServer}:{_smtpPort}...");
                        smtpClient.Send(message);
                        
                        _logger.Info($"✅ Email enviat correctament a: {string.Join(", ", _emailsDestinataris)}");
                        return true;
                    }
                }
            }
            catch (SmtpException ex)
            {
                _logger.Error($"❌ Error SMTP enviant email: {ex.Message}", ex);
                _logger.Error($"   Codi d'estat SMTP: {ex.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Error general enviant email: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Envia un email de resum del processament amb estadístiques
        /// </summary>
        /// <param name="resum">Resum del processament</param>
        /// <param name="logFilePath">Ruta al fitxer de log (opcional)</param>
        /// <returns>True si s'ha enviat correctament</returns>
        public bool EnviarEmailResumProcessament(
            Application.DTOs.ResumProcessamentDto resum, 
            string logFilePath = null)
        {
            try
            {
                var dataExecutio = DateTime.Now;
                var subject = $"MultiR - Integració Modulab - {dataExecutio:dd/MM/yyyy HH:mm}";

                var body = GenerarCosEmailResum(resum, dataExecutio);

                return EnviarEmailAmbLog(subject, body, logFilePath);
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Error generant email de resum: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Genera el cos de l'email amb el resum del processament
        /// </summary>
        private string GenerarCosEmailResum(Application.DTOs.ResumProcessamentDto resum, DateTime dataExecutio)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=================================================");
            sb.AppendLine("    MULTIR - INTEGRACIÓ MODULAB");
            sb.AppendLine("=================================================");
            sb.AppendLine();
            sb.AppendLine($"Data d'execució: {dataExecutio:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("RESUM DEL PROCESSAMENT:");
            sb.AppendLine("-------------------");
            sb.AppendLine($"• Total processats:        {resum.TotalProcessats}");
            sb.AppendLine($"• Noves incorporacions:    {resum.NovesIncorporacions}");
            sb.AppendLine($"• Repetides:               {resum.MostresRepetides}");
            sb.AppendLine($"• Validades:               {resum.MostresValidades}");
            sb.AppendLine($"• Revalidades:             {resum.MostresRevalidades}");
            sb.AppendLine($"• Desvalidades:            {resum.MostresDesvalidades}");
            sb.AppendLine($"• Antigues:                {resum.MostresAntigues}");
            sb.AppendLine($"• Errors:                  {resum.MostresAmbError}");
            sb.AppendLine($"• Durada:                  {resum.DuradaProcessament.TotalSeconds:F2} segons");
            sb.AppendLine();

            if (resum.TotalProcessats > 0)
            {
                var percentatgeExit = ((resum.TotalProcessats - resum.MostresAmbError) * 100.0) / resum.TotalProcessats;
                sb.AppendLine($"Percentatge d'èxit: {percentatgeExit:F1}%");
            }

            sb.AppendLine();
            sb.AppendLine("=================================================");
            sb.AppendLine();
            sb.AppendLine("Per més detalls, consulta el fitxer de log adjunt.");
            sb.AppendLine();
            sb.AppendLine("--");
            sb.AppendLine("Aquest és un missatge automàtic del sistema MultiR");

            return sb.ToString();
        }

        /// <summary>
        /// Envia un email d'error crític
        /// </summary>
        /// <param name="missatgeError">Missatge d'error</param>
        /// <param name="exception">Excepció (opcional)</param>
        /// <param name="logFilePath">Ruta al fitxer de log (opcional)</param>
        /// <returns>True si s'ha enviat correctament</returns>
        public bool EnviarEmailError(string missatgeError, Exception exception = null, string logFilePath = null)
        {
            try
            {
                var dataExecutio = DateTime.Now;
                var subject = $"❌ MultiR - ERROR - {dataExecutio:dd/MM/yyyy HH:mm}";

                var body = GenerarCosEmailError(missatgeError, exception, dataExecutio);

                return EnviarEmailAmbLog(subject, body, logFilePath);
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Error enviant email d'error: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Genera el cos de l'email d'error
        /// </summary>
        private string GenerarCosEmailError(string missatgeError, Exception exception, DateTime dataExecutio)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=================================================");
            sb.AppendLine("    ❌ MULTIR - ERROR CRÍTIC");
            sb.AppendLine("=================================================");
            sb.AppendLine();
            sb.AppendLine($"Data: {dataExecutio:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("MISSATGE D'ERROR:");
            sb.AppendLine("-------------------");
            sb.AppendLine(missatgeError);
            sb.AppendLine();

            if (exception != null)
            {
                sb.AppendLine("DETALLS DE L'EXCEPCIÓ:");
                sb.AppendLine("-------------------");
                sb.AppendLine($"Tipus: {exception.GetType().Name}");
                sb.AppendLine($"Missatge: {exception.Message}");
                sb.AppendLine();
                sb.AppendLine("Stack Trace:");
                sb.AppendLine(exception.StackTrace);
                sb.AppendLine();

                if (exception.InnerException != null)
                {
                    sb.AppendLine("Inner Exception:");
                    sb.AppendLine($"  Tipus: {exception.InnerException.GetType().Name}");
                    sb.AppendLine($"  Missatge: {exception.InnerException.Message}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("=================================================");
            sb.AppendLine();
            sb.AppendLine("Si us plau, revisa el fitxer de log adjunt per més informació.");
            sb.AppendLine();
            sb.AppendLine("--");
            sb.AppendLine("Aquest és un missatge automàtic del sistema MultiR");

            return sb.ToString();
        }
    }
}
