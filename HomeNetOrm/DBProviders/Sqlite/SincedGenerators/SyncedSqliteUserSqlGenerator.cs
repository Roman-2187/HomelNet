using System;
using System.Collections.Generic;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Sqlite.SyncedGenerators
{
    internal class SyncedSqliteUserSqlGenerator : Generators.SqliteUserSqlGenerator, ISyncSqlGenerator<UserEntity>
    {
        public SyncedSqliteUserSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ILogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateCountById(Guid id) =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).CountOne().Emit(Sql.Phrase.From).Table()
                // 🧙‍♂️ Чистая замена: вместо .Raw(IdColumn) используем строгое имя свойства C# класса через .Col()
                .Emit(Sql.Phrase.Where).Col(nameof(UserEntity.Id)).Emit(Sql.Op.Equal).Param(nameof(UserEntity.Id), id)
                .Build();

        public SqlQueryCommand GenerateSelectUnsynced() =>
            CreateQuery()
                .Emit(Sql.Cmd.Select).Fields().Emit(Sql.Phrase.From).Table()
                .Emit(Sql.Phrase.Where)
                    .Col(nameof(UserEntity.IsSynced)).Emit(Sql.Op.Equal).Emit(Sql.Op.False)
                .Emit(Sql.Op.Or)
                    .Col(nameof(UserEntity.IsSynced)).Emit(Sql.Op.IsNull)
                .Build();

        public SqlQueryCommand GenerateUpdateMarkAsSynced(Guid id) =>
            CreateQuery()
                .Emit(Sql.Cmd.Update).Table()
                .Emit(Sql.Phrase.Set).Col(nameof(UserEntity.IsSynced)).Emit(Sql.Op.Equal).Emit(Sql.Op.True)
                // 🧙‍♂️ И здесь тоже вычищаем ручной IdColumn на типизированное свойство
                .Emit(Sql.Phrase.Where).Col(nameof(UserEntity.Id)).Emit(Sql.Op.Equal).Param(nameof(UserEntity.Id), id)
                .Build();
    }
}
