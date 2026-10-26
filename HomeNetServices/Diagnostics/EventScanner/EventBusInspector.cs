using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Diagnostics;
using HomeNetServices.Diagnostics.EventScanner;

namespace HomeNetServices.Diagnostics
{
    public class EventBusInspector : IEventInspector
    {
        private readonly Dictionary<string, ComponentNode> _nodes = [];
        private readonly List<ComponentNode.SignalEvent> _signalTimeline = [];
        private readonly Dictionary<Type, MessageState> _registry = [];
        private readonly HashSet<string> _ignoredMessageTypes = ["LogMessageReceived", "TextChanged"];

        private bool _isProjectScanned = false;
        private readonly object _scanLock = new();

        public EventBusInspector() { }

        public void RecordPublish(object sender, Type messageType)
        {
            EnsureStaticScan();
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

            // 🔥 Пишем изменения в файл СРАЗУ на лету, не дожидаясь клика кнопки!
            GraphReporter.BuildGraph(_nodes, _registry, _signalTimeline, _scanLock);
        }

        public void RecordSubscribe(string componentName, Type messageType, string methodName, Type subscriberType)
        {
            EnsureStaticScan();
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

            // 🔥 Сразу обновляем отчет на жестком диске
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

        private void EnsureStaticScan()
        {
            if (_isProjectScanned) return;
            lock (_scanLock)
            {
                if (_isProjectScanned) return;
                AssemblyScanner.ScanPublishers(_nodes, _registry, GetOrCreateNode);
                _isProjectScanned = true;
            }
        }

        private ComponentNode GetOrCreateNode(string name)
        {
            if (!_nodes.TryGetValue(name, out var node)) _nodes[name] = node = new ComponentNode { Name = name };
            return node;
        }
    }
}
