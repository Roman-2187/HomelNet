using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Enums;

namespace HomeNetOrm.DBProviders.Postgres.Generators
{
    internal class PostgresMessageSqlGenerator : BaseSqlGenerator<MessageEntity>, IMessageSqlGenerator
    {
        private readonly string _tableName;
        private readonly string _idColumn;

        public PostgresMessageSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, NameFormat.SnakeCase, logger) // Ядро само находит схему по типу T и кэширует поля в snake_case
        {
            _tableName = Schema.TableName.ConvertTableName(NameFormat.SnakeCase);
            _idColumn = (Schema.SinglePrimaryKey?.Name ?? "Id").ConvertColumnName(NameFormat.SnakeCase);
        }

        // --- БАЗОВЫЙ CRUD ОРМ (Параметры СУБД и Dapper берем строго из словарей ядра) ---

        public override string GenerateInsert() =>
            $@"INSERT INTO ""{_tableName}"" ({SqlQueryBuilder.BuildInsertFields(Schema, NameFormat.SnakeCase)}) 
               VALUES ({SqlQueryBuilder.BuildInsertParameters(Schema)}) RETURNING ""{_idColumn}"";";

        public override string GenerateUpdate() =>
            $"UPDATE \"{_tableName}\" SET {SqlQueryBuilder.BuildSetClause(Schema, NameFormat.SnakeCase)} WHERE \"{_idColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};";

        public override string GenerateDelete() =>
            $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\"::text = {DapperParams[nameof(MessageEntity.Id)]}::text;";

        public override string GenerateSelectById() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)} FROM \"{_tableName}\" WHERE \"{_idColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};";

        public override string GenerateSelectAll() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)} FROM \"{_tableName}\";";


        // --- 🔥 СПЕЦИФИКА СООБЩЕНИЙ БЕЗ ЕДИНОЙ СТРОКИ ХАРДКОДА ---

        public string GenerateSelectChatHistory() =>
            $@"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.SnakeCase)} FROM ""{_tableName}"" 
               WHERE ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]}) 
                  OR ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.SenderId)]})
               ORDER BY {DbColumns[nameof(MessageEntity.CreatedAt)]} ASC;";

        public string GenerateMarkAsRead() =>
            $@"UPDATE ""{_tableName}"" SET {DbColumns[nameof(MessageEntity.IsRead)]} = TRUE 
               WHERE {DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} 
                 AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} 
                 AND {DbColumns[nameof(MessageEntity.IsRead)]} = FALSE;";

        public string GenerateUnreadCount() =>
            $@"SELECT COUNT(*) FROM ""{_tableName}"" 
               WHERE {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} 
                 AND {DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} 
                 AND {DbColumns[nameof(MessageEntity.IsRead)]} = FALSE;";

        public string GenerateClearChatHistory() =>
            $@"DELETE FROM ""{_tableName}"" 
               WHERE ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]})
                  OR ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.SenderId)]});";

        public override object PrepareParameters(MessageEntity msg) => msg;
    }
}
