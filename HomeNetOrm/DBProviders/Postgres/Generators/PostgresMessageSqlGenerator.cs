using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres.Generators
{
    internal class PostgresMessageSqlGenerator : BaseSqlGenerator<MessageEntity>, IMessageSqlGenerator
    {
        public PostgresMessageSqlGenerator(GenericSchemaAdapter adapter, IEnumerable<TableSchema> schemas, ILogger logger)
            : base(adapter, schemas, logger)
        {
        }

        // --- БАЗОВЫЙ CRUD ОРМ ---
        public override string GenerateInsert()
        {
            string idColumn = FormattedTable.IdColumnName ?? "id";
            return $@"INSERT INTO ""{FormattedTable.TableName}"" ({FormattedTable.InsertFields}) VALUES ({FormattedTable.InsertParameters}) RETURNING {idColumn};";
        }

        public override string GenerateUpdate() =>
            $"UPDATE \"{FormattedTable.TableName}\" SET {FormattedTable.SetClause} WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateDelete()
        {
            string idColumn = FormattedTable.IdColumnName ?? "id";
            return $"DELETE FROM \"{FormattedTable.TableName}\" WHERE {idColumn}::text = @{idColumn}::text;";
        }

        public override string GenerateSelectById() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\" WHERE {FormattedTable.IdColumnName} = @{FormattedTable.IdColumnName};";

        public override string GenerateSelectAll() =>
            $"SELECT {FormattedTable.AllFields} FROM \"{FormattedTable.TableName}\";";

        // --- СПЕЦИФИКА СООБЩЕНИЙ ---
        public string GenerateSelectChatHistory() =>
            $@"SELECT * FROM ""{FormattedTable.TableName}"" 
               WHERE (sender_id = @userId AND receiver_id = @friendId) 
                  OR (sender_id = @friendId AND receiver_id = @userId)
               ORDER BY created_at ASC;";

        public string GenerateMarkAsRead() =>
            $@"UPDATE ""{FormattedTable.TableName}"" SET is_read = TRUE 
               WHERE sender_id = @SenderId AND receiver_id = @ReceiverId AND is_read = FALSE;";

        public string GenerateUnreadCount() =>
            $@"SELECT COUNT(*) FROM ""{FormattedTable.TableName}"" 
               WHERE receiver_id = @CurrentUserId AND sender_id = @SenderId AND is_read = FALSE;";

        public string GenerateClearChatHistory() =>
            $@"DELETE FROM ""{FormattedTable.TableName}"" 
               WHERE (sender_id = @SenderId AND receiver_id = @ReceiverId)
                  OR (sender_id = @ReceiverId AND receiver_id = @SenderId);";

        public override object PrepareParameters(MessageEntity msg) => msg;
    }
}
