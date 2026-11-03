using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.ViewModels.MainZone;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Изолированная вьюмодель для системных кнопок окна (Свернуть, Развернуть, Закрыть).
    /// </summary>
    public partial class SystemButtonsViewModel(IEventBus eventBus) : ObservableObject
    {
        private readonly IEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        // ✕ КНОПКА: Закрыть приложение
        [RelayCommand]
        public void RequestCloseApplication()
        {
            _eventBus.Publish(this, new IMainViewModel.CloseRequest());
        }

        // 🔳 КНОПКА: Развернуть/Восстановить окно
        [RelayCommand]
        public void ToggleGrowWindow()
        {
            // 🔥 Публикуем сигнал изменения размеров
            _eventBus.Publish(this, new IMainViewModel.ToggleSize());
        }

        // ➖ КНОПКА: Свернуть окно
        [RelayCommand]
        public void MinimizeWindow()
        {
            // 🔥 Публикуем сигнал сворачивания
            _eventBus.Publish(this, new IMainViewModel.MinimizeRequest());
        }
    }
}
