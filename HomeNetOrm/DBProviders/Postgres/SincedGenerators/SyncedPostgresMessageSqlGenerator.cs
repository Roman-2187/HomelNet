using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Enums;

namespace HomeNetOrm.DBProviders.Postgres.SyncedGenerators
{
    internal class SyncedPostgresMessageSqlGenerator : PostgresMessageSqlGenerator, ISyncSqlGenerator<MessageEntity>
    {
        private readonly string _tableName;
        private readonly string _idColumn;

        public SyncedPostgresMessageSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger)
        {
            _tableName = Schema.TableName.ConvertTableName(NameFormat.SnakeCase);
            _idColumn = (Schema.SinglePrimaryKey?.Name ?? "Id").ConvertColumnName(NameFormat.SnakeCase);
        }

        // --- ИНКАПСУЛИРОВАННАЯ ЛОГИКА СИНХРОНИЗАЦИИ ---

        public string GenerateCountById() =>
            $"SELECT COUNT(1) FROM \"{_tableName}\" WHERE \"{_idColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};";

        public string GenerateSelectUnsynced() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)} FROM \"{_tableName}\" " +
            $"WHERE \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" = FALSE OR \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" IS NULL;";

        public string GenerateUpdateMarkAsSynced() =>
            $"UPDATE \"{_tableName}\" SET \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" = TRUE WHERE \"{_idColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};";

        // 🔥 СМОТРИ, ВОТ ОН — ТРИУМФ ЕДИНСТВЕННОЙ ПРАВДЫ СУЩНОСТИ:
        // Репозиторий просто передает в Dapper объект сообщения, где:
        // msg.SenderId — это ID пользователя, а msg.UpdatedAt — это время чекпоинта!
        public string GenerateSelectMessagesForSync() =>
            $@"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)}
               FROM ""{_tableName}""
               WHERE (""{DbColumns[nameof(MessageEntity.SenderId)]}"" = {DapperParams[nameof(MessageEntity.SenderId)]} OR ""{DbColumns[nameof(MessageEntity.ReceiverId)]}"" = {DapperParams[nameof(MessageEntity.SenderId)]})
                 AND ""{DbColumns[nameof(MessageEntity.UpdatedAt)]}"" > {DapperParams[nameof(MessageEntity.UpdatedAt)]}
               ORDER BY ""{DbColumns[nameof(MessageEntity.UpdatedAt)]}"" ASC;";

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
                IsSynced = true,
                msg.UpdatedAt
            };
        }
    }
}
