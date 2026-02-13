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
    }
}
