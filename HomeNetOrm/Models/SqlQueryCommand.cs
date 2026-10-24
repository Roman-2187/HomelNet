using Dapper;

namespace HomeNetOrm.Models
{
    /// <summary>
    /// 🚀 ОБЪЕКТ ЗАПРОСА: Финальный скомпилированный пакет для СУБД.
    /// Предоставляет строку SQL и параметры для Dapper через точку.
    /// </summary>
    internal class SqlQueryCommand
    {
        public string Sql { get; }
        public DynamicParameters? Params { get; }

        public SqlQueryCommand(string sql, DynamicParameters? parameters = null)
        {
            Sql = sql;
            Params = parameters;
        }
    }
}
