using HomeNetCore.Models;

namespace HomeNetCore.Interfaces.Sinc
{


    /// <summary>
    /// Контракт управления фоновой и реалтайм синхронизацией между SQLite и PostgreSQL.
    /// </summary>
    public interface ISiberNetSyncCoordinator
    {
        Task StartAsync();

        // ==========================================
        // 1. ЛОКАЛЬНЫЕ СОБЫТИЯ (Изменения внутри приложения/БД)
        // ==========================================
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


        // ==========================================
        // 2. ЗАПРОСЫ В СЕТЬ (Синхронизатор просит сеть отправить данные)
        // ==========================================
        /// <summary> Запрос на отправку нового/обновленного сообщения на сервер </summary>
        public record MessagePendingSync(MessageEntity Message);

        /// <summary> Запрос на отправку профиля пользователя на сервер </summary>
        public record UserPendingSync(UserEntity User);

        /// <summary> Запрос на удаление пользователя с сервера </summary>
        public record UserPendingDelete(Guid UserId);


        // ==========================================
        // 3. ОТВЕТЫ ОТ СЕРВЕРА (Сеть подтвердила успешную операцию)
        // ==========================================
        /// <summary> Сервер успешно принял и сохранил сообщение </summary>
        public record MessageSyncedOnServer(Guid MessageId);

        /// <summary> Сервер успешно принял и сохранил профиль пользователя </summary>
        public record UserSyncedOnServer(Guid UserId);

        /// <summary> Сервер подтвердил удаление пользователя на своей стороне </summary>
        public record UserDeleteConfirmedByServer(Guid UserId);
    }
}