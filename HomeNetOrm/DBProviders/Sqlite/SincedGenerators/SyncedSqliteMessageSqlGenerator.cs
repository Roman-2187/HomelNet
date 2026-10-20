using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.SyncedGenerators
{
    // Наследуем базовый SQLite генератор сообщений и подцепляем синхронизацию 🦾
    internal class SyncedSqliteMessageSqlGenerator : SqliteMessageSqlGenerator, ISyncSqlGenerator<MessageEntity>
    {
        public SyncedSqliteMessageSqlGenerator(GenericSchemaAdapter adapter, IEnumerable<TableSchema> schemas, ILogger logger)
            : base(adapter, schemas, logger)
        {
        }

        // === ИНКАПСУЛИРОВАННАЯ СИНХРОНИЗАЦИЯ СООБЩЕНИЙ ===

        public string GenerateCountById() =>
            $"SELECT COUNT(1) FROM {FormattedTable.TableName} WHERE {FormattedTable.IdColumnName ?? "id"} = @id;";

        public string GenerateSelectUnsynced() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE is_synced = 0 OR is_synced = '0' OR is_synced = 'false' OR is_synced IS NULL;";

        public string GenerateUpdateMarkAsSynced() =>
            $"UPDATE {FormattedTable.TableName} SET is_synced = 1 WHERE {FormattedTable.IdColumnName ?? "id"} = @id;";

        // Перехватываем маппинг параметров: переводим флаги в 0/1 строго перед отправкой в СУБД SQLite 🎯
        public override object PrepareParameters(MessageEntity msg)
        {
            return new
            {
                msg.Id,
                msg.SenderId,
                msg.ReceiverId,
                msg.Text,
                msg.MediaType,
                IsRead = msg.IsRead ? 1 : 0,
                msg.CreatedAt,
                IsSynced = 1, // Локально помечаем, что готово к сверке
                msg.UpdatedAt
            };
        }
    }
}
