using System;
using MultirIntegraModulab.Domain.Entities;

namespace MultirIntegraModulab.Domain.Interfaces
{
    /// <summary>
    /// Port (interfície) per accedir a les dades de Modulab (Oracle)
    /// Seguint el principi de Dependency Inversion (SOLID)
    /// </summary>
    public interface IModulabRepository
    {
        /// <summary>
        /// Carrega resultats de mostres dels últims X dies
        /// </summary>
        ColeccioMostres CarregarResultats(int diesEndarrera, int limit = 0);

        /// <summary>
        /// Carrega resultats per un pacient específic
        /// </summary>
        ColeccioMostres CarregarResultatsPerPacient(string pacientSap, int diesEndarrera, int limit = 0);

        /// <summary>
        /// Carrega resultats per rang de dates
        /// </summary>
        ColeccioMostres CarregarResultatsPerRangDates(DateTime dataInici, DateTime dataFi);

        /// <summary>
        /// Obté la data actual del sistema Oracle
        /// </summary>
        DateTime GetCurrentDate();

        /// <summary>
        /// Obté el tipus de base de dades
        /// </summary>
        string GetDatabaseType();
    }
}
