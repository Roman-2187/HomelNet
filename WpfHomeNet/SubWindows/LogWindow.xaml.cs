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
        private const double InitialOffset = 5; // Наш зазор

        public LogWindow(ILogger logger)
        {
            InitializeComponent();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Принудительно отключаем авто-позиционирование
            this.WindowStartupLocation = WindowStartupLocation.Manual;

            // ЖЕЛЕЗОБЕТОННЫЙ ХАК: Слушаем реальное изменение видимости окна!
            this.IsVisibleChanged += LogWindow_IsVisibleChanged;

            _renderer = new LogRenderer(LogTextBox);
        }

        private void LogWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Как только окно переключилось в режим Visible (открылось)
            if ((bool)e.NewValue == true)
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null && mainWindow.IsLoaded)
                {
                    // Мгновенно выставляем стартовую позицию справа до отрисовки на экране!
                    this.Left = mainWindow.Left + mainWindow.Width + InitialOffset;
                    this.Top = mainWindow.Top;
                    this.Height = mainWindow.Height;
                    this.Width = 600;
                }
            }
        } 
        public async Task AddLog(string text, LogLevel level, LogColor color, bool isAnimating)
        {
            await _renderer.AddLog(text, level, color, isAnimating);
        }  
    }
}
