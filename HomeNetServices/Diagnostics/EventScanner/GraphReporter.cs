using System.Text;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models.Diagnostics;

namespace HomeNetServices.Diagnostics.EventScanner
{
    public static class GraphReporter
    {
        public static List<IEventInspector.Line> BuildGraph(
            Dictionary<string, ComponentNode> nodes,
            Dictionary<Type, MessageState> registry,
            List<ComponentNode.SignalEvent> timeline,
            object scanLock)
        {
            var lines = new List<IEventInspector.Line>
            {
                new("======= 🧠 ОБЪЕКТНЫЙ ГРАФ СИСТЕМЫ EVENTBUS =======", MessageState.None)
            };

            foreach (var node in nodes.Values.OrderBy(n => n.Name))
            {
                if (node.PublishedMessages.Count == 0 && node.Subscriptions.Count == 0) continue;

                lines.Add(new($"[ КОМПОНЕНТ: {node.Name} ]", MessageState.None));

                // Сборщик Публикаций
                foreach (var msgType in node.PublishedMessages)
                {
                    MessageState state;
                    lock (scanLock) { state = registry.TryGetValue(msgType, out var s) ? s : MessageState.None; }

                    var line = (state & MessageState.Subscribed) == 0
                        ? new IEventInspector.Line($" 📢 ПУБЛИКУЕТ --> 🚨 [ПУСТОТА!] [{msgType.Name}] <-- 🛑 НЕТ ПОДПИСЧИКОВ!", MessageState.Published)
                        : new IEventInspector.Line($" 📢 ПУБЛИКУЕТ --> ✅ [АКТИВЕН]  [{msgType.Name}]", MessageState.Live);
                    lines.Add(line);
                }

                // Сборщик Подписок
                foreach (var sub in node.Subscriptions)
                {
                    MessageState state;
                    lock (scanLock) { state = registry.TryGetValue(sub.MessageType, out var s) ? s : MessageState.None; }

                    var line = (state & MessageState.Published) == 0
                        ? new IEventInspector.Line($" 🎧 СЛУШАЕТ  <-- 🚨 [КРИТ_ОШИБКА] [{sub.MessageType.Name}] = _{sub.TargetClassName.ToLower()}.{sub.MethodName}() <-- 🛑 КОД ПУБЛИКАЦИИ ФИЗИЧЕСКИ ОТСУТСТВУЕТ В ПРОЕКТЕ!", MessageState.Subscribed)
                        : new IEventInspector.Line($" 🎧 СЛУШАЕТ  <-- ✅ [АКТИВЕН]  [{sub.MessageType.Name}] = _{sub.TargetClassName.ToLower()}.{sub.MethodName}()", MessageState.Live);
                    lines.Add(line);
                }
            }

            lines.Add(new IEventInspector.Line("======= ⏱️ ПОСЛЕДНИЕ СИГНАЛЫ (ЛЕНТА СОБЫТИЙ) =======", MessageState.None));
            lock (scanLock)
            {
                foreach (var signal in timeline.AsEnumerable().Reverse())
                {
                    lines.Add(new($"[{signal.Time:HH:mm:ss.fff}] {signal.Source} пустил {signal.MessageType.Name}", MessageState.None));
                }
            }

            WriteFiles(lines);
            return lines;
        }

        private static void WriteFiles(List<IEventInspector.Line> allLines)
        {
            try
            {
                string debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "debug");
                if (!Directory.Exists(debugPath)) Directory.CreateDirectory(debugPath);

                // Аккуратно пилим логику по разделителю заголовка ленты событий
                var relLines = allLines.TakeWhile(l => !l.Text.Contains("ПОСЛЕДНИЕ СИГНАЛЫ")).ToList();
                var timeLines = allLines.SkipWhile(l => !l.Text.Contains("ПОСЛЕДНИЕ СИГНАЛЫ")).ToList();

                // Дампим сочные HTML-страницы прямо на десктоп
                File.WriteAllText(Path.Combine(debugPath, "eventbus_relations.html"), BuildHtmlPage("Архитектурный Граф EventBus", relLines), Encoding.UTF8);
                File.WriteAllText(Path.Combine(debugPath, "eventbus_timeline.html"), BuildHtmlPage("Лента Событий (Рантайм)", timeLines), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GraphReporter] Ошибка записи HTML-файлов: {ex.Message}");
            }
        }

        private static string BuildHtmlPage(string title, List<IEventInspector.Line> lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='utf-8'>");
            sb.AppendLine($"   <title>{title}</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { background-color: #1e1e1e; color: #d4d4d4; font-family: 'Consolas', 'Courier New', monospace; padding: 20px; line-height: 1.6; }");
            sb.AppendLine("        h1 { color: #569cd6; border-bottom: 2px solid #3e3e3e; padding-bottom: 10px; font-size: 22px; margin-bottom: 20px; }");
            sb.AppendLine("        .component-card { background-color: #252526; border: 1px solid #3e3e3e; border-radius: 6px; padding: 15px; margin-bottom: 15px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }");
            sb.AppendLine("        .component-title { color: #4fc1ff; font-weight: bold; font-size: 16px; margin-bottom: 10px; display: block; border-left: 4px solid #007acc; padding-left: 8px; }");
            sb.AppendLine("        .line { white-space: pre-wrap; display: block; padding: 4px 8px; border-radius: 4px; margin: 2px 0; font-size: 14px; }");
            // CSS Подсветка статусов
            sb.AppendLine("        .state-live { color: #4ec9b0; background-color: rgba(78, 201, 176, 0.06); border-left: 3px solid #4ec9b0; }");
            sb.AppendLine("        .state-published { color: #dcdcaa; background-color: rgba(220, 220, 170, 0.04); border-left: 3px solid #dcdcaa; }");
            sb.AppendLine("        .state-subscribed { color: #f44336; background-color: rgba(244, 67, 54, 0.08); font-weight: bold; border-left: 3px solid #f44336; }");
            sb.AppendLine("        .state-none { color: #9cdcfe; background-color: rgba(156, 220, 254, 0.03); }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine($"    <h1>{title}</h1>");

            bool isInCard = false;

            foreach (var line in lines)
            {
                // Если строка — это заголовок компонента, упаковываем его в красивую VS-карточку
                if (line.Text.StartsWith("[ КОМПОНЕНТ:"))
                {
                    if (isInCard) sb.AppendLine("    </div>"); // Закрываем предыдущую карточку
                    sb.AppendLine("    <div class='component-card'>");
                    sb.AppendLine($"        <span class='component-title'>{HtmlEncode(line.Text)}</span>");
                    isInCard = true;
                    continue;
                }

                // Для разделителей верхнего и нижнего уровня
                if (line.Text.StartsWith("===="))
                {
                    if (isInCard) { sb.AppendLine("    </div>"); isInCard = false; }
                    sb.AppendLine($"    <div style='color:#6a9955; font-weight:bold; margin: 15px 0;'>{HtmlEncode(line.Text)}</div>");
                    continue;
                }

                string cssClass = "state-none";
                if (line.State == MessageState.Live) cssClass = "state-live";
                else if (line.State == MessageState.Published) cssClass = "state-published";
                else if (line.State == MessageState.Subscribed) cssClass = "state-subscribed";

                // Обычная строка данных (подписка/публикация/сигнал)
                string padding = isInCard ? "        " : "    ";
                sb.AppendLine($"{padding}<span class='line {cssClass}'>{HtmlEncode(line.Text)}</span>");
            }

            if (isInCard) sb.AppendLine("    </div>"); // Закрываем последнюю карточку

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private static string HtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
