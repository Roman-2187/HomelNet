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
        // 🔥 Создаем событие, на которое намертво подпишется наш встроенный AdminLogRender
        public event Func<string, LogLevel, LogColor, bool, Task>? OnLogReceived;

        private readonly Channel<(LogLevel level, string message, LogColor color)> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly int _typingDelayMs;
        private bool _isReady;

        // Конструктор теперь принимает только задержку печати, без окон!
        public LogQueueManager(int typingDelayMs = 30)
        {
            _typingDelayMs = typingDelayMs >= 0 ? typingDelayMs : throw new ArgumentOutOfRangeException(nameof(typingDelayMs));

            _channel = Channel.CreateUnbounded<(LogLevel, string, LogColor)>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
        }

        public void SetReady()
        {
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

            _channel.Writer.TryWrite((level, message, logEntry.Color));
        }

        private async Task ProcessLogQueueAsync(CancellationToken token)
        {
            try
            {
                while (await _channel.Reader.WaitToReadAsync(token))
                {
                    while (_channel.Reader.TryRead(out var logEntry))
                    {
                        string currentText = "";
                        foreach (char c in logEntry.message)
                        {
                            token.ThrowIfCancellationRequested();
                            currentText += c;

                            // Перенаправляем вывод через событие в UI поток встроенного контрола
                            await Application.Current.Dispatcher.InvokeAsync(async () =>
                            {
                                if (OnLogReceived != null)
                                {
                                    await OnLogReceived.Invoke(currentText, logEntry.level, logEntry.color, true);
                                }
                            });

                            if (_typingDelayMs > 0)
                                await Task.Delay(_typingDelayMs, token);
                        }

                        // Печатаем перенос строки в конце лога
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            if (OnLogReceived != null)
                            {
                                await OnLogReceived.Invoke(Environment.NewLine, logEntry.level, logEntry.color, false);
                            }
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
