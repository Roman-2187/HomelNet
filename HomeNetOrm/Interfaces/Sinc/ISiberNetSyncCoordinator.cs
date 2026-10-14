namespace HomeNetOrm.Interfaces.Sinc
{
    using System;
    using System.Threading.Tasks;
    using HomeNetCore.Models;

    /// <summary>
    /// Контракт управления фоновой и реалтайм синхронизацией между SQLite и PostgreSQL.
    /// </summary>
    public interface ISiberNetSyncCoordinator
    {
        // 👤 СОБЫТИЯ ПОЛЬЗОВАТЕЛЕЙ
        public record UserInserted(UserEntity User);
        public record UserUpdated(UserEntity User);
        public record UserDeleted(Guid UserId);

        // 💬 СОБЫТИЯ СООБЩЕНИЙ
        public record MessageInserted(MessageEntity Message);
        public record MessageUpdated(MessageEntity Message);
        public record MessageDeleted(Guid MessageId);
        public record ChatCleared(Guid SenderId, Guid ReceiverId);

        // 🤝 СОБЫТИЯ КОНТАКТОВ (ДРУЗЕЙ)
        public record FriendInserted(FriendEntity Friend);
        public record FriendDeleted(Guid FriendConnectionId);

        Task StartAsync();
    }



}
