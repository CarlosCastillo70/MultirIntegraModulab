using System;
using System.Threading.Tasks;
using MultirIntegraModulab.Domain.Entities;
using MultirIntegraModulab.Domain.Interfaces;

namespace MultirIntegraModulab.Infrastructure.ExternalServices.Pacient
{
    /// <summary>
    /// Adapter que implementa la interfície IPacientWebService utilitzant el servei legacy PacientWebService
    /// </summary>
    public class PacientWebServiceAdapter : IPacientWebService
    {
        private readonly PacientWebService _pacientWebService;
        private readonly ILoggerService _logger;

        public PacientWebServiceAdapter(string webServiceUrl, ILoggerService logger)
        {
            _pacientWebService = new PacientWebService(webServiceUrl);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obté informació d'un pacient (retorna object per compatibilitat)
        /// </summary>
        public object ObtenirPacient(string pacientId)
        {
            try
            {
                var task = _pacientWebService.ConsultarPacientAsync(pacientId);
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error obtenint pacient {pacientId}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Obté les dades completes d'un pacient
        /// </summary>
        public DadesPacient ObtenirDadesPacient(string pacientId)
        {
            try
            {
                var task = _pacientWebService.ConsultarPacientAsync(pacientId);
                task.Wait();
                var dadesPacientWs = task.Result;

                if (dadesPacientWs == null)
                {
                    return null;
                }

                // Convertir de DadesPacientWebService a DadesPacient (entitat del domini)
                return new DadesPacient
                {
                    PacientSap = dadesPacientWs.PacientSap,
                    Nom = dadesPacientWs.Nom,
                    Cognom1 = dadesPacientWs.Cognom1,
                    Cognom2 = dadesPacientWs.Cognom2,
                    DataNaixement = dadesPacientWs.DataNaixement,
                    Sexe = dadesPacientWs.Sexe,
                    Cip = dadesPacientWs.Cip,
                    Abs = dadesPacientWs.Abs
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error obtenint dades del pacient {pacientId}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Valida si un pacient existeix
        /// </summary>
        public bool ValidarPacient(string pacientId)
        {
            var dadesPacient = ObtenirDadesPacient(pacientId);
            return dadesPacient != null;
        }
    }
}
