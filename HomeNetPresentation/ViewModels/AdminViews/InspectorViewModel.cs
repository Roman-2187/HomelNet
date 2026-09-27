using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    /// <summary>
    /// Вьюмодель инспектора шины событий SiberNet.
    /// Автоматически генерирует интерактивный граф подписок по факту открытия вкладки.
    /// </summary>
    public partial class InspectorViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IEventInspectorSource _inspector;

        [ObservableProperty]
        private string _reportText = "🧠 Граф системы пуст или не инициализирован...";

        public InspectorViewModel(IEventBus eventBus, IEventInspectorSource inspector, AdminNavigationManager navigation)
            : base(eventBus, navigation) // Передали строго типизированный навигатор в generic-базу
        {
            _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));

            // 🔥 ПОДПИСКА ПО ФАКТАМ: Сидим на шине событий и ждем изменения вкладок навигатора
            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);

            // Первичный сбор при инициализации
            UpdateReport();
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            if (msg == null) return;

            // 🔥 ЕСЛИ НАВИГАТОР ПОДТВЕРДИЛ: Открыта вкладка инспектора — моментально перестраиваем граф!
            if (msg.ActiveTab == AdminSubTab.EventInspector)
            {
                UpdateReport();
            }
        }

        #endregion

        public void UpdateReport()
        {
            // Бьем напрямую в синглтон-сервис за свежим графом именованных методов
            ReportText = _inspector.GenerateReport();
        }

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            base.Dispose(); // Чистим базовый слой
            _eventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        }

        #endregion
    }
}
