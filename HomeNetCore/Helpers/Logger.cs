using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HomeNetCore.Helpers
{
    public class Logger : ILogger
    {
        private Action<(string Message, LogColor Color)> _output;

        // Обновляем словарь маппинга
        private static readonly Dictionary<LogLevel, LogColor> LevelToColorMap = new()
    {
        { LogLevel.Trace, LogColor.Trace },
        { LogLevel.Debug, LogColor.Debug },
        { LogLevel.Information, LogColor.Information },
        { LogLevel.Warning, LogColor.Warning },
        { LogLevel.Error, LogColor.Error },
        { LogLevel.Critical, LogColor.Critical }
    };

        // Добавляем конструктор без параметров
        public Logger()
        {
            // Можно установить вывод по умолчанию, например, в Debug
            _output = (logEntry) => Debug.WriteLine(logEntry.Message);
        }

        // Добавляем метод для установки вывода
        public void SetOutput(Action<(string Message, LogColor Color)> output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output), "Вывод не может быть null");
            }
            _output = output;
        }

        // Остальные методы остаются без изменений
        public void Log(
            LogLevel level,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            params object[] args)
        {
            LogInternal(level, message, memberName, filePath, lineNumber, args);
        }

        public void LogDebug(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            params object[] args)
        {
            LogInternal(LogLevel.Debug, message, memberName, filePath, lineNumber, args);
        }

        public void LogInformation(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            params object[] args)
        {
            LogInternal(LogLevel.Information, message, memberName, filePath, lineNumber, args);
        }

        public void LogWarning(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            params object[] args)
        {
            LogInternal(LogLevel.Warning, message, memberName, filePath, lineNumber, args);
        }

        public void LogError(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            params object[] args)
        {
            LogInternal(LogLevel.Error, message, memberName, filePath, lineNumber, args);
        }

        public void LogCritical(
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            params object[] args)
        {
            LogInternal(LogLevel.Critical, message, memberName, filePath, lineNumber, args);
        }

        private void LogInternal(
            LogLevel level,
            string message,
            string memberName,
            string filePath,
            int lineNumber,
            object[] args)
        {
            string formattedMessage = string.Format(message, args);

            string className = string.IsNullOrEmpty(filePath)
                ? "UnknownClass"
                : Path.GetFileNameWithoutExtension(filePath)
                      .Split('.').Last() ?? "Unknown";

            className = CleanName(className);
            memberName = CleanName(memberName);

            var timestamp = DateTime.UtcNow.ToString("MM/dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper();

            var logEntry = $"[{timestamp}] [{levelStr}] [{className}.{memberName}:{lineNumber}] | {formattedMessage}";

            // Получаем цвет на основе уровня логирования
            LogColor color = LevelToColorMap.TryGetValue(level, out var foundColor)
                ? foundColor
                : LogColor.Information;

            _output((logEntry, color));
        }

        private string CleanName(string name)
        {
            return name.Replace('_', ' ')
                      .Trim()
                      .Replace(".", " ")
                      .Replace("`", "");
        }
    }


}
