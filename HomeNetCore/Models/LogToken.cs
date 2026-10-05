using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums;


namespace HomeNetCore.Models
{
    
    public partial class LogToken : ObservableObject

    {
        [ObservableProperty]
        private string _text = string.Empty;

        public LogLevel Level { get; }

        public LogToken(string text, LogLevel level)
        {
            _text = text;
            Level = level;
        }

        // Метод быстрого добавления символа без пересоздания строки
        public void Append(char c)
        {
            Text += c;
        }
    }
   
}





