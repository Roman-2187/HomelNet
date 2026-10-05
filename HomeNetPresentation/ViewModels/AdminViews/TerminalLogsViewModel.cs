using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Models;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class TerminalLogsViewModel : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;

        // Ссылка на текущую строку лога, которую мы сейчас анимируем
        private List<LogToken>? _currentLine;

        // Ссылка на последний активный токен (слово), куда прямо сейчас капают буквы
        private LogToken? _activeToken;

        // Символы, после которых мы железно "отрезаем" слово и начинаем новое
        private readonly HashSet<char> _splitters = new() { ' ', ']', ':', ',', ';' };

        // Главная коллекция строк лога для XAML
        public ObservableCollection<List<LogToken>> Logs { get; } = new();

        public TerminalLogsViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventBus.Subscribe<ILogQueueManager.LogMessageReceived>(OnLogReceived);
        }

        private void OnLogReceived(ILogQueueManager.LogMessageReceived msg)
        {
            if (msg.IsAnimating) // Идет кумулятивная печать строки
            {
                if (string.IsNullOrEmpty(msg.Text)) return;

                // 1. Парсим всю прилетевшую подросшую строку на отдельные слова
                // Используем те же разделители, но сохраняем их в тексте, чтобы логи не слиплись
                var tokensList = new List<LogToken>();
                string currentWord = "";

                foreach (char c in msg.Text)
                {
                    currentWord += c;

                    // Если дошли до разделителя — фиксируем слово как отдельный токен
                    if (_splitters.Contains(c))
                    {
                        tokensList.Add(new LogToken(currentWord, msg.Level));
                        currentWord = "";
                    }
                }

                // Если в конце осталось недописанное слово — тоже добавляем его в токены
                if (!string.IsNullOrEmpty(currentWord))
                {
                    tokensList.Add(new LogToken(currentWord, msg.Level));
                }

                // 2. Выводим результат в коллекцию
                if (_currentLine == null)
                {
                    // Самый первый символ строки
                    _currentLine = tokensList;
                    Logs.Add(_currentLine);
                    if (Logs.Count > 1000) Logs.RemoveAt(0);
                }
                else
                {
                    // 🔥 ТВОЯ РОДНАЯ ЖЕЛЕЗОБЕТОННАЯ ПОДМЕНА!
                    // Находим индекс и просто перезаписываем строчку новым списком слов
                    int index = Logs.IndexOf(_currentLine);
                    if (index >= 0)
                    {
                        _currentLine = tokensList;
                        Logs[index] = tokensList; // Шлём в XAML обновленный массив слов
                    }
                }
            }
            else // Конец сообщения (msg.IsAnimating == false)
            {
                if (_currentLine != null && _currentLine.Count > 0)
                {
                    var emptyLine = new List<LogToken> { new LogToken(" ", LogLevel.Info) };
                    Logs.Add(emptyLine);
                }
                _currentLine = null;
                _activeToken = null;
            }
        }



        public void Dispose() => _eventBus.Unsubscribe<ILogQueueManager.LogMessageReceived>(OnLogReceived);
    }
}
