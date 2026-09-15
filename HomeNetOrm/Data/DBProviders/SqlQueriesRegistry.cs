using HomeNetOrm.Enums;
using System;

namespace HomeNetOrm.Data.DBProviders
{
    public static class SqlQueriesRegistry
    {
        // 🔌 НАСТРОЙКИ ДЛЯ SQLITE
        public static class Sqlite
        {
            public const string TableExists = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{0}'";
            public const string GetTableStructure = "PRAGMA table_info(\"{0}\")";

            // Индексы для ридера PRAGMA table_info
            public const int NameIndex = 1;
            public const int TypeIndex = 2;
            public const int NullableIndex = 3;
            public const int PrimaryKeyIndex = 5;
            public const int ExtraInfoIndex = -1; // У SQLite нет nextval в структуре pragma

            // Маппинг типов для генерации таблиц
            public static string MapToSqlType(ColumnType type, bool isPk, bool isAi, int? length) => type switch
            {
                ColumnType.Integer => "INTEGER", // SQLite сам поймет AUTOINCREMENT дальше
                ColumnType.Varchar => "TEXT",
                ColumnType.DateTime => "DATETIME",
                ColumnType.Boolean => "BOOLEAN",
                _ => "TEXT"
            };
        }

        // 🐘 НАСТРОЙКИ ДЛЯ POSTGRESQL
        public static class Postgres
        {
            public const string TableExists = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{0}';";
            public const string GetTableStructure = @"
                SELECT c.column_name, c.data_type, c.character_maximum_length, c.is_nullable,
                       CASE WHEN tc.constraint_type = 'PRIMARY KEY' THEN 'primary' ELSE '' END as key_type,
                       COALESCE(c.column_default, '') as extra_info
                FROM information_schema.columns c
                LEFT JOIN information_schema.key_column_usage kcu ON c.table_name = kcu.table_name AND c.column_name = kcu.column_name
                LEFT JOIN information_schema.table_constraints tc ON kcu.table_name = tc.table_name AND kcu.constraint_name = tc.constraint_name
                WHERE lower(c.table_name) = lower(@tableName) AND c.table_schema = 'public';";

            // Индексы для ридера information_schema
            public const int NameIndex = 0;
            public const int TypeIndex = 1;
            public const int NullableIndex = 3;
            public const int PrimaryKeyIndex = 4;
            public const int ExtraInfoIndex = 5;

            // Маппинг типов для генерации таблиц
            public static string MapToSqlType(ColumnType type, bool isPk, bool isAi, int? length) => type switch
            {
                ColumnType.Integer => isPk && isAi ? "SERIAL" : "INTEGER",
                ColumnType.Varchar => length.HasValue ? $"VARCHAR({length})" : "VARCHAR",
                ColumnType.DateTime => "TIMESTAMP",
                ColumnType.Boolean => "BOOLEAN",
                _ => "VARCHAR"
            };
        }
    }
}

