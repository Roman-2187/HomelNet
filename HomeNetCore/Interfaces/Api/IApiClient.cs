using HomeNetCore.Models.Entity;

namespace HomeNetCore.Interfaces
{
    public interface IApiClient
    {
        bool IsServerAvailable { get; }

        // =========================================================================
        // 📤 НАПРАВЛЕНИЕ PUSH: Отправка данных на сервер (Твои методы)
        // =========================================================================
        Task<bool> DeleteUserAsync(Guid userId);
        Task<bool> PushMessageAsync(object messageEntity);
        Task<bool> PushUserAsync(object userEntity);

        // =========================================================================
        // 📥 🔥 НАПРАВЛЕНИЕ PULL: Скачивание свежих данных с сервера (Добавляем!)
        // =========================================================================

        /// <summary>
        /// Запросить у сервера пакет сообщений, появившихся строго после чекпоинта устройства
        /// </summary>
        Task<IReadOnlyCollection<MessageEntity>> FetchFreshMessagesAsync(Guid deviceId, DateTimeOffset lastSyncedAt);

        /// <summary>
        /// Запросить у сервера новые или обновленные профили пользователей
        /// </summary>
        Task<IReadOnlyCollection<UserEntity>> FetchFreshUsersAsync(Guid deviceId, DateTimeOffset lastSyncedAt);
    }
}
