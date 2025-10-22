using System;

namespace MultirIntegraModulab
{
    public interface IDbService
    {
        object GetCurrentDate();
        string GetDatabaseType();
        int GetTableRecordCount(string tableName);
    }
}