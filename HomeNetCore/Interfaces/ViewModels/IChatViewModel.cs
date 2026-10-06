

namespace HomeNetCore.Interfaces.ViewModels
{
    /// <summary>
    /// Архитектурный контракт чата SiberNet.
    /// Задает единый стандарт сигналов для вьюмоделей, сервисов и Инспектора шины.
    /// </summary>
    public interface IChatViewModel
    {
        /// <summary>
        /// 🚀 НАМЕРЕНИЕ: UI-окно просит сервис обработать и отправить сообщение.
        /// </summary>
        public record Send(
            Guid SenderId,       // ID того, кто отправляет ( UI берет из своего кэша/сессии )
            string Text,        // Текст сообщения
            string ChatType,    // "Private" или "Group"
            Guid? TargetId = null, // ID получателя ( друга или беседы )
            string? FilePath = null // Путь к файлу, если шлём не только текст
        );

        /// <summary>
        /// 📢 СИСТЕМНЫЙ ФАКТ: Бэкенд-сервис подтвердил запись в SQLite/Postgres.
        /// </summary>
        public record Received(
            Guid MessageId,      // Реальный ID, который выдала база данных при сохранении
            Guid SenderId,       // Кто отправил
            string Text,        // Что отправил
            string ChatType,    // Тип чата
            string? FilePath = null // Прикрепленный файл
        );

        /// <summary>
        /// ⚡ UI-ТРИГГЕР: Локальный выстрел кнопки "Отправить" внутри формы ввода.
        /// </summary>
        public record NewSent(
            string Text,
            Guid ReceiverId
        );
    }
}


