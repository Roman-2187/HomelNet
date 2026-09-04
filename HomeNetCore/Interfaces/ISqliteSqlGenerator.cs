namespace HomeNetCore.Data.DBProviders.Sqlite
{
    namespace HomeNetCore.Data.DBProviders.Sqlite
    {
        // Сделали большую букву L в названии интерфейса: ISqLiteSqlGenerator
        public interface ISqLiteSqlGenerator<T> where T : class
        {
            string GenerateInsert();
            string GenerateUpdate();
            string GenerateDelete();
            string GenerateSelectById();
            string GenerateSelectAll();

            // Эти методы нужны для UserRepository
            string GenerateSelectByEmail();
            string GenerateEmailExists();
        }
    }
}
