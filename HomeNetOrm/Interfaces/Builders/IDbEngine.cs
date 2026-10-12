using HomeNetOrm.Interfaces.Generators;
using System.Data.Common;

namespace HomeNetOrm.Interfaces.Builders
{
    public interface IDbEngine
    {
        // Из старого ConnectionBuilder
        DbConnection Connection { get; }
        Task InitializeChannelAsync();

        // Из старого SqlProvider
        ISqlGenerator<T> GetGenerator<T>() where T : class;
    }
}
