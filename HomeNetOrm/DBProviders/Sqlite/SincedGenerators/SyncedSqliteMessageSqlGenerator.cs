using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Enums;

namespace HomeNetOrm.DBProviders.Sqlite.SincedGenerators
{
    // Наследуем базовый SQLite генератор сообщений и подцепляем синхронизацию 🦾
    internal class SyncedSqliteMessageSqlGenerator : SqliteMessageSqlGenerator, ISyncSqlGenerator<MessageEntity>
    {
        private readonly string _tableName;
        private readonly string _idColumn;

        // 🔥 ФИКС: Вырезали адаптер, инициализируем ядро в CamelCase под SQLite
        public SyncedSqliteMessageSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger)
        {
            _tableName = Schema.TableName.ConvertTableName(NameFormat.CamelCase);
            _idColumn = (Schema.SinglePrimaryKey?.Name ?? "Id").ConvertColumnName(NameFormat.CamelCase);
        }

        // === ИНКАПСУЛИРОВАННАЯ СИНХРОНИЗАЦИЯ СООБЩЕНИЙ БЕЗ ЕДИНОЙ СТРОКИ ХАРДКОДА ===

        public string GenerateCountById() =>
            $"SELECT COUNT(1) FROM \"{_tableName}\" WHERE \"{_idColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};";

        public string GenerateSelectUnsynced() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.CamelCase)} FROM \"{_tableName}\" " +
            $"WHERE \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" = 0 OR \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" IS NULL;";

        public string GenerateUpdateMarkAsSynced() =>
            $"UPDATE \"{_tableName}\" SET \"{DbColumns[nameof(MessageEntity.IsSynced)]}\" = 1 WHERE \"{_idColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};";

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
