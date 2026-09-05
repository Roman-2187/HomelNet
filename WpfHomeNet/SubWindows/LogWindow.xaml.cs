using HomeNetCore.Data.Interfaces;
using HomeNetCore.Enums;
using System;
using System.ComponentModel; // 🔥 Нужен для CancelEventArgs
using System.Threading.Tasks;
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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.WindowStartupLocation = WindowStartupLocation.Manual;
            _renderer = new LogRenderer(LogTextBox);

            // 🔥 Капкан на закрытие: вместо уничтожения окна просто прячем его!
            this.Closing += LogWindow_Closing;
        }

        private void LogWindow_Closing(object? sender, CancelEventArgs e)
        {
            e.Cancel = true; // Отменяем полное уничтожение окна
            this.Hide();     // Просто скрываем с глаз
        }

        public async Task AddLog(string text, LogLevel level, LogColor color, bool isAnimating)
        {
            await _renderer.AddLog(text, level, color, isAnimating);
        }
    }
}
