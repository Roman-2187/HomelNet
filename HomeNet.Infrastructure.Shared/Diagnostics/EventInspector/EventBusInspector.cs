using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;

namespace HomeNet.Infrastructure.Shared.Diagnostics.EventInspector
{
    public class EventBusInspector : IEventInspector
    {
        private readonly Dictionary<string, ComponentNode> _nodes = [];
        private readonly List<ComponentNode.SignalEvent> _signalTimeline = [];
        private readonly Dictionary<Type, MessageState> _registry = [];
        private readonly HashSet<string> _ignoredMessageTypes = ["LogMessageReceived", "TextChanged"];

   
        private readonly object _scanLock = new();

        private bool _isProjectScanned = false;
        private bool _isScanning = false;

        // 🔥 Событие для UI, чтобы он узнал, когда сканирование завершится
        public event Action? ScanCompleted;

        // Публичные свойства для проверки статуса
        public bool IsScanning => _isScanning;
        public bool IsScanned => _isProjectScanned;

        public EventBusInspector()
        {
          
        }

        public void RecordPublish(object sender, Type messageType)
        {
            string name = sender != null ? (sender is string str ? str : sender.GetType().Name) : "UnknownSource";
            if (name == "Object" && sender is string sName) name = sName;

            var node = GetOrCreateNode(name);
            if (!node.PublishedMessages.Contains(messageType)) node.PublishedMessages.Add(messageType);

            lock (_scanLock)
            {
                if (!_registry.ContainsKey(messageType)) _registry[messageType] = MessageState.None;
                _registry[messageType] |= MessageState.Published;

                if (!_ignoredMessageTypes.Contains(messageType.Name))
                    _signalTimeline.Add(new ComponentNode.SignalEvent(DateTime.Now, name, messageType));
            }

            GraphReporter.BuildGraph(_nodes, _registry, _signalTimeline, _scanLock);
        }

        public void RecordSubscribe(string componentName, Type messageType, string methodName, Type subscriberType)
        {
            var node = GetOrCreateNode(componentName);
            string targetClass = subscriberType?.Name ?? "UnknownClass";

            if (!node.Subscriptions.Any(s => s.MessageType == messageType && s.MethodName == methodName && s.TargetClassName == targetClass))
            {
                node.Subscriptions.Add(new ComponentNode.SubscriptionLink(messageType, methodName, targetClass));
            }

            lock (_scanLock)
            {
                if (!_registry.ContainsKey(messageType)) _registry[messageType] = MessageState.None;
                _registry[messageType] |= MessageState.Subscribed;
            }

            GraphReporter.BuildGraph(_nodes, _registry, _signalTimeline, _scanLock);
        }

        public void RecordUnsubscribe(Type messageType)
        {
            lock (_scanLock)
            {
                if (_registry.ContainsKey(messageType)) _registry[messageType] &= ~MessageState.Subscribed;
            }
            GraphReporter.BuildGraph(_nodes, _registry, _signalTimeline, _scanLock);
        }

        public List<IEventInspector.Line> GenerateObjectGraph() =>
            GraphReporter.BuildGraph(_nodes, _registry, _signalTimeline, _scanLock);

        public string GenerateReport() =>
            string.Join("\n", GenerateObjectGraph().Select(l => l.Text));

        /// <summary>
        /// Запускает сканирование сборок в фоне, чтобы не тормозить старт приложения.
        /// </summary>
        public void StartBackgroundScan(ICastomLogger logger)
        {
            if (_isProjectScanned || _isScanning) return;

            _isScanning = true;
            logger.LogInfo("[EventBusInspector] Начато фоновое сканирование сборок...");

            Task.Run(() =>
            {
                try
                {
                    lock (_scanLock)
                    {
                        AssemblyScanner.ScanPublishers(_nodes, _registry, GetOrCreateNode);
                        _isProjectScanned = true;
                    }
                    logger.LogInfo("[EventBusInspector] Фоновое сканирование сборок успешно завершено.");

                    // 🔥 Уведомляем UI, что сканирование завершено
                    ScanCompleted?.Invoke();
                }
                catch (Exception ex)
                {
                    logger.LogError($"[EventBusInspector] Ошибка при фоновом сканировании: {ex.Message}");
                }
                finally
                {
                    _isScanning = false;
                }
            });
        }

        private ComponentNode GetOrCreateNode(string name)
        {
            if (!_nodes.TryGetValue(name, out var node)) _nodes[name] = node = new ComponentNode { Name = name };
            return node;
        }
    }
}