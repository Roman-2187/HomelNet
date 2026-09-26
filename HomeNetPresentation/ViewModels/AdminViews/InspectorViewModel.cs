using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;
using System;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class InspectorViewModel : FormViewModelBase, IDisposable
    {
        private readonly IEventInspectorSource _inspector;

        [ObservableProperty]
        private string _reportText = "🧠 Граф системы пуст или не инициализирован...";

        public InspectorViewModel(IEventBus eventBus, IEventInspectorSource inspector, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));

            // 🔥 ИДЕАЛЬНО ОДИНАКОВО: Сидим на шине событий и ждем команду на апдейт
            EventBus.Subscribe<IAdminMenuViewModel.ReportGenerationRequested>(OnReportGenerationRequested);

            // Первичный сбор при старте приложения
            UpdateReport();
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private void OnReportGenerationRequested(IAdminMenuViewModel.ReportGenerationRequested msg)
        {
            UpdateReport();
        }

        #endregion

        public void UpdateReport()
        {
            // Бьем напрямую в синглтон-сервис за свежим графом
            ReportText = _inspector.GenerateReport();
        }

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public void Dispose()
        {
            EventBus.Unsubscribe<IAdminMenuViewModel.ReportGenerationRequested>(OnReportGenerationRequested);
        }

        #endregion
    }
}
