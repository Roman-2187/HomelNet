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
            // 1. Сначала жёстко забираем вьюмодель и проверяем на null! 🧼
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            // 2. СРАЗУ отдаём её в DataContext, пока окно ещё слепое! 🦾⚡
            DataContext = _mainVm;

            // 3. И только теперь, когда мозг на месте, запускаем сборку интерфейса!
            InitializeComponent();

            // Подписки на движение окна
            this.ContentRendered += (s, e) => SendCoordinatesToBus();
            this.LocationChanged += (s, e) => SendCoordinatesToBus();
            this.SizeChanged += (s, e) => SendCoordinatesToBus();
        }





        private void SendCoordinatesToBus()
        {
            // Окно само громко кричит в шину: «Я сдвинулось, вот мои новые размеры!»
            _mainVm.EventBus.Publish(new WindowPositionChangedMessage(
                this.Left,
                this.Top,
                this.Width,
                this.Height,
                this.IsLoaded
            ));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)=>Application.Current.Shutdown();
        

        private void WindowDrag_MouseDown(object sender, MouseButtonEventArgs e) => this.DragMove();
    }
}
