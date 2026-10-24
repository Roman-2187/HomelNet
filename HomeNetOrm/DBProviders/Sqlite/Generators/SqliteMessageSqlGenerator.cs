using Dapper;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.Generators
{
    internal class SqliteMessageSqlGenerator : BaseSqlGenerator<MessageEntity>, IMessageSqlGenerator
    {
        protected readonly string TableName;
        protected readonly string IdColumn;

        public SqliteMessageSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger)
        {
            TableName = Schema.TableName;
            IdColumn = Schema.SinglePrimaryKey?.Name ?? "id";
        }

        // --- БАЗОВЫЙ CRUD ОРМ ---

        public override SqlQueryCommand GenerateInsert() =>
            Command($@"INSERT INTO ""{TableName}"" ({SqlQueryBuilder.BuildInsertFields(Schema)}) 
                       VALUES ({SqlQueryBuilder.BuildInsertParameters(CamelSchema)});
                       SELECT last_insert_rowid() AS id;");

        public override SqlQueryCommand GenerateUpdate() =>
            Command($"UPDATE \"{TableName}\" SET {SqlQueryBuilder.BuildSetClause(Schema, CamelSchema)} WHERE \"{IdColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};");

        public override SqlQueryCommand GenerateDelete(Guid id) =>
            Command($"DELETE FROM \"{TableName}\" WHERE \"{IdColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};",
                    nameof(MessageEntity.Id), id);

        public override SqlQueryCommand GenerateSelectById(Guid id) =>
            Command($"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM \"{TableName}\" WHERE \"{IdColumn}\" = {DapperParams[nameof(MessageEntity.Id)]};",
                    nameof(MessageEntity.Id), id);

        public override SqlQueryCommand GenerateSelectAll() =>
            Command($"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM \"{TableName}\";");


        // --- 🔥 СПЕЦИФИКА СООБЩЕНИЙ С АВТОЗАПОЛНЕНИЕМ ДВУХ ПАРАМЕТРОВ ---

        public SqlQueryCommand GenerateSelectChatHistory(Guid senderId, Guid receiverId)
        {
            string sql = $@"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, CamelSchema)} FROM ""{TableName}"" 
                           WHERE ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]}) 
                              OR ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.SenderId)]})
                           ORDER BY {DbColumns[nameof(MessageEntity.CreatedAt)]} ASC;";

            var p = new DynamicParameters();
            p.Add(DapperParams[nameof(MessageEntity.SenderId)].TrimStart('@'), senderId);
            p.Add(DapperParams[nameof(MessageEntity.ReceiverId)].TrimStart('@'), receiverId);

            return new SqlQueryCommand(sql, p);
        }

        public SqlQueryCommand GenerateMarkAsRead(Guid senderId, Guid receiverId)
        {
            string sql = $@"UPDATE ""{TableName}"" SET {DbColumns[nameof(MessageEntity.IsRead)]} = 1 
                           WHERE {DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} 
                             AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} 
                             AND {DbColumns[nameof(MessageEntity.IsRead)]} = 0;";

            var p = new DynamicParameters();
            p.Add(DapperParams[nameof(MessageEntity.SenderId)].TrimStart('@'), senderId);
            p.Add(DapperParams[nameof(MessageEntity.ReceiverId)].TrimStart('@'), receiverId);

            return new SqlQueryCommand(sql, p);
        }

        public SqlQueryCommand GenerateUnreadCount(Guid currentUserId, Guid friendId)
        {
            string sql = $@"SELECT COUNT(*) FROM ""{TableName}"" 
                           WHERE {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} 
                             AND {DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} 
                             AND {DbColumns[nameof(MessageEntity.IsRead)]} = 0;";

            var p = new DynamicParameters();
            p.Add(DapperParams[nameof(MessageEntity.ReceiverId)].TrimStart('@'), currentUserId);
            p.Add(DapperParams[nameof(MessageEntity.SenderId)].TrimStart('@'), friendId);

            return new SqlQueryCommand(sql, p);
        }

        public SqlQueryCommand GenerateClearChatHistory(Guid senderId, Guid receiverId)
        {
            string sql = $@"DELETE FROM ""{TableName}"" 
                           WHERE ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.SenderId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]})
                              OR ({DbColumns[nameof(MessageEntity.SenderId)]} = {DapperParams[nameof(MessageEntity.ReceiverId)]} AND {DbColumns[nameof(MessageEntity.ReceiverId)]} = {DapperParams[nameof(MessageEntity.SenderId)]});";

            var p = new DynamicParameters();
            p.Add(DapperParams[nameof(MessageEntity.SenderId)].TrimStart('@'), senderId);
            p.Add(DapperParams[nameof(MessageEntity.ReceiverId)].TrimStart('@'), receiverId);

            return new SqlQueryCommand(sql, p);
        }

        public override object PrepareParameters(MessageEntity msg) => msg;
    }
}
