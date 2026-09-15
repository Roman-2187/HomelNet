using HomeNetCore.Models.Diagnostics;

namespace HomeNetServices.Services.Diagnostics
{
    
        public class EventBusInspector
        {
            // Индекс всех компонентов системы: Имя класса -> Объект узла. Сложность O(1)
            private readonly Dictionary<string, ComponentNode> _nodes = new();

            // Хронология сигналов (история) в виде объектов метаданных
            private readonly List<SignalEvent> _signalTimeline = new();

            /// <summary>
            /// Фиксация публикации: O(1) сложность, никаких переборов.
            /// </summary>
            public void RecordPublish(object sender, Type messageType)
            {
                // Наш бронежилет: если прилетит null, подставим безопасный маркер
                string componentName = sender?.GetType()?.Name ?? "UnknownSource";

                // Быстрое получение или создание узла за O(1)
                var node = GetOrCreateNode(componentName);

                // HashSet защитит от дубликатов типов сообщений внутри карточки
                if (!node.PublishedMessages.Contains(messageType))
                {
                    node.PublishedMessages.Add(messageType);
                }

                // Пишем структурированное событие в ленту таймлайна
                _signalTimeline.Add(new SignalEvent(DateTime.Now, componentName, messageType));
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
                    node.Subscriptions.Add(new SubscriptionLink(messageType, methodName));
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
            /// Генерация текстового отчета: обходим готовое дерево объектов.
            /// Идеально для вывода в нашу новую текстовую панель админки!
            /// </summary>
            public string GenerateReport()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("======= 🧠 ОБЪЕКТНЫЙ ГРАФ СИСТЕМЫ EVENTBUS =======");

                foreach (var node in _nodes.Values.OrderBy(n => n.Name))
                {
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


