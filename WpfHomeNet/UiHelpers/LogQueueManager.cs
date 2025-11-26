using HomeNetCore.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace WpfHomeNet.UiHelpers
{

    // Обновленный LogQueueManager
    public class LogQueueManager
    {
        private readonly ConcurrentQueue<(LogLevel level, string message, LogColor color)> _logQueue = new();
        private bool _isProcessing;
        private readonly LogWindow _logWindow;
        private readonly CancellationTokenSource _cts = new();

        public LogQueueManager(LogWindow logWindow)
        {
            _logWindow = logWindow ?? throw new ArgumentNullException(nameof(logWindow));
            StartProcessing(); // Запускаем обработку при создании
        }

        public void WriteLog((string Message, LogColor Color) logEntry)
        {
            // Обработка сообщения
            string message = logEntry.Message
                .Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine)
                .Trim('\r', '\n');

            // Прямое сопоставление цвета и уровня логирования
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
                    await ProcessLogQueue();
                    await Task.Delay(10); // Небольшая задержка
                }
            }
            catch (OperationCanceledException)
            {
                // Обработка отмены
            }
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
                        await Task.Delay(30); // Анимация
                    }

                    // Добавляем перенос строки если нужно
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

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }






}
