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
    /// Напрямую рулит гостевыми экранами и мессенджером, отправляя полный дуэт стейтов.
    /// </summary>
    public partial class UserNavigationManager : BaseNavigationState
    {
        ILogger _logger { get; set; }
        [ObservableProperty] private ClientSubTab _currentClientTab = ClientSubTab.None;

        public UserNavigationManager(IEventBus eventBus,ILogger logger) : base(eventBus)
        {
            _logger = logger;
            EventBus.Subscribe<IUserVm.OpenAuth>(OnOpenAuthRequested);
            EventBus.Subscribe<IUserVm.OpenRegistration>(OnOpenRegistrationRequested);
            EventBus.Subscribe<IUserVm.BackToStart>(OnBackToStartRequested);

            // 🔥 ПОДПИСЫВАЕМСЯ НА ОБА БРАУЗЕРНЫХ ВЕБ-ИВЕНТА С КНОПОК
            EventBus.Subscribe<IUserVm.UserSignedIn>(OnUserAuthenticatedRequested);
            EventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSignedUpRequested); // Ловим успех регистрации!

            EventBus.Subscribe<IUserVm.LogoutClient>(OnLogoutClientRequested);
        }

        #region 👤 УПРАВЛЕНИЕ ВНУТРЕННИМИ ЭНУМАМИ ХОЛСТА ПОЛЬЗОВАТЕЛЯ

        private void OnOpenAuthRequested(IUserVm.OpenAuth msg)
        {
            // 🚀 СРАЗУ включаем макро-холст пользователя!
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Authentication; // Показываем форму входа
            PublishStateDuet();

            _logger.LogInfo($"Зашли в OnOpenAuthRequested текущий стате {CurrentClientTab}");
        }

        private void OnOpenRegistrationRequested(IUserVm.OpenRegistration msg)
        {
            // 🚀 СРАЗУ включаем макро-холст пользователя!
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Registration; // Показываем форму регистрации
            PublishStateDuet();

            _logger.LogInfo($"Зашли в OnOpenRegistrationRequested текущий стате {CurrentClientTab}");
        }

        private void OnBackToStartRequested(IUserVm.BackToStart msg)
        {
            // А вот если нажали отмену на формах — вылетаем обратно на чистую заставку гостя
            CurrentMainZone = MainTab.StartZone;
            CurrentClientTab = ClientSubTab.None;
            PublishStateDuet();
        }

        private void OnUserAuthenticatedRequested(IUserVm.UserSignedIn msg)
        {
            if (msg?.User == null) return;

            CurrentUser = msg.User;
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger; // 🎯 Просто переключаем внутренний энум на мессенджер!
            PublishStateDuet();
        }

        private void OnUserSignedUpRequested(IUserVm.UserSignedUp msg)
        {
            if (msg?.User == null) return;

            CurrentUser = msg.User;
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger; // 🎯 И здесь просто переключаем на мессенджер!
            PublishStateDuet();
        }

        private void OnLogoutClientRequested(IUserVm.LogoutClient msg)
        {
            CurrentUser = null;
            CurrentMainZone = MainTab.StartZone; // Полный вылет на заставку
            CurrentClientTab = ClientSubTab.None;

            
            PublishStateDuet();
        }

        #endregion



        private void PublishStateDuet()
        {
            // Отправляем сквозной дуэт стейтов, чтобы обновились DataTrigger в XAML
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
            EventBus.Unsubscribe<IUserVm.LogoutClient>(OnLogoutClientRequested);
        }
    }
}
