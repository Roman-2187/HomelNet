using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Extensions; 
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Base;

namespace HomeNetOrm.Repositories.Extensions
{
    public static class MessageRepoSincExtensions
    {
        // 📥 Выгрузка хронологии СМС по конкретному чату для репликации
        public static async Task<IEnumerable<MessageEntity>> GetMessagesForSyncAsync(this IMessageRepo repo, Guid userId, DateTimeOffset checkpoint)
        {
            var baseRepo = (BaseRepository<MessageEntity>)repo;
            var cmd = ((IMessageSqlGenerator)baseRepo.SqlGenerator).GenerateSelectMessagesForSync(userId, checkpoint);

            return await baseRepo.ExecuteAsync(db => db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params));
        }

        // 💾 Атомарное слияние СМС при конфликтах ID (ON CONFLICT DO UPDATE)
        public static async Task SaveOrSyncMessageAsync(this IMessageRepo repo, MessageEntity message)
        {
            var baseRepo = (BaseRepository<MessageEntity>)repo;
            var cmd = ((IMessageSqlGenerator)baseRepo.SqlGenerator).GenerateUpsert();

            await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, message));
        }

        // 🎯 Фиксация флага успешной отправки СМС клиентом
        public static async Task MarkMessageAsSyncedAsync(this IMessageRepo repo, Guid id)
        {
            var baseRepo = (BaseRepository<MessageEntity>)repo;
            var cmd = baseRepo.SqlGenerator.GenerateUpdateMarkAsSynced(id);

            await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params));
            baseRepo.Logger.LogInfo($"[MessageEntity] Запись {id} успешно засинхронена.");
        }
    }
}
