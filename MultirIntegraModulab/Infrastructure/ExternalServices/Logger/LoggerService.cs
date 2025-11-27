using System;
using MultirIntegraModulab.Domain.Interfaces;

namespace MultirIntegraModulab.Infrastructure.ExternalServices.Logger
{
    /// <summary>
    /// Implementació del servei de logging
    /// Adapta la classe Logger estàtica a la interfície del domini
    /// </summary>
    public class LoggerService : ILoggerService
    {
        public LoggerService()
        {
            // El Logger és estàtic, no necessita inicialització
        }

        public void Info(string missatge)
        {
            MultirIntegraModulab.Logger.Info(missatge);
        }

        public void Warning(string missatge)
        {
            MultirIntegraModulab.Logger.Warning(missatge);
        }

        public void Error(string missatge, Exception ex = null)
        {
            if (ex != null)
            {
                MultirIntegraModulab.Logger.Error(missatge, ex);
            }
            else
            {
                MultirIntegraModulab.Logger.Error(missatge);
            }
        }

        public void Debug(string missatge)
        {
            MultirIntegraModulab.Logger.Debug(missatge);
        }

        public void MarcarIniciExecucio()
        {
            MultirIntegraModulab.Logger.MarcarIniciExecucio();
        }

        public void MarcarFinalExecucio()
        {
            MultirIntegraModulab.Logger.MarcarFinalExecucio();
        }

        public void FlushLogs()
        {
            MultirIntegraModulab.Logger.FlushLogs();
        }

        public string ObtenirRutaLogAvui()
        {
            return MultirIntegraModulab.Logger.ObtenirRutaLogAvui();
        }

        public bool ExisteixLogAvui()
        {
            return MultirIntegraModulab.Logger.ExisteixLogAvui();
        }

        public long ObtenirMidaLogAvui()
        {
            return MultirIntegraModulab.Logger.ObtenirMidaLogAvui();
        }
    }
}
