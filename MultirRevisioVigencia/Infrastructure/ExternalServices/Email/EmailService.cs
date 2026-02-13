using MultirRevisioVigencia.Application.DTOs;
using MultirRevisioVigencia.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace MultirRevisioVigencia.Infrastructure.ExternalServices.Email
{
    /// <summary>
    /// Servei per enviar emails amb resum de la revisió de vigència
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
            _logger = logger;

            if (!_emailsDestinataris.Any())
            {
                throw new ArgumentException("La llista de destinataris no pot estar buida", nameof(emailsDestinataris));
            }

            // Determinar si s'utilitza autenticació
            _utilitzarAutenticacio = !string.IsNullOrWhiteSpace(_smtpUsuari) && 
                                     !EsValorPerDefecte(_smtpUsuari) &&
                                     !string.IsNullOrWhiteSpace(_smtpPassword) &&
                                     !EsValorPerDefecte(_smtpPassword);
        }

        private void Log(string missatge, string tipus = "INFO")
        {
            if (_logger != null)
            {
                switch (tipus.ToUpper())
                {
                    case "WARNING":
                        _logger.Warning(missatge);
                        break;
                    case "ERROR":
                        _logger.Error(missatge);
                        break;
                    default:
                        _logger.Info(missatge);
                        break;
                }
            }
            else
            {
                Console.WriteLine($"[{tipus}] {missatge}");
            }
        }

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
        /// Envia un email amb el resum de la revisió de vigència
        /// </summary>
        public bool EnviarEmailResumRevisio(ResumRevisioVigenciaDto resum, string logFilePath = null)
        {
            try
            {
                var dataExecutio = resum.DataRevisio;
                var subject = $"MultiR - Revisió Vigència Diagnòstics - {dataExecutio:dd/MM/yyyy HH:mm}";
                var body = GenerarCosEmailResum(resum);
                return EnviarEmailAmbLog(subject, body, logFilePath);
            }
            catch (Exception ex)
            {
                Log($"❌ Error generant email de resum: {ex.Message}", "ERROR");
                return false;
            }
        }

        /// <summary>
        /// Envia un email d'error crític
        /// </summary>
        public bool EnviarEmailError(string missatgeError, Exception exception = null, string logFilePath = null)
        {
            try
            {
                var dataExecutio = DateTime.Now;
                var subject = $"❌ MultiR - ERROR Revisió Vigència - {dataExecutio:dd/MM/yyyy HH:mm}";
                var body = GenerarCosEmailError(missatgeError, exception, dataExecutio);
                return EnviarEmailAmbLog(subject, body, logFilePath);
            }
            catch (Exception ex)
            {
                Log($"❌ Error enviant email d'error: {ex.Message}", "ERROR");
                return false;
            }
        }

        private bool EnviarEmailAmbLog(string subject, string body, string logFilePath = null)
        {
            try
            {
                Log($"📧 Preparant enviament d'email: '{subject}'");

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_emailFrom);
                    
                    foreach (var destinatari in _emailsDestinataris)
                    {
                        message.To.Add(new MailAddress(destinatari));
                    }

                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = false;
                    message.BodyEncoding = Encoding.UTF8;
                    message.SubjectEncoding = Encoding.UTF8;

                    // Afegir adjunt de log si existeix
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
                                Log($"📎 Adjuntant fitxer de log: {Path.GetFileName(logFilePath)}");
                            }
                            catch (IOException ioEx) when (intents < maxIntents)
                            {
                                Log($"⚠️ Intent {intents}/{maxIntents} - Fitxer de log encara bloquejat. Reintentant...", "WARNING");
                                System.Threading.Thread.Sleep(500);
                            }
                        }
                        
                        if (!adjuntAfegit)
                        {
                            Log($"⚠️ No s'ha pogut adjuntar el fitxer després de {maxIntents} intents. S'enviarà sense adjunt.", "WARNING");
                        }
                    }

                    using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        if (_utilitzarAutenticacio)
                        {
                            smtpClient.Credentials = new NetworkCredential(_smtpUsuari, _smtpPassword);
                            Log($"🔐 Autenticació SMTP: {_smtpUsuari}");
                        }
                        else
                        {
                            smtpClient.UseDefaultCredentials = false;
                            Log($"🔓 Connexió SMTP anònima");
                        }

                        smtpClient.EnableSsl = _usarSSL;
                        smtpClient.Timeout = 30000;

                        Log($"📤 Enviant email a {_emailsDestinataris.Count} destinatari(s) via {_smtpServer}:{_smtpPort}...");
                        smtpClient.Send(message);
                        
                        Log($"✅ Email enviat a: {string.Join(", ", _emailsDestinataris)}");
                        return true;
                    }
                }
            }
            catch (SmtpException ex)
            {
                Log($"❌ Error SMTP: {ex.Message} (Codi: {ex.StatusCode})", "ERROR");
                return false;
            }
            catch (Exception ex)
            {
                Log($"❌ Error enviant email: {ex.Message}", "ERROR");
                return false;
            }
        }

        private string GenerarCosEmailResum(ResumRevisioVigenciaDto resum)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=================================================");
            sb.AppendLine("    MULTIR - REVISIÓ VIGÈNCIA DIAGNÒSTICS");
            sb.AppendLine("=================================================");
            sb.AppendLine();
            sb.AppendLine($"Data de revisió: {resum.DataRevisio:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("RESUM DE LA REVISIÓ:");
            sb.AppendLine("-------------------");
            sb.AppendLine($"• Total revisats:               {resum.TotalRevisats}");
            sb.AppendLine($"• Marcats com a no vigents:     {resum.MarcatsNoVigents}");
            sb.AppendLine($"• Errors:                       {resum.Errors}");
            sb.AppendLine();

            if (resum.DiagnosticsMarcats.Any())
            {
                sb.AppendLine("DIAGNÒSTICS MARCATS COM A NO VIGENTS:");
                sb.AppendLine("-------------------------------------");
                
                foreach (var diagnostic in resum.DiagnosticsMarcats)
                {
                    sb.AppendLine($"• ID {diagnostic.DiagnosticId} - Pacient: {diagnostic.PacientSap}");
                    sb.AppendLine($"  Microorganisme: {diagnostic.Microorganisme}");
                    sb.AppendLine($"  Mecanisme: {diagnostic.Mecanisme}");
                    sb.AppendLine($"  Última mostra: {diagnostic.DataUltimaMostra:dd/MM/yyyy}");
                    sb.AppendLine($"  Dies vigència: {diagnostic.DiesVigencia}");
                    sb.AppendLine($"  Motiu: {diagnostic.Motiu}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("=================================================");
            sb.AppendLine();
            sb.AppendLine("Per més detalls, consulta el fitxer de log adjunt.");
            sb.AppendLine();
            sb.AppendLine("--");
            sb.AppendLine("Aquest és un missatge automàtic del sistema MultiR");
            return sb.ToString();
        }

        private string GenerarCosEmailError(string missatgeError, Exception exception, DateTime dataExecutio)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=================================================");
            sb.AppendLine("    ❌ MULTIR - ERROR REVISIÓ VIGÈNCIA");
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
