using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation; // 🔥 Подключаем движок анимаций Microsoft!
using WpfHomeNet.Messaging;
using WpfHomeNet.UiHelpers; // 🔥 Не забываем для LogQueueManager
using WpfHomeNet.ViewModels;
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
            _logWindow.Show();
            PositionLogWindow();
            _logWindow.Hide();

            this.ContentRendered += (s, e) => PositionLogWindow();
            this.LocationChanged += (s, e) => PositionLogWindow();
            this.SizeChanged += (s, e) => PositionLogWindow();

            _mainVm.EventBus.Subscribe<LogWindowVisibilityChangedMessage>(OnVisibilityCommandReceived);

           


        }



       


        private void PositionLogWindow()
        {
            if (!this.IsLoaded || _logWindow == null) return;

            // 1. Узнаем полную ширину текущего рабочего стола (без панели задач)
            double screenWidth = SystemParameters.WorkArea.Width;

            // 2. Рассчитываем левую границу лога (она прилипла к правому боку главного окна)
            double logLeft = this.Left + this.Width;

            // 3. Железно привязываем координаты
            _logWindow.Left = logLeft;
            _logWindow.Top = this.Top;
            _logWindow.Height = this.Height;

            // 4. 🔥 МАГИЯ: Ширина лога — это строго ВСЁ оставшееся место до правого края экрана!
            // Если главное окно уехало в ноль, то лог займет вообще всё свободное пространство справа!
            double remainingWidth = screenWidth - logLeft;

            // Страховка, чтобы ширина не ушла в минус, если главное окно частично вылезло за экран
            _logWindow.Width = remainingWidth > 0 ? remainingWidth : 100;
        }






        private double _originalLeftPosition; // 📌 Переменная-память: запомнит, где окно стояло изначально

    private void OnVisibilityCommandReceived(LogWindowVisibilityChangedMessage msg)
    {
        // Настраиваем плавное замедление для обеих анимаций
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        if (msg.IsVisible)
        {
            // 🔥 Шаг 1: Запоминаем текущую координату КРАЙНИЙ раз, перед тем как уехать
            _originalLeftPosition = this.Left;

            var winMoveLeftAnimation = new DoubleAnimation
            {
                To = 0, // Уезжаем к левому краю
                Duration = TimeSpan.FromMilliseconds(800), // Плавная, вальяжная скорость
                EasingFunction = ease
            };

            this.BeginAnimation(Window.LeftProperty, winMoveLeftAnimation);

            _logWindow.Show();
            _queueManager.SetReady();
        }
        else
        {
            // 🔥 Шаг 2: При скрытии логов плавно возвращаем окно на сохраненное место!
            var winMoveBackAnimation = new DoubleAnimation
            {
                To = _originalLeftPosition, // Едем обратно домой 🏠
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = ease
            };

            // Лог-окно сначала прячем, а главное красиво уезжает назад
            _logWindow.Hide();
            this.BeginAnimation(Window.LeftProperty, winMoveBackAnimation);
        }
    }






    private void CloseButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void WindowDrag_MouseDown(object sender, MouseButtonEventArgs e) => this.DragMove();
    }
}
