using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;
using HomeNetServices.Diagnostics;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class InspectorViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly EventBusInspector _inspector;

        // 🔥 Наш зрячий реактивный список строк для ItemsControl
        [ObservableProperty] private List<IEventInspector.Line> _graphLines = new();

        public InspectorViewModel(IEventBus eventBus, IEventInspector inspector, AdminNavigationManager navigation)
            : base(eventBus, navigation)
        {
            // Кастуем интерфейс к нашему реальному профайлеру
            _inspector = inspector as EventBusInspector ?? throw new ArgumentNullException(nameof(inspector));

            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);

            UpdateReport();
        }

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            if (msg == null) return;

            // Как только админ открыл вкладку инспектора — мгновенно перестраиваем граф из оперативки!
            if (msg.ActiveTab == AdminSubTab.EventInspector)
            {
                UpdateReport();
            }
        }

        public void UpdateReport()
        {
            GraphLines = _inspector.GenerateObjectGraph();
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        }
    }
}
