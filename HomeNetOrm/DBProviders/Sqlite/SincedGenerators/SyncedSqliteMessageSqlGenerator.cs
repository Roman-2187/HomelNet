using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.SincedGenerators
{
    internal class SyncedSqliteMessageSqlGenerator : SqliteMessageSqlGenerator, ISyncSqlGenerator<MessageEntity>
    {
        public SyncedSqliteMessageSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger) { }

        // === 🎯 ИНКАПСУЛИРОВАННАЯ СИНХРОНИЗАЦИЯ СООБЩЕНИЙ С УЧЕТОМ GUID PARAMETERS ===

        // 🔥 ФИКС: Добавляем Guid id на входе и прокидываем в фабрику параметров
        public SqlQueryCommand GenerateCountById(Guid id) =>
            Command($"SELECT COUNT(1) FROM \"{TableName}\" WHERE \"{IdColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};",
                    nameof(MessageEntity.Id), id);

        public SqlQueryCommand GenerateSelectUnsynced() =>
            Command($"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM \"{TableName}\" " +
                    $"WHERE \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" = 0 OR \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" IS NULL;");

        // 🔥 ФИКС: Добавляем Guid id и сюда тоже
        public SqlQueryCommand GenerateUpdateMarkAsSynced(Guid id) =>
            Command($"UPDATE \"{TableName}\" SET \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" = 1 WHERE \"{IdColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};",
                    nameof(MessageEntity.Id), id);

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
                IsSynced = 1,
                msg.UpdatedAt
            };
        }
    }
}
