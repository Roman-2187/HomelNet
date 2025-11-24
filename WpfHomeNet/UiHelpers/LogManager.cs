using HomeNetCore.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace WpfHomeNet.UiHelpers
{

    public class LogManager
    {
        private readonly ConcurrentQueue<(LogLevel level, string message, LogColor color)> _logQueue = new();
        private bool _isProcessing;
        private LogWindow _logWindow;

        public LogManager(LogWindow logWindow)
        {
            _logWindow = logWindow ?? throw new ArgumentNullException(nameof(logWindow));
        }

        public void WriteLog((string Message, LogColor Color) logEntry)
        {
            Debug.WriteLine($"Получено сообщение для логирования: {logEntry.Message}");

            // Удаляем лишние переносы строк
            string message = logEntry.Message
                .Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine)
                .Trim('\r', '\n');

            // Теперь уровень логирования передается через Logger
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
                        await Task.Delay(30);
                    }

                    // Добавляем перенос строки только если сообщение не заканчивается на перенос
                    if (!logEntry.message.EndsWith(Environment.NewLine))
                    {
                        await _logWindow.AddLog(Environment.NewLine, logEntry.level, logEntry.color, false);
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }




}
