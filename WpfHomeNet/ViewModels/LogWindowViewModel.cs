using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WpfHomeNet.ViewModels
{
    public class LogViewModel : INotifyPropertyChanged, IDisposable
    {
        public MainViewModel MainVm
        {
            get => _mainVm ?? throw new InvalidOperationException($"{nameof(_mainVm)} не инициализирован");
            set => _mainVm = value;
        }

        private  MainViewModel? _mainVm;
        private bool _isSubscribed;

        public double Offset { get; set; } = 5;

        public void ConnectToMainViewModel(MainViewModel mainVm)
        {
            MainVm = mainVm;
            
            PositionLogWindow();

            if (!_isSubscribed)
            {
                MainVm.MainWindow.LocationChanged += OnMainWindowMoved;
                MainVm.MainWindow.SizeChanged += OnMainWindowResized;
                _isSubscribed = true;
            }
        }

        public void PositionLogWindow()
        {
            //if (_mainVm.LogWindow.Visibility != Visibility.Visible) return;

            var mainWindow = MainVm.MainWindow;
            if ( !mainWindow.IsLoaded) return;

            MainVm.LogWindow.Left = mainWindow.Left + mainWindow.Width + Offset;
            MainVm.LogWindow.Top = mainWindow.Top;
            MainVm.LogWindow.Height = mainWindow.Height;
            MainVm.LogWindow.Width = 600;
        }

        private void OnMainWindowMoved(object? sender, EventArgs e) => PositionLogWindow();
        private void OnMainWindowResized(object sender, SizeChangedEventArgs e) => PositionLogWindow();

        public void Show() => MainVm.LogWindow.Show();
        public void Hide() => MainVm.LogWindow.Hide();
        public bool IsVisible => MainVm.LogWindow.Visibility == Visibility.Visible;

        public void Dispose()
        {
            if (_isSubscribed)
            {
                MainVm.MainWindow.LocationChanged -= OnMainWindowMoved;
                MainVm.MainWindow.SizeChanged -= OnMainWindowResized;
                _isSubscribed = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
