using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace WpfHomeNet.Helpers
{
    public static class FlyOutVisibilityManager
    {
        public static readonly DependencyProperty IsAnimatedVisibilityProperty =
            DependencyProperty.RegisterAttached(
                "IsAnimatedVisibility",
                typeof(Visibility),
                typeof(FlyOutVisibilityManager),
                new PropertyMetadata(Visibility.Collapsed, OnVisibilityChanged));

        public static Visibility GetIsAnimatedVisibility(DependencyObject obj) => (Visibility)obj.GetValue(IsAnimatedVisibilityProperty);
        public static void SetIsAnimatedVisibility(DependencyObject obj, Visibility value) => obj.SetValue(IsAnimatedVisibilityProperty, value);

        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                var newVisibility = (Visibility)e.NewValue;

                if (newVisibility == Visibility.Visible)
                {
                    // 1. Сначала делаем элемент физически видимым
                    element.Visibility = Visibility.Visible;
                    // 2. Включаем состояние прилета
                    VisualStateManager.GoToState(element, "Opened", true);
                }
                else
                {
                    // 1. Переключаем состояние на улет (Closed)
                    VisualStateManager.GoToState(element, "Closed", true);

                    // 2. Ждем 0.5 сек (время нашей анимации закрытия) и физически скрываем элемент
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(0.5)
                    };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        // Проверяем, не открыли ли форму заново, пока она улетала
                        if (GetIsAnimatedVisibility(element) != Visibility.Visible)
                        {
                            element.Visibility = newVisibility;
                        }
                    };
                    timer.Start();
                }
            }
        }
    }
}
