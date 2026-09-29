using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Diagnostics;

namespace HomeNetServices.Diagnostics
{
    public class EventBusInspector : IEventInspectorSource
    {
        private readonly Dictionary<string, ComponentNode> _nodes = new();
        private readonly List<ComponentNode.SignalEvent> _signalTimeline = new();

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

            if (_ignoredMessageTypes.Contains(messageType.Name))
            {
                return;
            }

            _signalTimeline.Add(new ComponentNode.SignalEvent(DateTime.Now, componentName, messageType));
        }

        /// <summary>
        /// 🔥 ОБНОВЛЕНО: Теперь фиксируем не только метод, но и КЛАСС-подписчик!
        /// </summary>
        public void RecordSubscribe(string componentName, Type messageType, string methodName, Type subscriberType)
        {
            var node = GetOrCreateNode(componentName);
            string targetClassName = subscriberType?.Name ?? "UnknownClass";

            // Проверяем дубликаты с учетом целевого класса
            bool alreadyExists = node.Subscriptions.Any(s =>
                s.MessageType == messageType &&
                s.MethodName == methodName &&
                s.TargetClassName == targetClassName);

            if (!alreadyExists)
            {
                // Передаем имя класса-слушателя в структуру связи
                node.Subscriptions.Add(new ComponentNode.SubscriptionLink(messageType, methodName, targetClassName));
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

        public string GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("======= 🧠 ОБЪЕКТНЫЙ ГРАФ СИСТЕМЫ EVENTBUS =======");

            foreach (var node in _nodes.Values.OrderBy(n => n.Name))
            {
                if (node.PublishedMessages.Count == 0 && node.Subscriptions.Count == 0)
                    continue;

                sb.AppendLine($"\n[ КОМПОНЕНТ: {node.Name} ]");

                if (node.PublishedMessages.Count > 0)
                {
                    foreach (var msgType in node.PublishedMessages)
                        sb.AppendLine($" 📢 ПУБЛИКУЕТ --> [{msgType.Name}]");
                }

                if (node.Subscriptions.Count > 0)
                {
                    foreach (var sub in node.Subscriptions)
                    {
                        // 🔥 КРАСИВЫЙ ВЫВОД: Теперь пишем "СЛУШАЕТ <-- [Ивент] в классе НазваниеКласса -> Метод()"
                        sb.AppendLine($" 🎧 СЛУШАЕТ <-- [{sub.MessageType.Name}] = _{sub.TargetClassName.ToLower()}.{sub.MethodName}()");
                    }
                }
            }

            sb.AppendLine("\n======= ⏱️ ПОСЛЕДНИЕ СИГНАЛЫ (ЛЕНТА СОБЫТИЙ) =======");

            var recentSignals = _signalTimeline.AsEnumerable().Reverse().Take(20);
            foreach (var signal in recentSignals)
            {
                sb.AppendLine($"[{signal.Time:HH:mm:ss.fff}] {signal.Source} пустил {signal.MessageType.Name}");
            }

            return sb.ToString();
        }
    }
}
