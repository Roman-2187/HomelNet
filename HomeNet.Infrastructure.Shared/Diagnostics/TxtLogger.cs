using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using System.Text;

namespace HomeNet.Infrastructure.Shared.Diagnostics
{
    /// <summary>
    /// Боевой одноствольный логгер SiberNet Engine для записи плоского текста.
    /// Гарантирует чистый запуск сессии и пишет структурированный лог на Рабочий стол. 🪐
    /// </summary>
    public class TxtLogger : ILogger
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public TxtLogger(string logName = "app_debug")
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "debug");

            _filePath = Path.Combine(folder, $"{logName}.txt");

            // 🔥 ЧИСТЫЙ ЗАПУСК СЕССИИ: Если старый текстовый лог существует — сносим!
            try
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                if (File.Exists(_filePath)) File.Delete(_filePath);
            }
            catch
            {
                // Пуленепробиваемый щит рантайма от случайных блокировок
            }
        }

        public void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0)
        {
            // Форматируем строку один раз через наш общий LogFormatter
            string fullLine = LogFormatter.Format(message, memberName, filePath, lineNumber, isAutoLog: false);

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_filePath, fullLine + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Гасим ошибки записи диска
                }
            }
        }
    }
}
