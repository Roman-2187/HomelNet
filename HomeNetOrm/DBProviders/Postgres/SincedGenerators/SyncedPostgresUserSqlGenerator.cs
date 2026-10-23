using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres.SyncedGenerators
{
    // Наследуем базовый Postgres-генератор и явно заявляем интерфейс синхронизации 🦾
    internal class SyncedPostgresUserSqlGenerator : PostgresUserSqlGenerator, ISyncSqlGenerator<UserEntity>
    {
        public SyncedPostgresUserSqlGenerator(GenericSchemaAdapter adapter, IEnumerable<TableSchema> schemas, ILogger logger)
            : base(adapter, schemas, logger)
        {
        }

        // === ИНКАПСУЛИРОВАННАЯ ЛОГИКА СИНХРОНИЗАЦИИ (Диалект Postgres) ===

        public string GenerateCountById() =>
            $"SELECT COUNT(1) FROM \"{FormattedTable.TableName}\" WHERE {FormattedTable.IdColumnName ?? "id"} = @Id;";

        public string GenerateSelectUnsynced() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE is_synced = FALSE OR is_synced IS NULL;";

        public string GenerateUpdateMarkAsSynced() =>
            $"UPDATE \"{FormattedTable.TableName}\" SET is_synced = TRUE WHERE {FormattedTable.IdColumnName ?? "id"} = @Id;";

        // Метод для серверного PULL выгребания обновлений по таймстемпу чекпоинта
        public string GenerateSelectUpdatesByTimestamp() =>
            $"SELECT * FROM \"{FormattedTable.TableName}\" WHERE updated_at > @clientCheckpoint;";
    }
}
