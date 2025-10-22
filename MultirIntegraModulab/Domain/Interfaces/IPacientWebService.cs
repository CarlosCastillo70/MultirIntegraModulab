using MultirIntegraModulab.Domain.Entities;

namespace MultirIntegraModulab.Domain.Interfaces
{
    /// <summary>
    /// Port (interfície) per al servei extern de pacients
    /// Permet canviar la implementació del web service sense afectar el domini
    /// </summary>
    public interface IPacientWebService
    {
        /// <summary>
        /// Obté informació d'un pacient
        /// </summary>
        /// <param name="pacientId">Identificador del pacient</param>
        /// <returns>Dades del pacient</returns>
        object ObtenirPacient(string pacientId);

        /// <summary>
        /// Obté les dades completes d'un pacient
        /// </summary>
        /// <param name="pacientId">Identificador del pacient</param>
        /// <returns>Dades del pacient o null si no es troba</returns>
        DadesPacient ObtenirDadesPacient(string pacientId);

        /// <summary>
        /// Valida si un pacient existeix
        /// </summary>
        bool ValidarPacient(string pacientId);
    }
}
