using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;

namespace HomeNetPresentation.ViewModels
{
    // 🔥 Наша чистая модель символа: хранит букву и её уровень лога для XAML-триггера
    public record LogChar(char Value, LogLevel Level);

    public partial class TerminalViewModel : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;
        private List<LogChar>? _currentLine;

        // Главная коллекция строк. Каждая строка — это простой список чаров.
        public ObservableCollection<List<LogChar>> Logs { get; } = new();

        public TerminalViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventBus.Subscribe<ILogQueueManager.LogMessageReceived>(OnLogReceived);
        }

        private void OnLogReceived(ILogQueueManager.LogMessageReceived msg)
        {
            if (msg.IsAnimating) // Идет печать букв от менеджера логов
            {
                if (!string.IsNullOrEmpty(msg.Text))
                {
                    // 1. Создаем СВЕЖИЙ список чаров на каждом шаге анимации строки
                    var newLine = new List<LogChar>();

                    foreach (char c in msg.Text)
                    {
                        newLine.Add(new LogChar(c, msg.Level));
                    }

                    if (_currentLine == null)
                    {
                        // Если это самое начало строки лога — просто добавляем её в коллекцию
                        _currentLine = newLine;
                        Logs.Add(_currentLine);

                        if (Logs.Count > 1000) Logs.RemoveAt(0);
                    }
                    else
                    {
                        // 🔥 ВОТ ОНА, ЖЕЛЕЗОБЕТОННАЯ ПОДМЕНА:
                        // Находим индекс текущей строки и пихаем туда НОВУЮ ссылку.
                        // WPF мгновенно увидит изменение и дорисует буквы по горизонтали!
                        int index = Logs.IndexOf(_currentLine);
                        if (index >= 0)
                        {
                            _currentLine = newLine; // Обновляем локальную закладку
                            Logs[index] = newLine;  // Стреляем в XAML новой ссылкой
                        }
                    }
                }
            }

            else // Сигнал конца сообщения (msg.IsAnimating == false)
            {
                if (_currentLine != null && _currentLine.Count > 0)
                {
                    // Пустая строка-разделитель (ей даем дефолтный уровень)
                    var emptyLine = new List<LogChar> { new LogChar(' ', LogLevel.Information) };
                    Logs.Add(emptyLine);
                }
                _currentLine = null;
            }
        }

        public void Dispose() => _eventBus.Unsubscribe<ILogQueueManager.LogMessageReceived>(OnLogReceived);
    }
}
