using System;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Entitat que representa les dades de control de sincronització
    /// Guarda informació sobre l'última càrrega exitosa per optimitzar futures càrregues
    /// </summary>
    public class DadesSincronitzacio
    {
        /// <summary>
        /// Identificador únic del registre
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Data de resultat màxima processada en l'última càrrega exitosa
        /// S'utilitzarà per filtrar: DATA_RESULTAT > data_resultat_max_processada
        /// </summary>
        public DateTime? DataResultatMaxProcessada { get; set; }

        /// <summary>
        /// Data de validació màxima processada en l'última càrrega exitosa
        /// S'utilitzarà per filtrar: DATA_VALIDACIO > data_validacio_max_processada
        /// </summary>
        public DateTime? DataValidacioMaxProcessada { get; set; }

        /// <summary>
        /// Data i hora en què es va realitzar la sincronització
        /// </summary>
        public DateTime DataSincronitzacio { get; set; }

        /// <summary>
        /// Nombre de mostres processades en aquesta sincronització
        /// </summary>
        public int NombreMostresProcessades { get; set; }

        /// <summary>
        /// Nombre de mostres amb error en aquesta sincronització
        /// </summary>
        public int NombreMostresError { get; set; }

        /// <summary>
        /// Dies de revisió de seguretat per validacions tardanes
        /// Per defecte 7 dies
        /// </summary>
        public int DiesRevisioSeguretat { get; set; }

        /// <summary>
        /// Estat de la sincronització: OK, ERROR, PARCIAL
        /// </summary>
        public string Estat { get; set; }

        /// <summary>
        /// Observacions adicionals sobre la sincronització
        /// </summary>
        public string Observacions { get; set; }

        /// <summary>
        /// Durada del processament en segons
        /// </summary>
        public double? DuradaSegons { get; set; }

        public DadesSincronitzacio()
        {
            DataSincronitzacio = DateTime.Now;
            DiesRevisioSeguretat = 7; // Per defecte 7 dies
            Estat = "OK";
        }

        public override string ToString()
        {
            return $"Sincronització {Id}: {DataSincronitzacio:dd/MM/yyyy HH:mm} - " +
                   $"{NombreMostresProcessades} mostres - Estat: {Estat}";
        }
    }
}