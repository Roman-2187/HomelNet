using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.Generators
{
    internal class SqliteMessageSqlGenerator : BaseSqlGenerator<MessageEntity>, IMessageSqlGenerator
    {
        private readonly string _tableName;
        private readonly string _idColumn;

        public SqliteMessageSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, NameFormat.CamelCase, logger) // Ядро само находит схему по типу T и кэширует полей в CamelCase
        {
            _tableName = Schema.TableName.ConvertTableName(NameFormat.CamelCase);
            _idColumn = (Schema.SinglePrimaryKey?.Name ?? "Id").ConvertColumnName(NameFormat.CamelCase);
        }

        // --- БАЗОВЫЙ CRUD ОРМ (Берем параметры СУБД и Dapper строго из словарей ядра) ---

        public override string GenerateInsert() =>
            $@"INSERT INTO ""{_tableName}"" ({SqlQueryBuilder.BuildInsertFields(Schema, NameFormat.CamelCase)}) 
               VALUES ({SqlQueryBuilder.BuildInsertParameters(Schema)});
               SELECT last_insert_rowid() AS id;";

        public override string GenerateUpdate() =>
            $"UPDATE \"{_tableName}\" SET {SqlQueryBuilder.BuildSetClause(Schema, NameFormat.CamelCase)} WHERE \"{_idColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};";

        public override string GenerateDelete() =>
            $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};";

        public override string GenerateSelectById() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.CamelCase)} FROM \"{_tableName}\" WHERE \"{_idColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};";

        public override string GenerateSelectAll() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.CamelCase)} FROM \"{_tableName}\";";


        // --- 🔥 СПЕЦИФИКА СООБЩЕНИЙ БЕЗ ЕДИНОЙ СТРОКИ ХАРДКОДА И СОБАКМЕНОВ ---

        // Для истории чата репозиторий передает в Dapper заполненный объект MessageEntity
        public string GenerateSelectChatHistory() =>
            $@"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.CamelCase)} FROM ""{_tableName}"" 
               WHERE ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]}) 
                  OR ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.SenderId)]})
               ORDER BY {DbColumns[nameof(MessageEntity.CreatedAt)]} ASC;";

        public string GenerateMarkAsRead() =>
            $@"UPDATE ""{_tableName}"" SET {DbColumns[nameof(MessageEntity.IsRead)]} = 1 
               WHERE {DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} 
                 AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} 
                 AND {DbColumns[nameof(MessageEntity.IsRead)]} = 0;";

        // Для счетчика непрочитанных репозиторий передает объект, где ReceiverId — это текущий юзер, а SenderId — собеседник
        public string GenerateUnreadCount() =>
            $@"SELECT COUNT(*) FROM ""{_tableName}"" 
               WHERE {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} 
                 AND {DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} 
                 AND {DbColumns[nameof(MessageEntity.IsRead)]} = 0;";

        public string GenerateClearChatHistory() =>
            $@"DELETE FROM ""{_tableName}"" 
               WHERE ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]})
                  OR ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.SenderId)]});";

        public override object PrepareParameters(MessageEntity msg) => msg;
    }
}
