using System.Threading.Channels;
using HomeNetCore.Enums;
using HomeNetServices.Services.Diagnostics;

namespace HomeNetServices.Diagnostics
{
    public class LogQueueManager : IDisposable, ILogQueueManager
    {
        // 🔥 Событие теперь возвращает только LogLevel! Никакого упоминания графического LogColor
        public event Func<string, LogLevel, bool, Task>? OnLogReceived;

        // Канал теперь гоняет чистый кортеж без лишней краски
        private readonly Channel<(LogLevel level, string message)> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly int _typingDelayMs;
        private readonly SynchronizationContext? _syncContext; // Кроссплатформенный пульт возврата в UI поток
        private bool _isReady;

        public LogQueueManager(int typingDelayMs = 0)
        {
            _typingDelayMs = typingDelayMs >= 0 ? typingDelayMs : throw new ArgumentOutOfRangeException(nameof(typingDelayMs));

            // Запоминаем контекст потока (WPF или Avalonia подхватят его автоматически на старте)
            _syncContext = SynchronizationContext.Current;

            _channel = Channel.CreateUnbounded<(LogLevel, string)>(new UnboundedChannelOptions
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

        // 🔥 Принимает чистый текст и уровень логирования напрямую от Logger.cs
        public void WriteLog(string message, LogLevel level)
        {
            string cleanMessage = message
                .Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine)
                .Trim('\r', '\n');

            _channel.Writer.TryWrite((level, cleanMessage));
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

                            // Возвращаемся в UI-поток кроссплатформенно
                            await InvokeOnUiCtxAsync(async () =>
                            {
                                if (OnLogReceived != null)
                                {
                                    await OnLogReceived.Invoke(currentText, logEntry.level, true);
                                }
                            });

                            if (_typingDelayMs > 0)
                                await Task.Delay(_typingDelayMs, token);
                        }

                        // Печатаем перенос строки в конце лога
                        await InvokeOnUiCtxAsync(async () =>
                        {
                            if (OnLogReceived != null)
                            {
                                await OnLogReceived.Invoke(Environment.NewLine, logEntry.level, false);
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

        /// <summary>
        /// Помощник для перенаправления выполнения задачи в UI контекст без жесткой привязки к WPF Application
        /// </summary>
        private Task InvokeOnUiCtxAsync(Func<Task> action)
        {
            if (_syncContext == null) return action();

            var tcs = new TaskCompletionSource();
            _syncContext.Post(async _ =>
            {
                try
                {
                    await action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
