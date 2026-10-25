using HomeNetCore.Interfaces.Diagnostics;
using System.Text;

namespace HomeNetServices.Diagnostics
{
    public class EventBusGraphDumper
    {
        private readonly EventBusInspector _inspector;
        private readonly string _debugFolderPath;

        public EventBusGraphDumper(IEventInspector inspector)
        {
            _inspector = inspector as EventBusInspector ?? throw new ArgumentNullException(nameof(inspector));

            // Задаем гарантированный путь к папке дебага на Рабочем столе
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _debugFolderPath = Path.Combine(desktopPath, "debug");
        }

        /// <summary>
        /// Главный метод: вытаскивает данные из инспектора, делит на связи и ленту событий, 
        /// и сохраняет в два разных txt-файла.
        /// </summary>
        public void DumpGraphToFiles()
        {
            try
            {
                // Гарантируем наличие папки
                if (!Directory.Exists(_debugFolderPath))
                {
                    Directory.CreateDirectory(_debugFolderPath);
                }

                // 1. Получаем полный массив размеченных строк из профайлера
                var allLines = _inspector.GenerateObjectGraph();

                // 2. Разделяем граф на Связи и Ленту Событий
                // Связи — это всё, что до заголовка ленты событий
                var relationsLines = allLines
                    .TakeWhile(l => !l.Text.Contains("ПОСЛЕДНИЕ СИГНАЛЫ (ЛЕНТА СОБЫТИЙ)"))
                    .Select(l => l.Text);

                // Лента событий — это заголовок и всё, что идет после него
                var timelineLines = allLines
                    .SkipWhile(l => !l.Text.Contains("ПОСЛЕДНИЕ СИГНАЛЫ (ЛЕНТА СОБЫТИЙ)"))
                    .Select(l => l.Text);

                // 3. Формируем финальные пути к файлам
                string relationsPath = Path.Combine(_debugFolderPath, "eventbus_relations.txt");
                string timelinePath = Path.Combine(_debugFolderPath, "eventbus_timeline.txt");

                // 4. Физически пишем на диск (с перезаписью актуального состояния при вызове)
                File.WriteAllLines(relationsPath, relationsLines, Encoding.UTF8);
                File.WriteAllLines(timelinePath, timelineLines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GraphDumper] Ошибка при сохранении графа: {ex.Message}");
            }
        }
    }
}
