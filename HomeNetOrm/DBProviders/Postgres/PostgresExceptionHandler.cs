using HomeNetOrm.Exeptions;
using HomeNetOrm.Interfaces.DbInfrastructure;
using Npgsql;

namespace HomeNetOrm.DBProviders.Postgres
{
    public class PostgresExceptionHandler : IDbExceptionHandler
    {
        public bool IsDuplicateKeyException(Exception ex)
            => ex is PostgresException pgEx && pgEx.SqlState == "23505";

        public Exception Translate(Exception ex)
        {
            if (ex is PostgresException pgEx)
            {
                return pgEx.SqlState switch
                {
                    "23505" => new DuplicateEmailException(""), // Поймаем в репозитории и дополним
                    _ => new DatabaseException($"Ошибка Postgres: {pgEx.Message}", ex)
                };
            }
            return new DatabaseException($"Неизвестная ошибка БД: {ex.Message}", ex);
        }
    }
}