using HomeNetCore.Data.Enums;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace WpfHomeNet.UiHelpers
{

    /// <summary>
    /// менеджер  посимвольного вывода логов в окно
    /// </summary>



    public class LogQueueManager : IDisposable
    {
        private readonly ConcurrentQueue<(LogLevel level, string message, LogColor color)> _logQueue = new();
        private bool _isProcessing;
        private readonly LogWindow _logWindow;
        private readonly CancellationTokenSource _cts = new();
        private readonly int _typingDelayMs;
        private bool _isReady = false; // Флаг готовности

        public LogQueueManager(LogWindow logWindow, int typingDelayMs = 30)
        {
            _logWindow = logWindow ?? throw new ArgumentNullException(nameof(logWindow));

            if (typingDelayMs < 0)
                throw new ArgumentOutOfRangeException(nameof(typingDelayMs), "Задержка должна быть неотрицательной");

            _typingDelayMs = typingDelayMs;
        }

        // Метод для установки готовности
        public void SetReady()
        {
            _isReady = true;
            StartProcessing(); // Запускаем обработку после установки готовности
        }

        public void WriteLog((string Message, LogColor Color) logEntry)
        {
            string message = logEntry.Message
                .Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine)
                .Trim('\r', '\n');

            LogLevel level = logEntry.Color switch
            {
                LogColor.Critical => LogLevel.Critical,
                LogColor.Error => LogLevel.Error,
                LogColor.Warning => LogLevel.Warning,
                LogColor.Information => LogLevel.Information,
                LogColor.Debug => LogLevel.Debug,
                LogColor.Trace => LogLevel.Trace,
                _ => LogLevel.Information
            };

            _logQueue.Enqueue((level, message, logEntry.Color));
        }

        private async void StartProcessing()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    if (_isReady) // Обрабатываем только если готовы
                    {
                        await ProcessLogQueue();
                    }
                    await Task.Delay(10);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обработке лога: {ex.Message}");
            }
        }

        private async Task ProcessLogQueue()
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

                        if (_typingDelayMs > 0)
                            await Task.Delay(_typingDelayMs);
                    }

                    if (!logEntry.message.EndsWith(Environment.NewLine))
                    {
                        await _logWindow.AddLog(
                            Environment.NewLine,
                            logEntry.level,
                            logEntry.color,
                            false
                        );
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}

