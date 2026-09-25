using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Diagnostics; // Убедись, что этот namespace верный

namespace HomeNetServices.Diagnostics
{
    public class EventBusInspector : IEventInspectorSource
    {
        // Индекс всех компонентов системы: Имя класса -> Объект узла. Сложность O(1)
        private readonly Dictionary<string, ComponentNode> _nodes = new();

        // Хронология сигналов (история) в виде объектов метаданных
        private readonly List<ComponentNode.SignalEvent> _signalTimeline = new();

        /// <summary>
        /// 🦾 ЖЕЛЕЗОБЕТОННАЯ ФИКСАЦИЯ ПУБЛИКАЦИИ
        /// </summary>
        public void RecordPublish(object sender, Type messageType)
        {
            string componentName = "UnknownSource";

            if (sender != null)
            {
                var senderType = sender.GetType();

                // 🧠 ЛАЙФХАК: Если отправителем случайно указали саму шину EventBus,
                // то мы попробуем вытащить имя реального класса из контекста, 
                // но если там чистый вызов — берем имя типа отправителя.
                componentName = senderType.Name;

                // Если имя получилось слишком общим (например, "Object"), подстрахуемся
                if (componentName == "Object" && sender is string strName)
                {
                    componentName = strName;
                }
            }

            // Быстрое получение или создание узла за O(1) — теперь сервисы ТОЧНО получат свой узел! 🧼
            var node = GetOrCreateNode(componentName);

            // HashSet защитит от дубликатов типов сообщений внутри карточки
            if (!node.PublishedMessages.Contains(messageType))
            {
                node.PublishedMessages.Add(messageType);
            }

            // Пишем структурированное событие в ленту таймлайна
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
                // Пропускаем вывод пустых или технических узлов, если они случайно проскочили
                if (node.PublishedMessages.Count == 0 && node.Subscriptions.Count == 0)
                    continue;

                sb.AppendLine($"\n[ КОМПОНЕНТ: {node.Name} ]");

                // Выводим то, что компонент генерирует в систему
                if (node.PublishedMessages.Count > 0)
                {
                    foreach (var msgType in node.PublishedMessages)
                        sb.AppendLine($"   📢 ПУБЛИКУЕТ  --> [{msgType.Name}]");
                }

                // Выводим то, на что компонент подписан
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