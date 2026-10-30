using System.Text;
using System.Threading.Channels;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;

namespace HomeNetServices.Diagnostics
{
    public class LogQueueManager : IDisposable, ILogQueueManager
    {
        private readonly Channel<(LogLevel level, string message, string ns)> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly int _typingDelayMs;
        private readonly IEventBus _eventBus;
        private bool _isReady;
        private Task? _processingTask; // Храним таску воркера

        public LogQueueManager(IEventBus eventBus, int typingDelayMs = 0)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _typingDelayMs = typingDelayMs >= 0 ? typingDelayMs : throw new ArgumentOutOfRangeException(nameof(typingDelayMs));

            // Сначала полностью создаем канал
            _channel = Channel.CreateUnbounded<(LogLevel, string, string)>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
        }

        public void SetReady()
        {
            if (_isReady) return;
            _isReady = true;

            // Запускаем воркер СТРОГО один раз, когда менеджер готов
            _processingTask = Task.Run(() => ProcessLogQueueAsync(_cts.Token));
        }

        public void WriteLog(string message, LogLevel level, string namespaceName)
        {
            _channel.Writer.TryWrite((level, message, namespaceName));
        }

        private async Task ProcessLogQueueAsync(CancellationToken token)
        {
            try
            {
                // Правильный способ чтения из канала: ждем, пока появятся данные
                while (await _channel.Reader.WaitToReadAsync(token))
                {
                    while (_channel.Reader.TryRead(out var logEntry))
                    {
                        token.ThrowIfCancellationRequested();

                        string cleanMessage = logEntry.message.TrimEnd('\r', '\n');
                        if (string.IsNullOrEmpty(cleanMessage)) continue;

                        // Оптимизация: используем StringBuilder вместо конкатенации строк в цикле 'currentText += c'
                        var currentTextBuilder = new StringBuilder();

                        foreach (char c in cleanMessage)
                        {
                            token.ThrowIfCancellationRequested();
                            currentTextBuilder.Append(c);

                            // Отправляем текущую накопленную строку
                            _eventBus.Publish(this, new ILogQueueManager.LogMessageReceived(
                                currentTextBuilder.ToString(), logEntry.level, logEntry.ns, true));

                            if (_typingDelayMs > 0)
                                await Task.Delay(_typingDelayMs, token);
                        }

                        // Финал строки
                        _eventBus.Publish(this, new ILogQueueManager.LogMessageReceived(
                            Environment.NewLine, logEntry.level, logEntry.ns, false));
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
