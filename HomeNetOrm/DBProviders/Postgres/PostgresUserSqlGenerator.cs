using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres
{
    // Убедитесь, что класс объявлен именно так, с явным указанием интерфейса:
    internal class PostgresUserSqlGenerator : BaseSqlGenerator<UserEntity>, IUserSqlGenerator
    {
        public PostgresUserSqlGenerator(GenericSchemaAdapter adapter, IEnumerable<TableSchema> schemas, ILogger logger)
            : base(adapter, schemas, logger)
        {
        }


        // --- БАЗОВЫЙ CRUD ОРМ ---
        public override string GenerateInsert() =>
            $@"INSERT INTO ""{FormattedTable.TableName}"" ({FormattedTable.InsertFields}) VALUES ({FormattedTable.InsertParameters}) RETURNING {FormattedTable.IdColumnName ?? "id"};";

        public override string GenerateUpdate() =>
            $"UPDATE \"{FormattedTable.TableName}\" SET {FormattedTable.SetClause} WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateDelete() =>
            $"DELETE FROM \"{FormattedTable.TableName}\" WHERE {FormattedTable.IdColumnName}::text = @{FormattedTable.IdColumnName}::text;";

        public override string GenerateSelectById() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateSelectAll() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\";";


        // --- Реализация подключаемого модуля СИНХРОНИЗАЦИИ (Никакого хардкода полей!) ---
        public override string GenerateCountById() =>
            $"SELECT COUNT(1) FROM \"{FormattedTable.TableName}\" WHERE {FormattedTable.IdColumnName ?? "id"} = @Id;";

        public override string GenerateSelectUnsynced() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE is_synced = 0 OR is_synced = '0' OR is_synced = 'false' OR is_synced IS NULL;";

        public override string GenerateUpdateMarkAsSynced() =>
            $"UPDATE \"{FormattedTable.TableName}\" SET is_synced = 1 WHERE {FormattedTable.IdColumnName ?? "id"} = @Id;";

        // Специфика Пользователей
        public string GenerateSelectByEmail() => $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE email = @email;";
        public string GenerateEmailExists() => $"SELECT COUNT(*) FROM \"{FormattedTable.TableName}\" WHERE email = @email;";
    }
}
