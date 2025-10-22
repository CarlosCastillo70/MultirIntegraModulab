using System;

namespace MultirIntegraModulab.Domain.Entities
{
    /// <summary>
    /// Representa un resultat individual d'una mostra de laboratori
    /// </summary>
    public class ResultatMostra
    {
        /// <summary>
        /// Identificador del resultat
        /// </summary>
        public string EtiquetaId { get; set; }

        /// <summary>
        /// Identificador del pacient
        /// </summary>
        public string PacientSap { get; set; }

        /// <summary>
        /// CIP del pacient
        /// </summary>
        public string Cip { get; set; }

        /// <summary>
        /// Número de col·legiat del metge sol·licitant
        /// </summary>
        public string ColegiatId { get; set; }

        /// <summary>
        /// Nom del metge sol·licitant
        /// </summary>
        public string NomMetge { get; set; }

        /// <summary>
        /// Centre on s'ha fet la sol·licitud
        /// </summary>
        public string CentreDescripcio { get; set; }

        /// <summary>
        /// Data de la sol·licitud
        /// </summary>
        public DateTime? DataPeticioTrunc { get; set; }

        /// <summary>
        /// Servei que ha fet la sol·licitud
        /// </summary>
        public string ServeiDescripcio { get; set; }

        /// <summary>
        /// Microorganisme que es vol analitzar
        /// </summary>
        public string AillamentDescripcio { get; set; }

        /// <summary>
        /// Indica si el microorganisme és especial (basat en la consulta a MySQL)
        /// </summary>
        public bool? EsMicroorganismeEspecial { get; set; }

        /// <summary>
        /// Primer mecanisme de resistència al microorganisme (pot ser null)
        /// </summary>
        public string MecanismeResistencia1Id { get; set; }

        /// <summary>
        /// Descripció del primer mecanisme de resistència (pot ser null)
        /// </summary>
        public string MecanismeResistenciaDescrip { get; set; }

        /// <summary>
        /// Segon mecanisme de resistència al microorganisme (pot ser null)
        /// </summary>
        public string MecanismeResistencia2Id { get; set; }

        /// <summary>
        /// Descripció del segon mecanisme de resistència (pot ser null)
        /// </summary>
        public string MecanismeResistenciaDescrip2 { get; set; }

        /// <summary>
        /// Tercer mecanisme de resistència al microorganisme (pot ser null)
        /// </summary>
        public string MecanismeResistencia3Id { get; set; }

        /// <summary>
        /// Descripció del tercer mecanisme de resistència (pot ser null)
        /// </summary>
        public string MecanismeResistenciaDescrip3 { get; set; }

        /// <summary>
        /// Quart mecanisme de resistència al microorganisme (pot ser null)
        /// </summary>
        public string MecanismeResistencia4Id { get; set; }

        /// <summary>
        /// Descripció del quart mecanisme de resistència (pot ser null)
        /// </summary>
        public string MecanismeResistenciaDescrip4 { get; set; }

        /// <summary>
        /// Cinquè mecanisme de resistència al microorganisme (pot ser null)
        /// </summary>
        public string MecanismeResistencia5Id { get; set; }

        /// <summary>
        /// Descripció del cinquè mecanisme de resistència (pot ser null)
        /// </summary>
        public string MecanismeResistenciaDescrip5 { get; set; }

        /// <summary>
        /// Tipus de prova que s'ha fet
        /// </summary>
        public string ProvaDescripcio { get; set; }

        /// <summary>
        /// Tipus de mostra que s'ha fet servir
        /// </summary>
        public string MostraDescripcio { get; set; }

        /// <summary>
        /// Data del resultat de la prova
        /// </summary>
        public DateTime DataResultat { get; set; }

        /// <summary>
        /// Data en què s'ha validat la prova (null si encara no ha estat validada)
        /// </summary>
        public DateTime? DataValidacio { get; set; }

        /// <summary>
        /// Indica si la prova ha estat validada per un professional
        /// </summary>
        public bool EstaValidada => DataValidacio.HasValue;

        public override string ToString()
        {
            string especial = EsMicroorganismeEspecial.HasValue 
                ? (EsMicroorganismeEspecial.Value ? " [ESPECIAL]" : "") 
                : " [DESCONEGUT]";
            
            return $"Etiqueta: {EtiquetaId}, Pacient: {PacientSap}, Metge: {NomMetge}, Centre: {CentreDescripcio}, Prova: {ProvaDescripcio}, Data: {DataResultat:dd/MM/yyyy}{especial}";
        }
    }
}