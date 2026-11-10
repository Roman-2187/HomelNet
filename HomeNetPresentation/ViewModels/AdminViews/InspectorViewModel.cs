using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Diagnostics.EventInspector;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.ViewModels.AdminZone;
using HomeNetPresentation.Services;
using HomeNetPresentation.ViewModels;
// ... остальные using ...

public partial class InspectorViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
{
    private readonly EventBusInspector _inspector;

    // 🔥 Запоминаем контекст UI-потока при создании ViewModel
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

    [ObservableProperty] private List<IEventInspector.Line> _graphLines = new();
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _loadingMessage = string.Empty;

    public InspectorViewModel(IEventBus eventBus, IEventInspector inspector, AdminNavigationManager navigation)
        : base(eventBus, navigation)
    {
        _inspector = inspector as EventBusInspector ?? throw new ArgumentNullException(nameof(inspector));

        // 🔥 Подписываемся на событие сканирования
        _inspector.ScanCompleted += OnScanCompleted;

        _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        UpdateReport();
    }

    private void OnScanCompleted()
    {
        // 🔥 Возвращаемся в UI-поток и обновляем граф
        if (_uiContext != null)
        {
            _uiContext.Post(_ => UpdateReport(), null);
        }
        else
        {
            // Если контекст не найден (например, в тестах), обновляем напрямую
            UpdateReport();
        }
    }

    private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
    {
        if (msg == null) return;

        if (msg.ActiveTab == AdminSubTab.EventInspector)
        {
            UpdateReport();
        }
    }

    public void UpdateReport()
    {
        // 🔥 Проверяем, идет ли сканирование
        if (_inspector.IsScanning)
        {
            IsScanning = true;
            LoadingMessage = "⌛ Идёт сканирование сборок... Пожалуйста, подождите.";
            GraphLines = new List<IEventInspector.Line>();
            return;
        }

        // Если сканирование завершено, показываем граф
        IsScanning = false;
        LoadingMessage = string.Empty;
        GraphLines = _inspector.GenerateObjectGraph();
    }

    public override void Dispose()
    {
        base.Dispose();
        _inspector.ScanCompleted -= OnScanCompleted; // 🔥 Отписываемся
        _eventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
    }
}