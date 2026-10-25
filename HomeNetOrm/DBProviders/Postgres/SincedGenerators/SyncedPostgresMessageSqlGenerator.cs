using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres.SyncedGenerators
{
    internal class SyncedPostgresMessageSqlGenerator : PostgresMessageSqlGenerator, ISyncSqlGenerator<MessageEntity>
    {
        public SyncedPostgresMessageSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger) { }

        // --- ИНКАПСУЛИРОВАННАЯ ЛОГИКА СИНХРОНИЗАЦИИ ---

        public SqlQueryCommand GenerateCountById(Guid id) =>
            Command($"SELECT COUNT(1) FROM \"{TableName}\" WHERE \"{IdColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};",
                    nameof(MessageEntity.Id), id);

        public SqlQueryCommand GenerateSelectUnsynced() =>
            Command($"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM \"{TableName}\" " +
                    $"WHERE \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" = FALSE OR \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" IS NULL;");

        public SqlQueryCommand GenerateUpdateMarkAsSynced(Guid id) =>
            Command($"UPDATE \"{TableName}\" SET \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" = TRUE WHERE \"{IdColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};",
                    nameof(MessageEntity.Id), id);

        public SqlQueryCommand GenerateSelectMessagesForSync(Guid senderId, DateTimeOffset timestamp)
        {
            string sql = $@"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)}
               FROM ""{TableName}""
               WHERE (""{DbColumns[nameof(MessageEntity.SenderId)]}"" = {DapperParams[nameof(MessageEntity.SenderId)]}
               OR ""{DbColumns[nameof(MessageEntity.ReceiverId)]}"" = {DapperParams[nameof(MessageEntity.SenderId)]})
               AND ""{DbColumns[nameof(MessageEntity.UpdatedAt)]}"" > {DapperParams[nameof(MessageEntity.UpdatedAt)]}
               ORDER BY ""{DbColumns[nameof(MessageEntity.UpdatedAt)]}"" ASC;";

            var p = new Dapper.DynamicParameters();
            p.Add(DapperParams[nameof(MessageEntity.SenderId)].TrimStart('@'), senderId);
            p.Add(DapperParams[nameof(MessageEntity.UpdatedAt)].TrimStart('@'), timestamp);

            return new SqlQueryCommand(sql, p);
        }

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
                msg.Version,
                msg.UpdatedAt
            };
        }
    }
}
