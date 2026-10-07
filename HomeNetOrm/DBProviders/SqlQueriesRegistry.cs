using HomeNetOrm.Enums;
using HomeNetOrm.DBProviders.Extensions; // Используем твой родной неймспейс! 🔌
using System;

namespace HomeNetOrm.DBProviders
{
    /// <summary>
    /// Единый реестр спецификаций и запросов СУБД для SiberNet ORM.
    /// </summary>
    public static class SqlQueriesRegistry
    {
        // Инициализация спецификации SQLite через твой родной класс
        public static DbProviderSpecificationExtensions Sqlite { get; } = new DbProviderSpecificationExtensions(
            tableExistsQuery: "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{0}'",
            getTableStructureQuery: "PRAGMA table_info(\"{0}\")",
// Внутри SqlQueriesRegistry.cs для SQLite:
mapToSqlType: (type, isPk, isAi, length) => type switch
{
    ColumnType.Integer => "INTEGER",
    ColumnType.Varchar => "TEXT",
    ColumnType.DateTime => "DATETIME",
    ColumnType.Boolean => "BOOLEAN",
    ColumnType.Guid => "TEXT",
    ColumnType.DateTimeOffset => "DATETIME", // 🔥 МЕНЯЕМ TEXT НА DATETIME для Dapper!
    _ => "TEXT"
},
parsePropertyType: dbType => dbType.ToLower() switch
{
    "integer" => ColumnType.Integer,
    "text" or "varchar" => ColumnType.Varchar,
    "datetime" or "datetimeoffset" => ColumnType.DateTimeOffset,
    "boolean" => ColumnType.Boolean,
    _ => ColumnType.Unknown
},

            nameIndex: 1,
            typeIndex: 2,
            nullableIndex: 3,
            primaryKeyIndex: 5,
            extraInfoIndex: -1
        );

        // Инициализация спецификации PostgreSQL через твой родной класс 🐘
        public static DbProviderSpecificationExtensions Postgres { get; } = new DbProviderSpecificationExtensions(
            tableExistsQuery: "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{0}';",
            getTableStructureQuery: @"
                SELECT c.column_name, c.data_type, c.character_maximum_length, c.is_nullable,
                       CASE WHEN tc.constraint_type = 'PRIMARY KEY' THEN 'primary' ELSE '' END as key_type,
                       COALESCE(c.column_default, '') as extra_info
                FROM information_schema.columns c
                LEFT JOIN information_schema.key_column_usage kcu ON c.table_name = kcu.table_name AND c.column_name = kcu.column_name
                LEFT JOIN information_schema.table_constraints tc ON kcu.table_name = tc.table_name AND kcu.constraint_name = tc.constraint_name
                WHERE lower(c.table_name) = lower(@tableName) AND c.table_schema = 'public';",
            mapToSqlType: (type, isPk, isAi, length) => type switch
            {
                ColumnType.Integer => isPk && isAi ? "SERIAL" : "INTEGER",
                ColumnType.Varchar => length.HasValue ? $"VARCHAR({length})" : "VARCHAR",
                ColumnType.DateTime => "TIMESTAMP",
                ColumnType.Boolean => "BOOLEAN",
                ColumnType.Guid => "UUID", // Нативный UUID для Постгреса
                ColumnType.DateTimeOffset => "TIMESTAMP WITH TIME ZONE", // Честная временная зона
                _ => "VARCHAR"
            },
            parsePropertyType: dbType => dbType.ToLower() switch
            {
                "integer" or "serial" => ColumnType.Integer,
                "character varying" or "varchar" or "text" => ColumnType.Varchar,
                "timestamp without time zone" or "timestamp" => ColumnType.DateTime,
                "timestamp with time zone" or "timestamptz" => ColumnType.DateTimeOffset, // 🎯 Узнаем DateTimeOffset в Postgres
                "uuid" => ColumnType.Guid, // 🎯 Узнаем Guid из метаданных СУБД
                "boolean" => ColumnType.Boolean,
                _ => ColumnType.Unknown
            },
            nameIndex: 0,
            typeIndex: 1,
            nullableIndex: 3,
            primaryKeyIndex: 4,
            extraInfoIndex: 5
        );
    }
}
