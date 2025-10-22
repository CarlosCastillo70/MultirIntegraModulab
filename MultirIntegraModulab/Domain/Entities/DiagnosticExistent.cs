using System;
using System.Collections.Generic;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Representa un diagnòstic existent a la base de dades MySQL
    /// </summary>
    public class DiagnosticExistent
    {
        public int Id { get; set; }
        public string PacientSap { get; set; }
        public string ClauDiagnostic { get; set; }
        public string TipusMostra { get; set; }
        public string Microorganisme { get; set; }
        public string MecanismeResistencia { get; set; }
        public DateTime DataCreacio { get; set; }
        public DateTime? DataActualitzacio { get; set; }
        public bool Actiu { get; set; }
        public List<string> EtiquetesMostres { get; set; }

        public DiagnosticExistent()
        {
            EtiquetesMostres = new List<string>();
            Actiu = true;
        }

        /// <summary>
        /// Comprova si aquest diagnòstic conté una etiqueta específica
        /// </summary>
        /// <param name="etiquetaId">Etiqueta a comprovar</param>
        /// <returns>True si la conté, False en cas contrari</returns>
        public bool ConteEtiqueta(string etiquetaId)
        {
            return EtiquetesMostres.Contains(etiquetaId);
        }

        /// <summary>
        /// Afegeix una etiqueta al diagnòstic si no existeix
        /// </summary>
        /// <param name="etiquetaId">Etiqueta a afegir</param>
        /// <returns>True si s'ha afegit, False si ja existia</returns>
        public bool AfegirEtiqueta(string etiquetaId)
        {
            if (!ConteEtiqueta(etiquetaId))
            {
                EtiquetesMostres.Add(etiquetaId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Elimina una etiqueta del diagnòstic
        /// </summary>
        /// <param name="etiquetaId">Etiqueta a eliminar</param>
        /// <returns>True si s'ha eliminat, False si no existia</returns>
        public bool EliminarEtiqueta(string etiquetaId)
        {
            return EtiquetesMostres.Remove(etiquetaId);
        }

        public override string ToString()
        {
            return $"Diagnòstic #{Id} - {PacientSap} - {ClauDiagnostic} ({EtiquetesMostres.Count} mostres)";
        }
    }
}