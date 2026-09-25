using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;
using System;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    // 🔥 Добавили реализацию IDisposable для безопасного снятия подписок
    public partial class InspectorViewModel : FormViewModelBase, IDisposable
    {
        private readonly IEventInspectorSource _inspector;

        [ObservableProperty]
        private string _reportText = "🧠 Граф системы пуст или не инициализирован...";

        public InspectorViewModel(IEventBus eventBus, IEventInspectorSource inspector, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));

            // 🔥 ЧИСТОТА: Передаем имя метода класса вместо анонимной лямбды! 🧼
            EventBus.Subscribe<IAdminMenuViewModel.ReportGenerationRequested>(OnReportGenerationRequested);

            // Первичный сбор при создании
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
            // Прямой return от сервиса бэкенда! Сложность O(1)
            ReportText = _inspector.GenerateReport();
        }

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        /// <summary>
        /// Полностью выписывает инспектор из шины событий при скрытии или закрытии вкладки.
        /// </summary>
        public void Dispose()
        {
            EventBus.Unsubscribe<IAdminMenuViewModel.ReportGenerationRequested>(OnReportGenerationRequested);
        }

        #endregion
    }
}