using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres.SyncedGenerators
{
    // Наследуем базовый Postgres-генератор сообщений и заявляем интерфейс синхронизации 🦾
    internal class SyncedPostgresMessageSqlGenerator : PostgresMessageSqlGenerator, ISyncSqlGenerator<MessageEntity>
    {
        public SyncedPostgresMessageSqlGenerator(GenericSchemaAdapter adapter, IEnumerable<TableSchema> schemas, ILogger logger)
            : base(adapter, schemas, logger)
        {
        }

        // === ИНКАПСУЛИРОВАННАЯ ЛОГИКА СИНХРОНИЗАЦИИ СООБЩЕНИЙ ===

        public string GenerateCountById() =>
            $"SELECT COUNT(1) FROM \"{FormattedTable.TableName}\" WHERE {FormattedTable.IdColumnName ?? "id"} = @Id;";

        public string GenerateSelectUnsynced() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE is_synced = FALSE OR is_synced IS NULL;";

        public string GenerateUpdateMarkAsSynced() =>
            $"UPDATE \"{FormattedTable.TableName}\" SET is_synced = TRUE WHERE {FormattedTable.IdColumnName ?? "id"} = @Id;";

        // Уникальный сложный запрос для выгребания сообщений конкретного чата ПОСЛЕ чекпоинта
        public string GenerateSelectMessagesForSync() =>
            $@"SELECT id AS Id, sender_id AS SenderId, receiver_id AS ReceiverId, 
                      text AS Text, media_type AS MediaType, is_read AS IsRead, 
                      created_at AS CreatedAt, updated_at AS UpdatedAt
               FROM ""{FormattedTable.TableName}""
               WHERE (sender_id = @userId OR receiver_id = @userId)
                 AND updated_at > @clientCheckpoint
               ORDER BY updated_at ASC;";

        // Параметры для репликации сообщений на стороне сервера Postgres
        public override object PrepareParameters(MessageEntity msg)
        {
            return new
            {
                msg.Id,
                msg.SenderId,
                msg.ReceiverId,
                msg.Text,
                msg.MediaType,
                msg.IsRead,
                msg.CreatedAt,
                IsSynced = true, // На сервере принудительно флаг репликации в TRUE
                msg.UpdatedAt
            };
        }
    }
}
