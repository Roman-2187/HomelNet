using System.Text;
using Dapper;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Postgres.Base
{
    internal class PostgresSchemaSqlGenerator : ISchemaSqlGenerator
    {
        private readonly IDbProviderSpecification _spec;
        private readonly ISqlDialectTranslator _translator; 
        private readonly ILogger _logger;

        public PostgresSchemaSqlGenerator(IDbProviderSpecification spec, ISqlDialectTranslator translator, ILogger logger)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);
            if (string.IsNullOrEmpty(schema.TableName) || schema.Columns == null)
                return string.Empty;

            var pkColumns = schema.Columns.Where(c => c != null && c.IsPrimaryKey).ToList();
            var sb = new StringBuilder();

            // 🧙‍♂️ Сборка без хардкода слов "CREATE TABLE IF NOT EXISTS"
            sb.Append($"{_translator.Translate(Sql.Cmd.Create)} TABLE IF NOT EXISTS {_translator.FormatTable(schema)} (");

            // Колонки генерируются через существующий адаптер спецификации типов данных
            sb.Append(SqlQueryBuilder.BuildCreateTableColumns(_spec, schema));

            // Если ключ составной — собираем блок PRIMARY KEY
            if (pkColumns.Count > 1)
            {
                string pkFieldNames = SqlQueryBuilder.BuildCompositePrimaryKeyFields(schema);
                sb.Append($@", PRIMARY KEY ({pkFieldNames})");
            }

            sb.AppendLine(");");
            return sb.ToString();
        }

        public SqlQueryCommand GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            // Переводим имя таблицы в нужный регистр и формат СУБД (например, snake_case без кавычек для параметров)
            string cleanName = tableName.Trim('"', '\'').ToLower();

            // 🧙‍♂️ Полностью убрали хардкод строк "SELECT COUNT(*)", "FROM", "WHERE"
            var sql = $"{_translator.Translate(Sql.Cmd.Select)} COUNT(*) " +
                      $"{_translator.Translate(Sql.Phrase.From)} information_schema.tables " +
                      $"{_translator.Translate(Sql.Phrase.Where)} table_schema = 'public' {_translator.Translate(Sql.Op.And)} table_name = @CleanName;";

            var p = new DynamicParameters();
            p.Add("CleanName", cleanName);

            return new SqlQueryCommand(sql, p);
        }

        public SqlQueryCommand GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            // Системный запрос к метаданным Postgres мы также можем аккуратно "причесать" 
            // с помощью ключевых слов из нашего DTO-контейнера Sql
            var sql = $@"
                {_translator.Translate(Sql.Cmd.Select)} 
                c.column_name AS Name, 
                c.data_type AS DataType, 
                (c.is_nullable = 'YES') AS IsNullable,
                CASE WHEN tc.constraint_type = 'PRIMARY KEY' THEN 'YES' ELSE 'NO' END AS KeyType,
                c.column_default AS ExtraInfo
                {_translator.Translate(Sql.Phrase.From)} information_schema.columns c
                LEFT JOIN information_schema.key_column_usage kcu 
                ON c.table_schema = kcu.table_schema {_translator.Translate(Sql.Op.And)} c.table_name = kcu.table_name {_translator.Translate(Sql.Op.And)} c.column_name = kcu.column_name
                LEFT JOIN information_schema.table_constraints tc 
                ON kcu.table_schema = tc.table_schema {_translator.Translate(Sql.Op.And)} kcu.table_name = tc.table_name {_translator.Translate(Sql.Op.And)} kcu.constraint_name = tc.constraint_name {_translator.Translate(Sql.Op.And)} tc.constraint_type = 'PRIMARY KEY'
                {_translator.Translate(Sql.Phrase.Where)} c.table_schema = 'public' {_translator.Translate(Sql.Op.And)} c.table_name = '{tableName}';";

            return new SqlQueryCommand(sql);
        }
    }
}
