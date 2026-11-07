using HomeNetOrm.Exeptions;
using HomeNetOrm.Interfaces.DbInfrastructure;
using Microsoft.Data.Sqlite;

namespace HomeNetOrm.DBProviders.Sqlite
{
    public class SqliteExceptionHandler : IDbExceptionHandler
    {
        public bool IsDuplicateKeyException(Exception ex)
            => ex is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19;

        public Exception Translate(Exception ex)
        {
            if (ex is SqliteException sqliteEx)
            {
                return sqliteEx.SqliteErrorCode == 19
                    ? new DuplicateEmailException("")
                    : new DatabaseException($"Ошибка SQLite: {sqliteEx.Message}", ex);
            }
            return new DatabaseException($"Неизвестная ошибка БД: {ex.Message}", ex);
        }
    }
}