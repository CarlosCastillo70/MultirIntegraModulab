using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;

namespace MultirIntegraModulab.Application.Services
{
    public class NotaCursClinicService
    {
        private readonly ILoggerService _logger;

        public NotaCursClinicService(ILoggerService logger)
        {
            _logger = logger;
        }

        public string ConfeccionarNota(List<DiagnosticActiuPacient> diagnostics)
        {
            if (diagnostics == null || !diagnostics.Any())
            {
                return "No hi ha diagnòstics actius.";
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== DIAGNÒSTICS ACTIUS ===");
            sb.AppendLine();

            foreach (var diagnostic in diagnostics)
            {
                sb.AppendLine($"Diagnòstic:");
                
                if (diagnostic.DataDarrerPositiu.HasValue)
                {
                    sb.AppendLine($"  - Data darrer positiu: {diagnostic.DataDarrerPositiu.Value:dd/MM/yyyy}");
                }
                
                if (!string.IsNullOrWhiteSpace(diagnostic.DescripcioTipusMostra))
                {
                    sb.AppendLine($"  - Tipus mostra: {diagnostic.DescripcioTipusMostra}");
                }
                
                sb.AppendLine($"  - Microorganisme: {diagnostic.Microorganisme}");
                
                if (!string.IsNullOrWhiteSpace(diagnostic.Mecanisme))
                {
                    sb.AppendLine($"  - Mecanisme resistència: {diagnostic.Mecanisme}");
                }

                // Notes especials
                List<string> notes = new List<string>();
                
                if (diagnostic.MicroorganismeNotaCursClinic.HasValue)
                {
                    if (diagnostic.MicroorganismeNotaCursClinic.Value == 1)
                    {
                        notes.Add("Microorganisme amb nota curs clínic (tipus 1: general)");
                    }
                    else if (diagnostic.MicroorganismeNotaCursClinic.Value == 2)
                    {
                        notes.Add("Microorganisme amb nota curs clínic (tipus 2: àrees crítiques)");
                    }
                }
                
                if (diagnostic.MecanismeNotaCursClinic.HasValue)
                {
                    if (diagnostic.MecanismeNotaCursClinic.Value == 1)
                    {
                        notes.Add("Mecanisme amb nota curs clínic (tipus 1: general)");
                    }
                    else if (diagnostic.MecanismeNotaCursClinic.Value == 2)
                    {
                        notes.Add("Mecanisme amb nota curs clínic (tipus 2: àrees crítiques)");
                    }
                }

                if (notes.Any())
                {
                    sb.AppendLine($"  - Observacions: {string.Join(", ", notes)}");
                }

                sb.AppendLine();
            }

            sb.AppendLine($"Total diagnòstics actius: {diagnostics.Count}");

            return sb.ToString();
        }
    }
}
