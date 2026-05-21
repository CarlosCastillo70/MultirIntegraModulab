using System;

namespace MultirIntegraModulab.Domain.Interfaces
{
    /// <summary>
    /// Port (interfície) per al sistema de logging
    /// Permet canviar la implementació sense afectar el domini
    /// </summary>
    public interface ILoggerService
    {
        void Info(string missatge);
        void Warning(string missatge);
        void Error(string missatge, Exception ex = null);
        void Debug(string missatge);
        void MarcarIniciExecucio();
        void MarcarFinalExecucio();
        void FlushLogs();
        string ObtenirRutaLogAvui();
        bool ExisteixLogAvui();
        long ObtenirMidaLogAvui();
        
        /// <summary>
        /// Genera logs de prova amb diferents nivells per configurar Seq
        /// </summary>
        void GenerarLogsDeProva();
    }
}
