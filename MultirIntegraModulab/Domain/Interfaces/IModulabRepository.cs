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
        /// Carrega resultats de mostres dels últims X dies enrere
        /// </summary>
        ColeccioMostres CarregarResultatsDiesEndarrera(int diesEndarrera, int limit = 0);

        /// <summary>
        /// Carrega resultats per un pacient específic
        /// </summary>
        ColeccioMostres CarregarResultatsPerPacient(string pacientSap, int diesEndarrera, int limit = 0);

        /// <summary>
        /// Carrega resultats per rang de dates
        /// </summary>
        ColeccioMostres CarregarResultatsPerRangDates(DateTime dataInici, DateTime dataFi, int limit = 0);

        /// <summary>
        /// Carrega resultats de forma incremental utilitzant filtres de darreres dates de resultat i validació
        /// Implementa la finestra de seguretat per validacions tardanes
        /// </summary>
        /// <param name="dadesSincronitzacio">Dades de l'última sincronització (null si és la primera)</param>
        /// <param name="limit">Límit de registres (0 = sense límit)</param>
        /// <returns>Col·lecció de mostres filtrades</returns>
        ColeccioMostres CarregarResultatsIncremental(DadesSincronitzacio dadesSincronitzacio, int limit = 0);

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
