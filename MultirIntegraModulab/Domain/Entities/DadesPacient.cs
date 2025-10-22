using System;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Representa les dades d'un pacient obtingudes del web service
    /// </summary>
    public class DadesPacient
    {
        public string PacientSap { get; set; }
        public string Nom { get; set; }
        public string Cognom1 { get; set; }
        public string Cognom2 { get; set; }
        public DateTime? DataNaixement { get; set; }
        public string Sexe { get; set; }
        public string Cip { get; set; }
        public string Abs { get; set; }

        public override string ToString()
        {
            return $"Pacient {PacientSap}: {Nom} {Cognom1} {Cognom2} (CIP: {Cip})";
        }
    }
}
