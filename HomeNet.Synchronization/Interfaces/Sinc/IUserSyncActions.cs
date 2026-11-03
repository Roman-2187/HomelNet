using HomeNet.Models.Shared.Entity;

namespace HomeNet.Synchronization.Interfaces.Sinc
{
    /// <summary>
    /// Выделенный контракт для репликации учетных записей пользователей.
    /// Полностью изолирует сигналы синхронизации профилей. 🧼
    /// </summary>
    public interface IUserSyncActions
    {
        // --- Направление: В СЕТЬ (Синхронизация локальных профилей) ---

        /// <summary> Профиль изменен локально и поставлен в очередь на отправку </summary>
        public record Pending(UserEntity User);

        /// <summary> Сервер подтвердил успешное сохранение/обновление пользователя </summary>
        public record Confirmed(Guid GuidId);


        // --- Направление: УДАЛЕНИЕ (Сквозная зачистка) ---

        /// <summary> Запрос на принудительное удаление пользователя с сервера </summary>
        public record DeletePending(Guid UserId);

        /// <summary> Сервер подтвердил, что пользователь полностью стерт из Postgres </summary>
        public record DeleteConfirmed(Guid UserId);


        // --- Направление: ИЗ СЕТИ ---

        /// <summary> Приказ сетевому слою: скачать новые профили пользователей </summary>
        public record PullRequested;
    }
}
