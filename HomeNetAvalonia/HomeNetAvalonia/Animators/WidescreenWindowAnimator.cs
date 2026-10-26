using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.ViewModels;
using System;

namespace HomeNetAvalonia.Animators
{
    public class WidescreenWindowAnimator : IDisposable
    {
        private readonly IEventBus _eventBus;
        private const double DeltaWidth = 100; // 50 пикселей с левой стороны + 50 пикселей с правой стороны
        private bool _isWidescreen = false;

        public WidescreenWindowAnimator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // 🎧 Подписываемся на потерявшееся событие из AdminViewModel
            // Примечание: Укажи точный тип события ToggleWidescreen, если он объявлен внутри AdminViewModel (например, AdminViewModel.ToggleWidescreen)
            _eventBus.Subscribe<IWidescreenService.ToggleWidescreen>(OnToggleWidescreenRequested);
        }

        private void OnToggleWidescreenRequested(IWidescreenService.ToggleWidescreen msg)
        {
            // Переходим в UI-поток Авалонии для безопасного изменения размеров
            Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var window = desktop.MainWindow;
                    if (window == null) return;

                    // Меняем флаг состояния
                    _isWidescreen = !_isWidescreen;

                    if (_isWidescreen)
                    {
                        // 🚀 РАСШИРЯЕМ: Сдвигаем позицию X влево на 50 и увеличиваем общую ширину на 100
                        window.Position = window.Position.WithX(window.Position.X - 50);
                        window.Width += DeltaWidth;

                        // Дополнительно можно активировать стили или псевдоклассы для XAML-анимаций
                        window.Classes.Add("widescreen-active");
                    }
                    else
                    {
                        // 🧼 СХЛОПЫВАЕМ ОБРАТНО: Возвращаем X на 50 вправо и уменьшаем ширину на 100
                        window.Position = window.Position.WithX(window.Position.X + 50);
                        window.Width -= DeltaWidth;

                        window.Classes.Remove("widescreen-active");
                    }
                }
            });
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IWidescreenService.ToggleWidescreen>(OnToggleWidescreenRequested);
        }
    }
}
