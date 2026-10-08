using System.Data.Common;

namespace HomeNetOrm.Interfaces
{

    /// <summary>
    /// Универсальный контракт строителя фабрики подключений к БД.
    /// </summary>
    public interface IDbConnectionBuilder
    {
        DbConnection Connection { get; }

        Task InitializeChannelAsync();

    }
}
