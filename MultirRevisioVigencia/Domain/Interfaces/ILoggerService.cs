using System;

namespace MultirRevisioVigencia.Domain.Interfaces
{
    /// <summary>
    /// Interfície per al servei de logging
    /// </summary>
    public interface ILoggerService
    {
        void Info(string missatge);
        void Warning(string missatge);
        void Error(string missatge, Exception exception = null);
        string GetLogFilePath();
    }
}
