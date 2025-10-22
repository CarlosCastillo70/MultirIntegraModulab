using System.Threading.Tasks;
using MultirIntegraModulab.Application.DTOs;
using MultirIntegraModulab.Domain.Entities;

namespace MultirIntegraModulab.Application.Interfaces
{
    /// <summary>
    /// Interfície per al servei de processament de mostres
    /// Aquesta és la capa d'aplicació que coordina els Use Cases
    /// </summary>
    public interface IProcessamentMostresService
    {
        /// <summary>
        /// Processa una col·lecció de mostres
        /// </summary>
        /// <param name="mostres">Col·lecció de mostres a processar</param>
        /// <returns>Resum del processament</returns>
        Task<ResumProcessamentDto> ProcessarMostresAsync(ColeccioMostres mostres);

        /// <summary>
        /// Processa una mostra individual
        /// </summary>
        /// <param name="mostra">Mostra a processar</param>
        /// <returns>True si s'ha processat correctament</returns>
        Task<bool> ProcessarMostraAsync(Mostra mostra);

        /// <summary>
        /// Valida una mostra abans de processar-la
        /// </summary>
        /// <param name="mostra">Mostra a validar</param>
        /// <returns>True si la mostra és vàlida</returns>
        bool ValidarMostra(Mostra mostra);
    }
}
