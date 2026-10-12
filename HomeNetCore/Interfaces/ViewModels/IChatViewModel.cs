
using System;

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
            Guid SenderId,         // ID того, кто отправляет
            string Text,           // Текст сообщения
            string ChatType,       // "Private" или "Group"
            Guid? TargetId = null, // ID получателя ( друга или беседы )
            string? FilePath = null // Путь к файлу, если шлём не только текст
        );

        /// <summary>
        /// 🔒 НАВИГАЦИЯ ЧАТА: Передаёт команду навигатора на открытие чата и ID друга.
        /// </summary>
        public record ChatStateChanged(bool IsChatOpen, Guid FriendId);
    }
}


