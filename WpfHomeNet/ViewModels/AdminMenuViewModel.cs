using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Controls;
using WpfHomeNet.UiHelpers;



namespace WpfHomeNet.ViewModels
{
    public class AdminMenuViewModel : INotifyPropertyChanged
    { 
        private bool _isSubscribedToMainWindowEvents;

        private MainViewModel? _mainVm;
        public MainViewModel MainVm => _mainVm ?? throw new InvalidOperationException("_mainVm не инициализирован");
       
        private readonly LogQueueManager _logQueueManager;

       



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

        public AdminMenuViewModel( LogQueueManager logQueueManager
            )
        {
            _logQueueManager = logQueueManager;

            ToggleLogWindowCommand = new RelayCommand(ExecuteToggleLogWindow); 
        }


        public void ConnectToMainViewModel(MainViewModel mainVm) => _mainVm = mainVm;

        private void ExecuteToggleLogWindow(object? parameter)
        {
            if (MainVm.LogWindow.Visibility == Visibility.Visible)
            {              
                MainVm.LogWindow.Hide();
              
                ToggleButtonText = "Показать лог"; 

                if (_isSubscribedToMainWindowEvents)
                {
                    MainVm.MainWindow.LocationChanged -= OnMainWindowMoved;
                    MainVm.MainWindow.SizeChanged -= OnMainWindowResized;
                    _isSubscribedToMainWindowEvents = false;
                }
            }
            else
            {
                // При первом показе — подписываемся на события
                if (!_isSubscribedToMainWindowEvents)
                {
                    MainVm.MainWindow.LocationChanged += OnMainWindowMoved;
                    MainVm.MainWindow.SizeChanged += OnMainWindowResized;
                    _isSubscribedToMainWindowEvents = true;
                }

                PositionLogWindow();
                MainVm.LogWindow.Show();
                _logQueueManager.SetReady();
                ToggleButtonText = "Скрыть лог";
            }
        }

        
       


        private void PositionLogWindow()
        {
            MainVm.LogWindow.Left = MainVm.MainWindow.Left + MainVm.MainWindow.Width +5;
            MainVm.LogWindow.Top = MainVm.MainWindow.Top;
            MainVm.LogWindow.Height = MainVm.MainWindow.Height;
            MainVm.LogWindow.Width = 600;
        }

        private void OnMainWindowMoved(object? sender, EventArgs e)
        {
            if (MainVm.LogWindow.Visibility == Visibility.Visible) PositionLogWindow();
        }

        private void OnMainWindowResized(object sender, SizeChangedEventArgs e)
        {
            if (MainVm.LogWindow.Visibility == Visibility.Visible) PositionLogWindow();
        }


        public void Dispose()
        {
            MainVm.MainWindow.LocationChanged -= OnMainWindowMoved;
            MainVm.MainWindow.SizeChanged -= OnMainWindowResized;
        }
    }
}
