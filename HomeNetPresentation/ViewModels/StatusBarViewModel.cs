using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.Network;
using HomeNetCore.Interfaces.ViewModels.AdminZone;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    public partial class StatusBarViewModel : FormViewModelBase<BaseNavigationState>, IDisposable
    {
        [ObservableProperty]
        private string _statusText = "Готов к работе";

        [ObservableProperty]
        private string _combinedStatusText = "OFFLINE";

        // 🔥 СЕНЬОРСКОЕ СВОЙСТВО ДЛЯ ДИНАМИЧЕСКОГО ЦВЕТА ИНДИКАТОРА
        [ObservableProperty]
        private string _statusColor = "#FFF5546F"; // Стартуем с тревожного розово-красного

        public StatusBarViewModel(IEventBus eventBus, BaseNavigationState navigation)
            : base(eventBus, navigation)
        {
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<IStatusBarViewModel.TextChanged>(OnTextChanged);
            _eventBus.Subscribe<IUsersTableVm.Refreshed>(OnUsersTableRefreshed);
            _eventBus.Subscribe<NetworkStatusChanged>(OnNetworkStatusChanged);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК

        private async void OnTextChanged(IStatusBarViewModel.TextChanged msg)
        {
            if (msg == null) return;
            await UpdateStatusTextAsync(msg.NewStatus);
        }

        private void OnUsersTableRefreshed(IUsersTableVm.Refreshed msg)
        {
            if (msg == null) return;

            StatusText = msg.Users != null && msg.Users.Count > 0
                ? $"База данных синхронизирована. Загружено пользователей: {msg.Users.Count}"
                : "Синхронизация завершена. Список пользователей пуст.";
        }

        // 🔥 ЖЕСТКОЕ УПРАВЛЕНИЕ ЦВЕТОМ И ТЕКСТОМ ИЗ ВЬЮМОДЕЛИ
        private void OnNetworkStatusChanged(NetworkStatusChanged msg)
        {
            if (msg == null) return;

            if (msg.IsOnline)
            {
                CombinedStatusText = "API_OK | DB_OK";
                StatusColor = "#FF4EC9B0"; // Вспыхивает сочный изумрудный зеленый!
            }
            else
            {
                CombinedStatusText = "OFFLINE";
                StatusColor = "#FFF5546F"; // Падает в розово-красный офлайн
            }
        }

        #endregion

        private Task UpdateStatusTextAsync(string newText)
        {
            StatusText = newText;
            return Task.CompletedTask;
        }

        #region 🛡️ ПУЛЕНЕПРОБИВАЕМЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IStatusBarViewModel.TextChanged>(OnTextChanged);
            _eventBus.Unsubscribe<IUsersTableVm.Refreshed>(OnUsersTableRefreshed);
            _eventBus.Unsubscribe<NetworkStatusChanged>(OnNetworkStatusChanged);
        }

        #endregion
    }
}
