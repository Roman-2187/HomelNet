using System.ComponentModel;
using System.Windows;

namespace WpfHomeNet.ViewModels
{
    public class LogViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Window _logWindow;
        private readonly MainViewModel _mainVm;

        public LogViewModel(Window logWindow, MainViewModel mainVm)
        {
            _logWindow = logWindow ?? throw new ArgumentNullException(nameof(logWindow));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            // Подписываемся на события главного окна через MainViewModel
            _mainVm.MainWindow.LocationChanged += OnMainWindowMoved;
            _mainVm.MainWindow.SizeChanged += OnMainWindowResized;
        }

        // Синхронизация позиции лог‑окна
        private void PositionLogWindow()
        {
            if (_logWindow.Visibility != Visibility.Visible) return;

            _logWindow.Left = _mainVm.MainWindow.Left + _mainVm.MainWindow.Width + 5;
            _logWindow.Top = _mainVm.MainWindow.Top;
            _logWindow.Height = _mainVm.MainWindow.Height;
            _logWindow.Width = 600;
        }

        private void OnMainWindowMoved(object sender, EventArgs e) => PositionLogWindow();
        private void OnMainWindowResized(object sender, SizeChangedEventArgs e) => PositionLogWindow();

        // Управление видимостью
        public void Show() => _logWindow.Show();
        public void Hide() => _logWindow.Hide();

        public bool IsVisible => _logWindow.Visibility == Visibility.Visible;

        public void Dispose()
        {
            _mainVm.MainWindow.LocationChanged -= OnMainWindowMoved;
            _mainVm.MainWindow.SizeChanged -= OnMainWindowResized;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
