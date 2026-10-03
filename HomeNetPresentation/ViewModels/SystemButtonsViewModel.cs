using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Изолированная вьюмодель для системных кнопок окна (Свернуть, Развернуть, Закрыть).
    /// </summary>
    public partial class SystemButtonsViewModel : ObservableObject
    {
        private readonly IEventBus _eventBus;

        public SystemButtonsViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        // ✕ КНОПКА: Закрыть приложение
        [RelayCommand]
        public void RequestCloseApplication()
        {
            _eventBus.Publish(this, new IMainViewModel.CloseRequest());
        }

        // 🔳 КНОПКА: Развернуть окно
        [RelayCommand]
        public void ToggleGrowWindow()
        {
            _eventBus.Publish(this, new IMainViewModel.ToggleSize());
        }

        // ➖ КНОПКА: Свернуть окно (Пока без логики на будущее)
        [RelayCommand]
        public void MinimizeWindow()
        {
            // Здесь будет логика сворачивания, когда допишешь эвент на бэке
        }
    }
}
