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

        public MainWindow(MainViewModel mainVm)
        {
            InitializeComponent();

            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
      
            DataContext = _mainVm;

            // СЦЕПЛЕНИЕ ЧЕРЕЗ АВТОБУС: Окно само подписывается на свои сдвиги
            // и при любом чихе швыряет свежие координаты в шину!
            this.ContentRendered += (s, e) => SendCoordinatesToBus();
            this.LocationChanged += (s, e) => SendCoordinatesToBus();
            this.SizeChanged += (s, e) => SendCoordinatesToBus();
        }




        private void SendCoordinatesToBus()
        {
            // Защита от дурака при инициализации
            if (double.IsNaN(this.Left) || double.IsNaN(this.Top) || double.IsNaN(this.Width)) return;

            // Окно само громко кричит в шину: «Я сдвинулось, вот мои новые размеры!»
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
