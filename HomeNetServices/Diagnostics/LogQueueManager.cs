using System.Threading.Channels;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Events; // Подключаем твою шину событий
using HomeNetCore.Interfaces.OutputLogging;

namespace HomeNetServices.Diagnostics
{
    public class LogQueueManager : IDisposable, ILogQueueManager
    {
        private readonly Channel<(LogLevel level, string message, string ns)> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly int _typingDelayMs;
        private readonly IEventBus _eventBus; // 🔥 Шина событий вместо эвентов
        private bool _isReady;

        public LogQueueManager(IEventBus eventBus, int typingDelayMs = 0)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _typingDelayMs = typingDelayMs >= 0 ? typingDelayMs : throw new ArgumentOutOfRangeException(nameof(typingDelayMs));

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
                        // Убираем \r и \n с концов прилетевшего сообщения
                        string cleanMessage = logEntry.message.TrimEnd('\r', '\n');

                        if (string.IsNullOrEmpty(cleanMessage)) continue;

                        string currentText = "";
                        foreach (char c in cleanMessage)
                        {
                            token.ThrowIfCancellationRequested();
                            currentText += c;

                            // 🔥 ОРЁТ В АВТОБУС НА КАЖДУЮ БУКВУ:
                            // Передаем подросший кусок текста, энум уровня, имя неймспейса и флаг анимации (true = идет печать)
                            _eventBus.Publish(this, new ILogQueueManager.LogMessageReceived(currentText, logEntry.level, logEntry.ns, true));

                            if (_typingDelayMs > 0)
                                await Task.Delay(_typingDelayMs, token);
                        }

                        // 🔥 ОРЁТ В АВТОБУС ФИНАЛОМ СТРОКИ:
                        // Посылаем сигнал закрытия строки (false = закрыть параграф)
                        _eventBus.Publish(this, new ILogQueueManager.LogMessageReceived(Environment.NewLine, logEntry.level, logEntry.ns, false));
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
