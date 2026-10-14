using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;

namespace HomeNetPresentation.Services
{
    /// <summary>
    /// Автономный менеджер навигации зоны пользователя мессенджера SiberNet.
    /// Рулит гостевыми экранами, мессенджером и открытием чатов через шину событий. 🦾
    /// </summary>
    public partial class UserNavigationManager : BaseNavigationState
    {
        ILogger _logger { get; set; }
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;

        public UserNavigationManager(IEventBus eventBus, ILogger logger) : base(eventBus)
        {
            _logger = logger;
            EventBus.Subscribe<IUserVm.OpenAuth>(OnOpenAuthRequested);
            EventBus.Subscribe<IUserVm.OpenRegistration>(OnOpenRegistrationRequested);
            EventBus.Subscribe<IUserVm.BackToStart>(OnBackToStartRequested);

            // Подписки на успех авторизации/регистрации
            EventBus.Subscribe<IUserVm.UserSignedIn>(OnUserAuthenticatedRequested);
            EventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSignedUpRequested);

            // 🔥 ЛОВИМ КЛИК ПО ПОЛЬЗОВАТЕЛЮ ИЗ СПИСКА КОНТАКТОВ!
            EventBus.Subscribe<IContactsVm.FriendSelected>(OnFriendSelectedRequested);

            EventBus.Subscribe<IUserVm.LogoutClient>(OnLogoutClientRequested);
        }

        #region 👤 УПРАВЛЕНИЕ ВНУТРЕННИМИ ЭНУМАМИ ХОЛСТА ПОЛЬЗОВАТЕЛЯ

        private void OnOpenAuthRequested(IUserVm.OpenAuth msg)
        {
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Authentication;
            PublishStateDuet();

            _logger.LogInfo($"Зашли в OnOpenAuthRequested текущий стате {CurrentClientTab}");
        }

        private void OnOpenRegistrationRequested(IUserVm.OpenRegistration msg)
        {
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Registration;
            PublishStateDuet();

            _logger.LogInfo($"Зашли в OnOpenRegistrationRequested текущий стате {CurrentClientTab}");
        }

        private void OnBackToStartRequested(IUserVm.BackToStart msg)
        {
            CurrentMainZone = MainTab.StartZone;
            CurrentClientTab = ClientSubTab.None;
            PublishStateDuet();
        }

        private void OnUserAuthenticatedRequested(IUserVm.UserSignedIn msg)
        {
            if (msg?.User == null) return;

            CurrentUser = msg.User;
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
            PublishStateDuet();
        }

        private void OnUserSignedUpRequested(IUserVm.UserSignedUp msg)
        {
            if (msg?.User == null) return;

            CurrentUser = msg.User;
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
            PublishStateDuet();
        }

        /// <summary>
        /// 🔥 РЕАКЦИЯ НА КЛИК: Переключаем энум на ЧАТ, когда выбрали друга
        /// </summary>
        private void OnFriendSelectedRequested(IContactsVm.FriendSelected msg)
        {
            if (msg?.Friend == null) return;

            _logger.LogInfo($"[Navigator] Выбран контакт: {msg.Friend.FirstName}. Переключаем стейт на Chat.");

            // Переводим под-вкладку в режим чата, чтобы XAML скрыл заглушку и открыл ChatViewControl!
            CurrentClientTab = ClientSubTab.Chat;
            PublishStateDuet();
        }

        private void OnLogoutClientRequested(IUserVm.LogoutClient msg)
        {
            CurrentUser = null;
            CurrentMainZone = MainTab.StartZone;
            CurrentClientTab = ClientSubTab.None;
            PublishStateDuet();
        }

        #endregion

        private void PublishStateDuet()
        {
            EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, CurrentClientTab));
            EventBus.Publish(this, new IUserVm.ClientTabChanged(CurrentClientTab));
        }

        public override void Dispose()
        {
            base.Dispose();
            EventBus.Unsubscribe<IUserVm.OpenAuth>(OnOpenAuthRequested);
            EventBus.Unsubscribe<IUserVm.OpenRegistration>(OnOpenRegistrationRequested);
            EventBus.Unsubscribe<IUserVm.BackToStart>(OnBackToStartRequested);
            EventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserAuthenticatedRequested);
            EventBus.Unsubscribe<IUserVm.UserSignedUp>(OnUserSignedUpRequested);

            // 🔥 Не забываем отписаться
            EventBus.Unsubscribe<IContactsVm.FriendSelected>(OnFriendSelectedRequested);

            EventBus.Unsubscribe<IUserVm.LogoutClient>(OnLogoutClientRequested);
        }
    }
}
