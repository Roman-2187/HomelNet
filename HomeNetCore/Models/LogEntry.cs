using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums;

namespace HomeNetCore.Models
{
    public partial class LogEntry : ObservableObject
    {
        // Тулкит сам сгенерирует публичное свойство Text с вызовом PropertyChanged
        [ObservableProperty]
        private string _text = string.Empty;

        public LogLevel Level { get; }
        public string Namespace { get; }

        public LogEntry(LogLevel level, string @namespace)
        {
            Level = level;
            Namespace = @namespace;
        }
    }

}
