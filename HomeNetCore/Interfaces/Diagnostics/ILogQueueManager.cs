using HomeNet.Infrastructure.Shared.Diagnostics;
using HomeNet.Infrastructure.Shared.Enums;

namespace HomeNetCore.Interfaces.Diagnostics
{
    public interface ILogQueueManager
    {
        void SetReady();

        // 🔥 Принимает строго наше готовое DTO! Без всяких ns строк
        void WriteLog(LogQueueEntry entry);

        // 🎯 ДТО ДЛЯ ВНУТРЕННЕЙ ТРУБЫ КАНАЛА
       

        // 🔥 ОБНОВЛЕННЫЙ РЕКОРД ДЛЯ АВТОБУСА СОБЫТИЙ БЕЗ NS:
        public record LogMessageReceived(string Text, LogLevel Level, bool IsAnimating);
    }
}
