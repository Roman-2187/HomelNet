using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Diagnostics;

namespace HomeNetServices.Diagnostics
{
    public class EventBusInspector : IEventInspector
    {
        private readonly Dictionary<string, ComponentNode> _nodes = new();
        private readonly List<ComponentNode.SignalEvent> _signalTimeline = new();

        // 🔥 СВЕРХБЫСТРЫЙ РЕГИСТР: Хранит типы ивентов и их битовые маски в оперативной памяти
        private readonly Dictionary<Type, MessageState> _registry = new();

        private readonly HashSet<string> _ignoredMessageTypes = new()
        {
            "LogMessageReceived",
            "TextChanged"
        };

        public void RecordPublish(object sender, Type messageType)
        {
            string componentName = "UnknownSource";

            if (sender != null)
            {
                var senderType = sender.GetType();
                componentName = senderType.Name;

                if (componentName == "Object" && sender is string strName)
                {
                    componentName = strName;
                }
            }

            var node = GetOrCreateNode(componentName);

            if (!node.PublishedMessages.Contains(messageType))
            {
                node.PublishedMessages.Add(messageType);
            }

            // 🎯 ЩЁЛКАЕМ ТУМБЛЕРОМ: Врубаем бит публикации (1)
            if (!_registry.ContainsKey(messageType)) _registry[messageType] = MessageState.None;
            _registry[messageType] |= MessageState.Published;

            if (_ignoredMessageTypes.Contains(messageType.Name))
            {
                return;
            }

            _signalTimeline.Add(new ComponentNode.SignalEvent(DateTime.Now, componentName, messageType));
        }

        public void RecordSubscribe(string componentName, Type messageType, string methodName, Type subscriberType)
        {
            var node = GetOrCreateNode(componentName);
            string targetClassName = subscriberType?.Name ?? "UnknownClass";

            bool alreadyExists = node.Subscriptions.Any(s =>
                s.MessageType == messageType &&
                s.MethodName == methodName &&
                s.TargetClassName == targetClassName);

            if (!alreadyExists)
            {
                node.Subscriptions.Add(new ComponentNode.SubscriptionLink(messageType, methodName, targetClassName));
            }

            // 🎯 📑 ЩЁЛКАЕМ ТУМБЛЕРОМ: Врубаем бит подписки (2)
            if (!_registry.ContainsKey(messageType)) _registry[messageType] = MessageState.None;
            _registry[messageType] |= MessageState.Subscribed;
        }

        public void RecordUnsubscribe(Type messageType)
        {
            if (_registry.ContainsKey(messageType))
            {
                // Побитово выключаем флаг подписки при деструктуризации окон (~Бит)
                _registry[messageType] &= ~MessageState.Subscribed;
            }
        }

        private ComponentNode GetOrCreateNode(string name)
        {
            if (!_nodes.TryGetValue(name, out var node))
            {
                node = new ComponentNode { Name = name };
                _nodes[name] = node;
            }
            return node;
        }

        // 🔥 НАШ ГЛАВНЫЙ СНАЙПЕР: Собирает зрячий список объектов вместо сырого StringBuilder
        public List<IEventInspector.Line> GenerateObjectGraph()
        {
            var lines = new List<IEventInspector.Line>();

            lines.Add(new IEventInspector.Line("======= 🧠 ОБЪЕКТНЫЙ ГРАФ СИСТЕМЫ EVENTBUS =======", MessageState.None));

            foreach (var node in _nodes.Values.OrderBy(n => n.Name))
            {
                if (node.PublishedMessages.Count == 0 && node.Subscriptions.Count == 0)
                    continue;

                lines.Add(new IEventInspector.Line($"\n[ КОМПОНЕНТ: {node.Name} ]", MessageState.None));

                if (node.PublishedMessages.Count > 0)
                {
                    foreach (var msgType in node.PublishedMessages)
                    {
                        var state = _registry.TryGetValue(msgType, out var s) ? s : MessageState.None;

                        if (state == MessageState.Published)
                        {
                            // Опубликовано, но никто не слушает -> Аварийный КРАСНЫЙ
                            lines.Add(new IEventInspector.Line($" 📢 ПУБЛИКУЕТ --> 🚨 [ПУСТОТА!] [{msgType.Name}] <-- 🛑 НЕТ ПОДПИСЧИКОВ!", MessageState.Published));
                        }
                        else
                        {
                            // Бит публикации + Бит подписки сработали вместе -> НЕОНОВО-ЗЕЛЕНЫЙ
                            lines.Add(new IEventInspector.Line($" 📢 ПУБЛИКУЕТ --> [АКТИВЕН]  [{msgType.Name}]", MessageState.Live));
                        }
                    }
                }

                if (node.Subscriptions.Count > 0)
                {
                    foreach (var sub in node.Subscriptions)
                    {
                        var state = _registry.TryGetValue(sub.MessageType, out var s) ? s : MessageState.None;

                        if (state == MessageState.Subscribed)
                        {
                            // Слушатель сидит, но никто не пуляет сигнал -> Предупреждающий ОРАНЖЕВЫЙ
                            lines.Add(new IEventInspector.Line($" " +
                                $"🎧 СЛУШАЕТ  <-- ⏳ [ОЖИДАНИЕ] [{sub.MessageType.Name}] = _{sub.TargetClassName.ToLower()}.{sub.MethodName}() <-- 🛑 НЕТ ОТПРАВИТЕЛЕЙ!", MessageState.Subscribed));
                        }
                        else
                        {
                            // Живой рабочий канал -> НЕОНОВО-ЗЕЛЕНЫЙ
                            lines.Add(new IEventInspector.Line($" 🎧 СЛУШАЕТ  <-- [АКТИВЕН]  [{sub.MessageType.Name}] = _{sub.TargetClassName.ToLower()}.{sub.MethodName}()", MessageState.Live));
                        }
                    }
                }
            }

            lines.Add(new IEventInspector.Line("\n======= ⏱️ ПОСЛЕДНИЕ СИГНАЛЫ (ЛЕНТА СОБЫТИЙ) =======", MessageState.None));

            var recentSignals = _signalTimeline.AsEnumerable().Reverse().Take(20);
            foreach (var signal in recentSignals)
            {
                lines.Add(new IEventInspector.  Line($"[{signal.Time:HH:mm:ss.fff}] {signal.Source} пустил {signal.MessageType.Name}", MessageState.None));
            }

            return lines;
        }

        public string GenerateReport() => string.Join("\n", GenerateObjectGraph().Select(l => l.Text));
    }
}
