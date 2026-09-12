using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using WpfHomeNet.ViewModels;

namespace WpfHomeNet
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainVm;

        public MainWindow(MainViewModel mainVm)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            DataContext = _mainVm;
            InitializeComponent();

            // 🚀 ЗАПУСКАЕМ АНИМАЦИЮ ВЫЛЕТА ОКНА ПРИ СТАРТЕ
            this.Loaded += MainWindow_Loaded;
        }

      
        // ====== 🎬 АНИМАЦИЯ ПОЯВЛЕНИЯ ПРИ СТАРТЕ (СТРОГИЙ МЕДЛЕННЫЙ ВЗЛЕТ) ======
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double targetTop = (screenHeight - this.Height) / 2;

            // 1. Принудительно ставим окно вниз перед стартом, чтобы сбросить любые авто-сдвиги
            this.Top = screenHeight + 100;

            // 2. Настраиваем сильное сглаживание (Power = 3 делает торможение в конце ещё более мягким)
            var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };

            var startAnimation = new DoubleAnimation
            {
                // ⚡️ ЯВНО фиксируем старт и финиш, чтобы WPF не срезал время анимации
                From = screenHeight + 100,
                To = targetTop,

                // Попробуем поставить 1.8 секунды — при жестком From/To это будет прямо вальяжный, тяжелый заплыв вверх
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = easeOut
            };

            // 3. Поехали! Теперь оно пойдет плавно с самой нижней точки
            this.BeginAnimation(Window.TopProperty, startAnimation);
        }



        // ====== 🎬 АНИМАЦИЯ УЕЗЖАНИЯ ПРИ КЛИКЕ НА КРЕСТИК ======
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            var easeIn = new QuadraticEase { EasingMode = EasingMode.EaseIn };

            var exitAnimation = new DoubleAnimation
            {
                To = screenHeight + 50, // Улетает вниз за пределы экрана
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = easeIn
            };

            // Жестко закрываем программу только ПОСЛЕ завершения анимации падения
            exitAnimation.Completed += (s, args) =>
            {
                Application.Current.Shutdown();
            };

            this.BeginAnimation(Window.TopProperty, exitAnimation);
        }

        private void WindowDrag_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
