using System;
using System.Collections.Generic;
using MultirRevisioVigencia.Application.DTOs;

namespace MultirRevisioVigencia.Domain.Interfaces
{
    /// <summary>
    /// Interfície per al repositori de la base de dades MultiR
    /// </summary>
    public interface IMultiRRepository
    {
        /// <summary>
        /// Valida la connexió amb la base de dades
        /// </summary>
        bool ValidarConnexio();

        /// <summary>
        /// Obté els diagnòstics vigents que poden haver caducat
        /// </summary>
        List<DiagnosticPerRevisar> ObtenirDiagnosticsVigentsPerRevisar();

        /// <summary>
        /// Marca un diagnòstic com a no vigent
        /// </summary>
        bool MarcarDiagnosticNoVigent(int diagnosticId, string responsable, string motiu = null);

        /// <summary>
        /// Reactiva un diagnòstic marcant-lo com a vigent
        /// </summary>
        bool ReactivarDiagnostic(int diagnosticId, string responsable, string motiu = null);

        /// <summary>
        /// Obté la regla de tipus de mostra per un microorganisme i mecanisme
        /// </summary>
        ReglaTipusMostra ObtenirReglaTipusMostra(string microorganisme, string mecanisme);

        /// <summary>
        /// Obté les mostres positives d'un diagnòstic
        /// </summary>
        List<MostraPositivaDiagnostic> ObtenirMostresPositivesDiagnostic(int diagnosticId);

        /// <summary>
        /// Obté totes les mostres (positives i negatives) d'un diagnòstic posteriors a la data de diagnòstic
        /// </summary>
        List<MostraDiagnostic> ObtenirMostresDiagnostic(int diagnosticId, DateTime dataDiagnostic);
    }
}
