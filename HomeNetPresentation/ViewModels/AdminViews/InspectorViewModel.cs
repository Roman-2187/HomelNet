using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class InspectorViewModel : FormViewModelBase
    {
        private readonly IEventInspectorSource _inspector;

        [ObservableProperty]
        private string _reportText = "🧠 Граф системы пуст или не инициализирован...";

        public InspectorViewModel(IEventBus eventBus, IEventInspectorSource inspector, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));

            // 🎧 Ловим сигнал от админской панели по автобусу
            EventBus.Subscribe<IAdminMenuViewModel.ReportGenerationRequested>(msg =>
            {
                UpdateReport();
            });

            // Первичный сбор при создании
            UpdateReport();
        }

        public void UpdateReport()
        {
            // Прямой return от сервиса бэкенда! Сложность O(1)
            ReportText = _inspector.GenerateReport();
        }
    }
}
