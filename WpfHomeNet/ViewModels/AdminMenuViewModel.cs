using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.UiHelpers;



namespace WpfHomeNet.ViewModels
{
    public class AdminMenuViewModel : INotifyPropertyChanged
    {
        private LogWindow _logWindow;
        private Window _mainWindow;
        
        private readonly LogQueueManager _logQueueManager;
        private bool _isSubscribedToMainWindowEvents;
        // Команда-переключатель
        public ICommand ToggleLogWindowCommand { get; }

        // Свойство для текста кнопки
        private string _toggleButtonText = "Показать лог";
        public string ToggleButtonText
        {
            get => _toggleButtonText;
            set
            {
                _toggleButtonText = value;
                OnPropertyChanged(nameof(ToggleButtonText));
            }
        }

        public AdminMenuViewModel(LogWindow logWindow, Window mainWindow, LogQueueManager logQueueManager)
        {
            _logWindow = logWindow;

            _logQueueManager = logQueueManager;

            _mainWindow = mainWindow;



            ToggleLogWindowCommand = new RelayCommand(ExecuteToggleLogWindow);
        }

        private void ExecuteToggleLogWindow(object? parameter)
        {
            if (_logWindow.Visibility == Visibility.Visible)
            {
                // Скрываем окно
                _logWindow.Hide();
                // Логика при скрытии
                ToggleButtonText = "Показать лог"; // Меняем текст кнопки

                if (_isSubscribedToMainWindowEvents)
                {
                    _mainWindow.LocationChanged -= OnMainWindowMoved;
                    _mainWindow.SizeChanged -= OnMainWindowResized;
                    _isSubscribedToMainWindowEvents = false;
                }
            }
            else
            {
                // При первом показе — подписываемся на события
                if (!_isSubscribedToMainWindowEvents)
                {
                    _mainWindow.LocationChanged += OnMainWindowMoved;
                    _mainWindow.SizeChanged += OnMainWindowResized;
                    _isSubscribedToMainWindowEvents = true;
                }

                PositionLogWindow();
                _logWindow.Show();
                _logQueueManager.SetReady();
                ToggleButtonText = "Скрыть лог";
            }
        }



        public void ConnectMainWindow(Window mainWindow)
        {
            _mainWindow = mainWindow;

            // Только теперь подписываемся на события
            _mainWindow.LocationChanged += OnMainWindowMoved;
            _mainWindow.SizeChanged += OnMainWindowResized;

            // Если лог-окно уже видно — сразу позиционируем
            if (_logWindow.Visibility == Visibility.Visible)
                PositionLogWindow();
        }




        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void PositionLogWindow()
        {
            _logWindow.Left = _mainWindow.Left + _mainWindow.Width +5;
            _logWindow.Top = _mainWindow.Top;
            _logWindow.Height = _mainWindow.Height;
            _logWindow.Width = 600;
        }

        private void OnMainWindowMoved(object sender, EventArgs e)
        {
            if (_logWindow.Visibility == Visibility.Visible) PositionLogWindow();
        }

        private void OnMainWindowResized(object sender, SizeChangedEventArgs e)
        {
            if (_logWindow.Visibility == Visibility.Visible) PositionLogWindow();
        }


        public void Dispose()
        {
            _mainWindow.LocationChanged -= OnMainWindowMoved;
            _mainWindow.SizeChanged -= OnMainWindowResized;
        }
    }
}
