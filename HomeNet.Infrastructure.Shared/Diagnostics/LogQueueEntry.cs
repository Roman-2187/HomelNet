using HomeNet.Infrastructure.Shared.Enums;

namespace HomeNet.Infrastructure.Shared.Diagnostics
{
    // Легковесный, иммутабельный контейнер для трубы
    public readonly struct LogQueueEntry
    {
        public string Message { get; }
        public LogLevel Level { get; }

        public LogQueueEntry(string message, LogLevel level)
        {
            Message = message;
            Level = level;
        }
    }
}
