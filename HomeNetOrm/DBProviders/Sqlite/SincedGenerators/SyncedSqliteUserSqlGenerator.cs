using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Sqlite.Generators;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using HomeNetOrm.Enums;
using System.Collections.Generic;

namespace HomeNetOrm.DBProviders.Sqlite.SyncedGenerators
{
    // Наследуем базовый SQLite генератор и заявляем контракт синхронизации 🦾
    internal class SyncedSqliteUserSqlGenerator : SqliteUserSqlGenerator, ISyncSqlGenerator<UserEntity>
    {
        private readonly string _tableName;
        private readonly string _idColumn;

        // 🔥 ЧИСТЫЙ КОНСТРУКТОР: Никаких прослоек адаптеров, строго 2 параметра!
        public SyncedSqliteUserSqlGenerator(IEnumerable<TableSchema> schemas, ILogger logger)
            : base(schemas, logger)
        {
            _tableName = Schema.TableName.ConvertTableName(NameFormat.CamelCase);
            _idColumn = (Schema.SinglePrimaryKey?.Name ?? "Id").ConvertColumnName(NameFormat.CamelCase);
        }

        // === ИНКАПСУЛИРОВАННАЯ СИНХРОНИЗАЦИЯ (Полный статический автомат без хардкода) ===

        public string GenerateCountById() =>
            $"SELECT COUNT(1) FROM \"{_tableName}\" WHERE \"{_idColumn}\" = {DapperParams[nameof(UserEntity.Id)]};";

        public string GenerateSelectUnsynced() =>
            $"SELECT {SqlQueryBuilder.BuildSelectFields(Schema, NameFormat.CamelCase)} FROM \"{_tableName}\" " +
            $"WHERE \"{DbColumns[nameof(UserEntity.IsSynced)]}\" = 0 OR \"{DbColumns[nameof(UserEntity.IsSynced)]}\" IS NULL;";

        public string GenerateUpdateMarkAsSynced() =>
            $"UPDATE \"{_tableName}\" SET \"{DbColumns[nameof(UserEntity.IsSynced)]}\" = 1 WHERE \"{_idColumn}\" = {DapperParams[nameof(UserEntity.Id)]};";
    }
}
