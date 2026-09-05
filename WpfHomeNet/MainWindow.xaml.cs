using System;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Messaging;
using WpfHomeNet.ViewModels;
using WpfHomeNet.UiHelpers; // 🔥 Не забываем для LogQueueManager

namespace WpfHomeNet
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainVm;
        private readonly LogWindow _logWindow; // 🔥 Прямая ссылка на соседа
        private readonly LogQueueManager _queueManager; // 🔥 Вытащили сюда из LogViewModel

        public MainWindow(MainViewModel mainVm, LogWindow logWindow, LogQueueManager logQueueManager)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            _logWindow = logWindow ?? throw new ArgumentNullException(nameof(logWindow));
            _queueManager = logQueueManager ?? throw new ArgumentNullException(nameof(logQueueManager));

            DataContext = _mainVm;
            InitializeComponent();

            // 1. Окна договариваются о координатах НАПРЯМУЮ без спама в шину! ⚡
            this.ContentRendered += (s, e) => PositionLogWindow();
            this.LocationChanged += (s, e) => PositionLogWindow();
            this.SizeChanged += (s, e) => PositionLogWindow();

            // 2. Ловим команду видимости логов прямо здесь, на UI-фасаде
            _mainVm.EventBus.Subscribe<LogWindowVisibilityChangedMessage>(OnVisibilityCommandReceived);
        }

        private void PositionLogWindow()
        {
            if (!this.IsLoaded || _logWindow == null) return;

            // Строгая привязка лога к правому краю главного окна
            _logWindow.Left = this.Left + this.Width;
            _logWindow.Top = this.Top;
            _logWindow.Height = this.Height;
            _logWindow.Width = 650;
        }

        private void OnVisibilityCommandReceived(LogWindowVisibilityChangedMessage msg)
        {
            if (msg.IsVisible)
            {
                _logWindow.Show();
                PositionLogWindow(); // Сразу корректируем позицию при показе

                // 🔥 Важная логика ядра: будим наш канал логов при первом открытии!
                _queueManager.SetReady();
            }
            else
            {
                _logWindow.Hide();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void WindowDrag_MouseDown(object sender, MouseButtonEventArgs e) => this.DragMove();
    }
}
