namespace HomeNetOrm.Interfaces.DbInfrastructure
{
    public interface IDbExceptionHandler
    {
        // 🔥 Проверка на дубликат ключа
        bool IsDuplicateKeyException(Exception ex);

        // 🔥 Преобразование любой ошибки в доменное исключение
        Exception Translate(Exception ex);
    }
}