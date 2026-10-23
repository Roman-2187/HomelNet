using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.SyncedGenerators
{
    // Наследуем базовый SQLite генератор и заявляем контракт синхронизации 🦾
    internal class SyncedSqliteUserSqlGenerator : SqliteUserSqlGenerator, ISyncSqlGenerator<UserEntity>
    {
        public SyncedSqliteUserSqlGenerator(GenericSchemaAdapter adapter, IEnumerable<TableSchema> schemas, ILogger logger)
            : base(adapter, schemas, logger)
        {
        }

        // === ИНКАПСУЛИРОВАННАЯ СИНХРОНИЗАЦИЯ (Диалект SQLite) ===

        public string GenerateCountById() =>
            $"SELECT COUNT(1) FROM {FormattedTable.TableName} WHERE {FormattedTable.IdColumnName ?? "id"} = @id;";

        public string GenerateSelectUnsynced() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE is_synced = 0 OR is_synced = '0' OR is_synced = 'false' OR is_synced IS NULL;";

        public string GenerateUpdateMarkAsSynced() =>
            $"UPDATE {FormattedTable.TableName} SET is_synced = 1 WHERE {FormattedTable.IdColumnName ?? "id"} = @id;";
    }
}
