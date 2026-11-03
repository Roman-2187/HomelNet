using HomeNet.Models.Shared.Entity;

namespace HomeNet.Synchronization.Interfaces.Sinc
{
    /// <summary>
    /// Выделенный контракт для репликации чатов и сообщений.
    /// Изолирует сетевые сигналы отправки и подтверждения СМС. 🧼
    /// </summary>
    public interface IMessageSyncActions
    {
        // --- Направление: В СЕТЬ (Локальные изменения уходят на сервер) ---

        /// <summary> Пакет отправлен в очередь на сервер </summary>
        public record Pending(MessageEntity Message);

        /// <summary> Сервер подтвердил успешную запись сообщения в Postgres </summary>
        public record Confirmed(Guid MessageId);


        // --- Направление: ИЗ СЕТИ (Запрос изменений с сервера) ---

        /// <summary> Приказ сетевому слою: вытянуть новые сообщения из Postgres </summary>
        public record PullRequested;
    }
}
