using System;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Messaging;
using WpfHomeNet.ViewModels;

namespace WpfHomeNet
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainVm;

        // Чистый DI: Контейнер сам передает готовую MainViewModel в конструктор окна!
        public MainWindow(MainViewModel mainVm)
        {
            InitializeComponent();

            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            // Соединяем окно и вьюмодель
            _mainVm.ConnectToMainWindow(this);

            // Назначаем контекст данных
            DataContext = _mainVm;

           
            this.ContentRendered += MainWindow_ContentRendered;
            
        }


        private void MainWindow_ContentRendered(object? sender, EventArgs e)
        {
            if (double.IsNaN(this.Left) || double.IsNaN(this.Top) || double.IsNaN(this.Width)) return;

            // Просто уведомляем систему, что главное окно готово
            _mainVm.EventBus.Publish(new WindowPositionChangedMessage(
                this.Left,
                this.Top,
                this.Width,
                this.Height,
                this.IsLoaded
            ));
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем окно логов через свойство вьюмодели безопасно
            _mainVm.LogWindow?.Close();
            Close();
        }

        private void WindowDrag_MouseDown(object sender, MouseButtonEventArgs e) => this.DragMove();
    }
}
