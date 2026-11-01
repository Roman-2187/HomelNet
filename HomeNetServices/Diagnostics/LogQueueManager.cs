using HomeNet.Infrastructure.Shared.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.Diagnostics;
using System.Text;
using System.Threading.Channels;
using static HomeNetCore.Interfaces.Diagnostics.ILogQueueManager;

namespace HomeNetServices.Diagnostics
{
    public class LogQueueManager : IDisposable, ILogQueueManager
    {
        // 🔥 Никаких кортежей — только наше строгое DTO
        private readonly Channel<LogQueueEntry> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly int _typingDelayMs;
        private readonly IEventBus _eventBus;
        private bool _isReady;
        private Task? _processingTask;

        public LogQueueManager(IEventBus eventBus, int typingDelayMs = 0)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _typingDelayMs = typingDelayMs >= 0 ? typingDelayMs : throw new ArgumentOutOfRangeException(nameof(typingDelayMs));

            // Канал завязан на рекорды
            _channel = Channel.CreateUnbounded<LogQueueEntry>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
        }

        public void SetReady()
        {
            if (_isReady) return;
            _isReady = true;
            _processingTask = Task.Run(() => ProcessLogQueueAsync(_cts.Token));
        }

        // 🔥 ПРОСТОЕ ПРИСВОЕНИЕ: Просто пересылаем собранный логгером DTO пакет в трубу
        public void WriteLog(LogQueueEntry entry)
        {
            _channel.Writer.TryWrite(entry);
        }

        private async Task ProcessLogQueueAsync(CancellationToken token)
        {
            try
            {
                while (await _channel.Reader.WaitToReadAsync(token))
                {
                    while (_channel.Reader.TryRead(out var logEntry))
                    {
                        token.ThrowIfCancellationRequested();

                        string cleanMessage = logEntry.Message.TrimEnd('\r', '\n');
                        if (string.IsNullOrEmpty(cleanMessage)) continue;

                        var currentTextBuilder = new StringBuilder();

                        foreach (char c in cleanMessage)
                        {
                            token.ThrowIfCancellationRequested();
                            currentTextBuilder.Append(c);

                            // Стреляем в автобус чистым рекордом события без ns
                            _eventBus.Publish(this, new LogMessageReceived(
                                currentTextBuilder.ToString(), logEntry.Level, true));

                            if (_typingDelayMs > 0)
                                await Task.Delay(_typingDelayMs, token);
                        }

                        _eventBus.Publish(this, new LogMessageReceived(
                            Environment.NewLine, logEntry.Level, false));
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
