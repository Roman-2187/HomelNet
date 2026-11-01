using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;

namespace HomeNet.Infrastructure.Shared.Diagnostics
{
    public class QueueLogger : ILogger
    {
        private readonly Action<LogQueueEntry> _logQueueWriter;

        // Конструктор принимает строго кабель под наше DTO
        public QueueLogger(Action<LogQueueEntry> logQueueWriter)
        {
            _logQueueWriter = logQueueWriter ?? throw new ArgumentNullException(nameof(logQueueWriter));
        }

        public void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0)
        {
            // Сами собираем всю красоту в монолит через твой Formatter
            string formattedLine = LogFormatter.Format(message, memberName, filePath, lineNumber, isAutoLog: false);

            // 🔥 Передаем менеджеру готовый пакет! Просто и без лишней каши.
            _logQueueWriter.Invoke(new LogQueueEntry(formattedLine, level));
        }
    }
}
