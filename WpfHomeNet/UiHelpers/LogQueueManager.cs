using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using HomeNetCore.Enums;

namespace WpfHomeNet.UiHelpers
{
    public class LogQueueManager : IDisposable
    {
        // Высокопроизводительный асинхронный канал вместо ConcurrentQueue + ручных циклов
        private readonly Channel<(LogLevel level, string message, LogColor color)> _channel;
        private readonly LogWindow _logWindow;
        private readonly CancellationTokenSource _cts = new();
        private readonly int _typingDelayMs;
        private bool _isReady;

        public LogQueueManager(LogWindow logWindow, int typingDelayMs = 30)
        {
            _logWindow = logWindow ?? throw new ArgumentNullException(nameof(logWindow));
            _typingDelayMs = typingDelayMs >= 0 ? typingDelayMs : throw new ArgumentOutOfRangeException(nameof(typingDelayMs));

            // Создаем немультиплексированный канал (один читатель — наш цикл)
            _channel = Channel.CreateUnbounded<(LogLevel, string, LogColor)>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
        }

        // Метод для установки готовности
        // 1. Метод установки готовности (вызывается из LogViewModel по шине при первом показе окна)
        public void SetReady()
        {
            // Защита: если уже запущен, выходим, чтобы не плодить циклы!
            if (_isReady) return;

            _isReady = true;

            // Запускаем долгоиграющую задачу чтения из канала на пуле потоков
            Task.Run(() => ProcessLogQueueAsync(_cts.Token));
        }




        public void WriteLog((string Message, LogColor Color) logEntry)
        {
            string message = logEntry.Message
                .Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine)
                .Trim('\r', '\n');

            LogLevel level = logEntry.Color switch
            {
                LogColor.Error => LogLevel.Error,
                LogColor.Warning => LogLevel.Warning,
                LogColor.Information => LogLevel.Information,
                LogColor.Debug => LogLevel.Debug,
                _ => LogLevel.Information
            };

            // Просто закидываем в канал, это мгновенно и безопасно
            _channel.Writer.TryWrite((level, message, logEntry.Color));
        }

        private async Task ProcessLogQueueAsync(CancellationToken token)
        {
            try
            {
                // Ждем, пока в канале появятся данные. Поток «спит» и не ест процессор! [2]
                while (await _channel.Reader.WaitToReadAsync(token))
                {
                    while (_channel.Reader.TryRead(out var logEntry))
                    {
                        string currentText = "";
                        foreach (char c in logEntry.message)
                        {
                            token.ThrowIfCancellationRequested();
                            currentText += c;

                            // Перенаправляем вывод в UI поток WPF, чтобы избежать крашей многопоточности
                            await Application.Current.Dispatcher.InvokeAsync(async () =>
                            {
                                await _logWindow.AddLog(currentText, logEntry.level, logEntry.color, true);
                            });

                            if (_typingDelayMs > 0)
                                await Task.Delay(_typingDelayMs, token);
                        }

                        // Печатаем перенос строки в конце лога
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await _logWindow.AddLog(Environment.NewLine, logEntry.level, logEntry.color, false);
                        });
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в лог-канале: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
