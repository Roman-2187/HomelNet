using System;
using System.Linq;
using System.Text;
using Dapper;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.Base
{
    internal class SqliteSchemaGenerator : ISchemaSqlGenerator
    {
        private readonly IDbProviderSpecification _spec;
        private readonly ISqlDialectTranslator _translator;

        public SqliteSchemaGenerator(IDbProviderSpecification spec, ISqlDialectTranslator translator)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
        }

        public string GenerateCreateTableSql(TableSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (string.IsNullOrEmpty(schema.TableName) || schema.Columns == null)
                return string.Empty;

            var pkColumns = schema.Columns.Where(c => c != null && c.IsPrimaryKey).ToList();
            var sb = new StringBuilder();

            sb.Append($"{_translator.Translate(Sql.Cmd.Create)} TABLE IF NOT EXISTS {_translator.FormatTable(schema)} (");
            sb.Append(SqlQueryBuilder.BuildCreateTableColumns(_spec, schema));

            if (pkColumns.Count > 1)
            {
                string pkFieldNames = SqlQueryBuilder.BuildCompositePrimaryKeyFields(schema);
                sb.Append($@", PRIMARY KEY ({pkFieldNames})");
            }

            sb.Append(");");
            return sb.ToString();
        }

        public SqlQueryCommand GenerateTableExistsSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            var sql = $"{_translator.Translate(Sql.Cmd.Select)} COUNT(*) " +
                      $"{_translator.Translate(Sql.Phrase.From)} sqlite_master " +
                      $"{_translator.Translate(Sql.Phrase.Where)} type='table' {_translator.Translate(Sql.Op.And)} name = @CleanName;";

            var p = new DynamicParameters();
            p.Add("CleanName", tableName.Trim('"', '\'').ToLower());

            return new SqlQueryCommand(sql, p);
        }

        public SqlQueryCommand GenerateGetTableStructureSql(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Имя таблицы пустое", nameof(tableName));

            var sql = $@"{_translator.Translate(Sql.Cmd.Select)} 
                name AS Name, 
                type AS DataType, 
                CASE WHEN [notnull] = 0 THEN 1 ELSE 0 END AS IsNullable,
                CASE WHEN pk >= 1 THEN 'YES' ELSE 'NO' END AS KeyType,
                dflt_value AS ExtraInfo
                {_translator.Translate(Sql.Phrase.From)} pragma_table_info('{tableName}');";

            return new SqlQueryCommand(sql);
        }
    }
}