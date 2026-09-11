using HomeNetCore.Enums;
using System.Windows.Controls;
using WpfHomeNet.UiHelpers;

namespace WpfHomeNet.Controls
{
    /// <summary>
    /// Interaction logic for LogRenderControl.xaml
    /// </summary>
    public partial class LogRenderControl : UserControl
    {
        private readonly ILogRenderer _renderer;
        public LogRenderControl()
        {
            InitializeComponent();

            _renderer = new LogRenderer(LogTextBox);
        }

        // Прямой асинхронный метод для добавления логов из любой точки приложения
        public async Task AddLog(string text, LogLevel level, LogColor color, bool isAnimating)
        {
            if (_renderer != null)
            {
                await _renderer.AddLog(text, level, color, isAnimating);
            }
        }
    }
}
