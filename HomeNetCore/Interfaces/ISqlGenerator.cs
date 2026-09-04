

namespace HomeNetCore.Data.Interfaces
{
    // 🌌 ЕДИНЫЙ интерфейс генератора для ВСЕХ баз данных в системе!
    public interface ISqlGenerator<T> where T : class
    {
        string GenerateInsert();
        string GenerateUpdate();
        string GenerateDelete();
        string GenerateSelectById();
        string GenerateSelectAll();

        // Универсальные контракты для работы с бизнес-логикой
        string GenerateSelectByEmail();
        string GenerateEmailExists();
    }
}

