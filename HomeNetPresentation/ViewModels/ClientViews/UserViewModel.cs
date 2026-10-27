using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Тотально изолированная и чистая панель пользователя SiberNet.
    /// Больше не содержит внутри себя ссылки на другие вью-модели! 🧼
    /// </summary>
    public partial class UserViewModel : FormViewModelBase<UserNavigationManager>, IDisposable
    {
        private Guid _currentUserId;
        ILogger _logger;

        [ObservableProperty] private ClientSubTab _activeClientTab = ClientSubTab.None;

        public UserViewModel(ILogger logger, IEventBus eventBus, UserNavigationManager navigation)
            : base(eventBus, navigation)
        {
            _logger = logger;
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {      
            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            _eventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSignedUp);
            _eventBus.Subscribe<IUserVm.ClientTabChanged>(OnClientTabChanged);       
        }


        // Добавь CommunityToolkit.Mvvm.Input в юзинги
        [RelayCommand]
        private void LogoutDevice()
        {
            _logger.LogInfo("[UserVM] Пользователь инициировал глобальный выход из системы.");

            // Просто стреляем в шину. Мы никого не ждем, вьюмодель пользователя чиста!
            _eventBus.Publish(this, new IUserVm.LogoutClient());
        }


        private void OnClientTabChanged(IUserVm.ClientTabChanged msg)
        {
            if (msg == null)
            {
                _logger.LogError("[UserVM] OnClientTabChanged вызван, но сообщение msg == NULL!");
                return;
            }

            _logger.LogInfo($"[UserVM -> OnClientTabChanged] Автобус доставил стейт: {msg.ActiveTab}. Было в модели: {ActiveClientTab}");
            ActiveClientTab = msg.ActiveTab;
            OnPropertyChanged(nameof(ActiveClientTab));
        }

        private void OnUserSignedIn(IUserVm.UserSignedIn msg)
        {
            if (msg?.User == null) return;
            _currentUserId = msg.User.Id;
        }

        private void OnUserSignedUp(IUserVm.UserSignedUp msg)
        {
            if (msg?.User == null) return;
            _currentUserId = msg.User.Id;
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserSignedIn);
            _eventBus.Unsubscribe<IUserVm.UserSignedUp>(OnUserSignedUp);
            _eventBus.Unsubscribe<IUserVm.ClientTabChanged>(OnClientTabChanged);
        }
    }
}
