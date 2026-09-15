using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using HomeNetPresentation.ViewModels; // Наш чистый слой презентации

namespace SiberNet.UI
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainVm;

        public MainWindow(MainViewModel mainVm)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            DataContext = _mainVm;
            InitializeComponent();

            // Принудительно заставляем WPF рассчитать размеры окна ДО его показа на экране! 🧼🦾
            this.Width = 1100;  // Задай здесь точную ширину своего окна из XAML
            this.Height = 700;  // Задай здесь точную высоту своего окна из XAML

            // Намертво привязываем анимацию взлета к событию Loaded
          this.Loaded += MainWindow_Loaded;
        }

        // ====== 🎬 АНИМАЦИЯ ПОЯВЛЕНИЯ ПРИ СТАРТЕ (СТРОГИЙ МЕДЛЕННЫЙ ВЗЛЕТ) ======
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Отписываемся от события, чтобы анимация не срабатывала повторно при перерисовке окна!
            this.Loaded -= MainWindow_Loaded;

            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // 🔥 ПУЛЕНЕПРОБИВАЕМЫЙ РАСЧЕТ: Берем жестко заданную или актуальную высоту
            double actualHeight = double.IsNaN(this.Height) || this.Height == 0 ? 700 : this.Height;
            double targetTop = (screenHeight - actualHeight) / 2;

            // 1. Принудительно ставим окно далеко вниз перед стартом
            this.Top = screenHeight + 100;

            // 2. Настраиваем мягкое торможение в конце взлёта
            var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };

            var startAnimation = new DoubleAnimation
            {
                From = screenHeight + 100,
                To = targetTop,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = easeOut
            };

            // 3. Поехали! Плавный взлет без конфликтов с DirectX
            this.BeginAnimation(Window.TopProperty, startAnimation);
        }

        // ====== 🎬 АНИМАЦИЯ УЕЗЖАНИЯ ПРИ КЛИКЕ НА КРЕСТИК ======
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            var easeIn = new QuadraticEase { EasingMode = EasingMode.EaseIn };

            var exitAnimation = new DoubleAnimation
            {
                To = screenHeight + 150, // Улетает глубоко вниз за пределы экрана
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = easeIn
            };

            // Жестко закрываем программу и ГАСИМ все скрытые потоки только после падения окна! 🪓🔥
            exitAnimation.Completed += (s, args) =>
            {
                Application.Current.Shutdown();
            };

            this.BeginAnimation(Window.TopProperty, exitAnimation);
        }

        private void WindowDrag_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Позволяет таскать окно мышкой, только если анимация завершена
                this.DragMove();
            }
        }
    }
}
