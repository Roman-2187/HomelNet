using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Extensions
{
    // 🔥 Расширяем публичный интерфейс генератора пользователей
    public static class UserSyncSqlGeneratorExtensions
    {
        // 📥 МЕТОД 1: Выборка лога изменений пользователей для синхронизации
        internal static SqlQueryCommand GenerateSelectUsersForSync(this IUserSqlGenerator generator, DateTimeOffset timestamp)
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    // Выгребаем измененных или добавленных пользователей по единому таймстампу
                    .Col(u => u.UpdatedAt).Emit(OpType.GreaterThan).Param(u => u.UpdatedAt, timestamp)
                .Emit(PhraseType.OrderBy).Col(u => u.UpdatedAt).Emit(OpType.Asc)
                .Build();
        }

        // 💾 МЕТОД 2: Атомарный UPSERT входящих пакетов пользователей
        internal static SqlQueryCommand GenerateUserUpsert(this IUserSqlGenerator generator)
        {
            var baseInsert = generator.GenerateInsert();

            return generator.CreateQuery()
                .Raw(baseInsert.Sql)
                .Emit(PhraseType.On).Emit(PhraseType.Conflict).Emit(OpType.OpenBracket).Col(u => u.Id).Emit(OpType.CloseBracket)
                .Emit(PhraseType.Do).Emit(CmdType.Update).Emit(PhraseType.Set)

                // firstName = EXCLUDED.firstName (Исправлено вместо старого Username)
                .Col(u => u.FirstName).Emit(OpType.Equal).Emit(OpType.Excluded).Col(u => u.FirstName).Emit(OpType.Comma)

                // email = EXCLUDED.email
                .Col(u => u.Email).Emit(OpType.Equal).Emit(OpType.Excluded).Col(u => u.Email).Emit(OpType.Comma)

                // local_created_at = EXCLUDED.local_created_at
                .Col(u => u.LocalCreatedAt).Emit(OpType.Equal).Emit(OpType.Excluded).Col(u => u.LocalCreatedAt).Emit(OpType.Comma)

                // updated_at = CURRENT_TIMESTAMP
                .Col(u => u.UpdatedAt).Emit(OpType.Equal).Emit(OpType.CurrentTimestamp)
                .Build();
        }
    }
}
