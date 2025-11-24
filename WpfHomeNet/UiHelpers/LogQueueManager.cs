using HomeNetCore.Helpers;
using System.Collections.Concurrent;

namespace WpfHomeNet.UiHelpers
{

    // Обновленный LogQueueManager
    public class LogQueueManager
    {
        private readonly ConcurrentQueue<(LogLevel level, string message, LogColor color)> _logQueue = new();
        private bool _isProcessing;
        private readonly LogWindow _logWindow;

        public LogQueueManager(LogWindow logWindow)
        {
            _logWindow = logWindow ?? throw new ArgumentNullException(nameof(logWindow));
        }

        public void WriteLog((string Message, LogColor Color) logEntry)
        {
            // Обработка сообщения
            string message = logEntry.Message
                .Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine)
                .Trim('\r', '\n');

            LogLevel level = GetLogLevelFromColor(logEntry.Color);

            _logQueue.Enqueue((level, message, logEntry.Color));

            if (!_isProcessing)
                ProcessLogQueue();
        }

        private LogLevel GetLogLevelFromColor(LogColor color)
        {
            return color switch
            {
                LogColor.Critical => LogLevel.Critical,
                LogColor.Error => LogLevel.Error,
                LogColor.Warning => LogLevel.Warning,
                LogColor.Information => LogLevel.Information,
                LogColor.Debug => LogLevel.Debug,
                LogColor.Trace => LogLevel.Trace,
                _ => LogLevel.Information
            };
        }

        private async void ProcessLogQueue()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                while (_logQueue.TryDequeue(out var logEntry))
                {
                    string currentText = "";
                    foreach (char c in logEntry.message)
                    {
                        currentText += c;
                        await _logWindow.AddLog(currentText, logEntry.level, logEntry.color, true);
                        await Task.Delay(30); // Анимация
                    }

                    // Добавляем перенос строки если нужно
                    if (!logEntry.message.EndsWith(Environment.NewLine))
                    {
                        await _logWindow.AddLog(Environment.NewLine, logEntry.level, logEntry.color, false);
                    }
                }
            }


            // Продолжение класса LogQueueManager
            finally
            {
                _isProcessing = false;
            }
        }
    }




}
