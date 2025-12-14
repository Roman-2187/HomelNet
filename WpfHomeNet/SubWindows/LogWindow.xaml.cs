using HomeNetCore.Data.Interfaces;
using HomeNetCore.Enums;
using System.Windows;
using WpfHomeNet.UiHelpers;

namespace WpfHomeNet
{ 
    public partial class LogWindow : Window
    {
        private readonly ILogger _logger;
        private readonly ILogRenderer _renderer;


        public LogWindow(ILogger logger)
        {
            InitializeComponent();
            _logger = logger;

            // Получаем TextBox из XAML по его имени
            if (LogTextBox == null)
                throw new InvalidOperationException("TextBox не найден в XAML");

            _renderer = new LogRenderer(LogTextBox);           
        }
        
        public async Task AddLog(string text, LogLevel level, LogColor color, bool isAnimating)
        {
            await _renderer.AddLog(text, level, color, isAnimating);
        }        
    }

}