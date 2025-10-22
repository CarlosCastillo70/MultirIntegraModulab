using System;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Representa les dades d'un pacient obtingudes de MySQL
    /// </summary>
    public class DadesPacientMySQL
    {
        public int Id { get; set; }
        public string Npat { get; set; }
        public string Nom { get; set; }
        public string Cognom1 { get; set; }
        public string Cognom2 { get; set; }
        public DateTime? DataNaixement { get; set; }
        public string Sexe { get; set; }
        public DateTime? DataCreacio { get; set; }
        public DateTime? DataActualitzacio { get; set; }
        public string Cip { get; set; }
        public string AbsReferencia { get; set; }
        public string Consolidat { get; set; }
        public string Usuari { get; set; }

        public override string ToString()
        {
            return $"Pacient {Npat}: {Nom} {Cognom1} {Cognom2} (ID: {Id})";
        }
    }
}
