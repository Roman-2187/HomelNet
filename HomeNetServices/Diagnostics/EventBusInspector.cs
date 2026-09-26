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
        // Индекс всех компонентов системы: Имя класса -> Объект узла. Сложность O(1)
        private readonly Dictionary<string, ComponentNode> _nodes = new();

        // Хронология сигналов (история) в виде объектов метаданных
        private readonly List<ComponentNode.SignalEvent> _signalTimeline = new();

        // 📋 НАШ ПЕРСОНАЛЬНЫЙ ЧЕРНЫЙ СПИСОК (АЛЯ .GITIGNORE)
        // Защищает ленту от циклического и технического спама
        private readonly HashSet<string> _ignoredMessageTypes = new()
        {
            "LogMessageReceived", // Шум от посимвольной/построчной печати логгера
            "TextChanged"         // Шум от программной смены текста на кнопках и контролах
        };

        /// <summary>
        /// 🦾 ЖЕЛЕЗОБЕТОННАЯ ФИКСАЦИЯ ПУБЛИКАЦИИ С ФИЛЬТРАЦИЕЙ СПАМА
        /// </summary>
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

            // Быстрое получение или создание узла за O(1) — граф компонентов строится ВСЕГДА
            var node = GetOrCreateNode(componentName);

            // HashSet внутри карточки компонента защитит от дубликатов
            if (!node.PublishedMessages.Contains(messageType))
            {
                node.PublishedMessages.Add(messageType);
            }

            // 🔥 ЧИСТЫЙ ФИЛЬТР СОБЫТИЙ:
            // Если тип сообщения находится в нашем "гит-игноре", 
            // мы обновляем только граф связей выше, но НЕ пишем это событие в живую ленту.
            if (_ignoredMessageTypes.Contains(messageType.Name))
            {
                return;
            }

            // Пишем в ленту таймлайна только важные архитектурные и бизнес-события
            _signalTimeline.Add(new ComponentNode.SignalEvent(DateTime.Now, componentName, messageType));
        }

        /// <summary>
        /// Фиксация подписки: вызывается строго при Subscribe в шине.
        /// </summary>
        public void RecordSubscribe(string componentName, Type messageType, string methodName)
        {
            var node = GetOrCreateNode(componentName);

            // Проверяем, нет ли уже точно такой же подписки, чтобы не плодить дубликаты в карточке
            bool alreadyExists = node.Subscriptions.Any(s => s.MessageType == messageType && s.MethodName == methodName);
            if (!alreadyExists)
            {
                node.Subscriptions.Add(new ComponentNode.SubscriptionLink(messageType, methodName));
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

        /// <summary>
        /// Генерация текстового отчета с разделением по ролям для максимальной читаемости
        /// </summary>
        public string GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("======= 🧠 ОБЪЕКТНЫЙ ГРАФ СИСТЕМЫ EVENTBUS =======");

            // Сортируем компоненты по алфавиту для идеального порядка на экране
            foreach (var node in _nodes.Values.OrderBy(n => n.Name))
            {
                if (node.PublishedMessages.Count == 0 && node.Subscriptions.Count == 0)
                    continue;

                sb.AppendLine($"\n[ КОМПОНЕНТ: {node.Name} ]");

                if (node.PublishedMessages.Count > 0)
                {
                    foreach (var msgType in node.PublishedMessages)
                        sb.AppendLine($"   📢 ПУБЛИКУЕТ  --> [{msgType.Name}]");
                }

                if (node.Subscriptions.Count > 0)
                {
                    foreach (var sub in node.Subscriptions)
                        sb.AppendLine($"   🎧 СЛУШАЕТ   <-- [{sub.MessageType.Name}] привязан к {sub.MethodName}()");
                }
            }

            sb.AppendLine("\n======= ⏱️ ПОСЛЕДНИЕ СИГНАЛЫ (ЛЕНТА СОБЫТИЙ) =======");

            // Берем последние 20 сигналов в обратном хронологическом порядке
            var recentSignals = _signalTimeline.AsEnumerable().Reverse().Take(20);
            foreach (var signal in recentSignals)
            {
                sb.AppendLine($"[{signal.Time:HH:mm:ss.fff}] {signal.Source} пустил {signal.MessageType.Name}");
            }

            return sb.ToString();
        }
    }
}
