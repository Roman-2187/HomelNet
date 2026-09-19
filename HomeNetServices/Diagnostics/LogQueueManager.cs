using System.Threading.Channels;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.OutputLogging.HomeNetCore.Interfaces.OutputLogging;

namespace HomeNetServices.Diagnostics
{
    public class LogQueueManager : IDisposable, ILogQueueManager
    {
        // 🔥 Событие теперь возвращает еще и имя неймспейса
        public event Func<string, LogLevel, string, bool, Task>? OnLogReceived;

        // Канал гоняет кортеж из трех элементов
        private readonly Channel<(LogLevel level, string message, string ns)> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly int _typingDelayMs;
        private readonly SynchronizationContext? _syncContext;
        private bool _isReady;

        public LogQueueManager(int typingDelayMs = 0)
        {
            _typingDelayMs = typingDelayMs >= 0 ? typingDelayMs : throw new ArgumentOutOfRangeException(nameof(typingDelayMs));
            _syncContext = SynchronizationContext.Current;

            _channel = Channel.CreateUnbounded<(LogLevel, string, string)>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
        }

        public void SetReady()
        {
            if (_isReady) return;
            _isReady = true;

            Task.Run(() => ProcessLogQueueAsync(_cts.Token));
        }

        // 🔥 Принимает неймспейс из логгера
        public void WriteLog(string message, LogLevel level, string namespaceName)
        {
            _channel.Writer.TryWrite((level, message, namespaceName));
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

                            await InvokeOnUiCtxAsync(async () =>
                            {
                                if (OnLogReceived != null)
                                {
                                    // Передаем неймспейс во View
                                    await OnLogReceived.Invoke(currentText, logEntry.level, logEntry.ns, true);
                                }
                            });

                            if (_typingDelayMs > 0)
                                await Task.Delay(_typingDelayMs, token);
                        }

                        await InvokeOnUiCtxAsync(async () =>
                        {
                            if (OnLogReceived != null)
                            {
                                await OnLogReceived.Invoke(Environment.NewLine, logEntry.level, logEntry.ns, false);
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

        private Task InvokeOnUiCtxAsync(Func<Task> action)
        {
            if (_syncContext == null) return action();
            var tcs = new TaskCompletionSource();
            _syncContext.Post(async _ =>
            {
                try { await action(); tcs.SetResult(); }
                catch (Exception ex) { tcs.SetException(ex); }
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
