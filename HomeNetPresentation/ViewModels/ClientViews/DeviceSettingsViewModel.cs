using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models.Entity;
using HomeNetPresentation.Services;
using HomeNetCore.Extensions;

namespace HomeNetPresentation.ViewModels.ClientViews
{
    /// <summary>
    /// Вьюмодель экрана настроек устройства и управления активной сессией.
    /// Направляет сигналы авторизации в бизнес-сервис конфигурации! 🧼
    /// </summary>
    public partial class DeviceSettingsViewModel : FormViewModelBase<UserNavigationManager>, IDisposable
    {
        private readonly IDeviceSettingsService _deviceSettingsService;
      
        private readonly ILogger _logger;

        [ObservableProperty] private bool _isRememberMeChecked = true;
        [ObservableProperty] private string _deviceIdDisplay = "Загрузка...";
        [ObservableProperty] private string _currentSessionEmail = "Гостевой режим";

       
        private readonly IUserService _userService; // 🔥 Заменили репозиторий на чистый сервис!
       

        public DeviceSettingsViewModel(
            IDeviceSettingsService deviceSettingsService,
            IUserService userService, // 🔥 Здесь теперь сервис
            IEventBus eventBus,
            ILogger logger,
            UserNavigationManager navigation) : base(eventBus, navigation)
        {
            _deviceSettingsService = deviceSettingsService;
            _userService = userService;
            _logger = logger;

            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnUserSessionActivated);
            _eventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSessionActivated);

            Task.Run(LoadHardwareIdAsync);
        }


        private async Task LoadHardwareIdAsync()
        {
            Guid deviceId = await _deviceSettingsService.GetDeviceIdAsync();
            DeviceIdDisplay = deviceId.ToString().ToUpper();
        }

        /// <summary>
        /// 🔥 ГЛАВНЫЙ ПЕРЕХВАТЧИК: Ловит успешный вход и запускает цепочку выжигания нулей!
        /// </summary>
        private async void OnUserSessionActivated(object msg)
        {
            UserEntity? user = msg switch
            {
                IUserVm.UserSignedIn signIn => signIn.User,
                IUserVm.UserSignedUp signUp => signUp.User,
                _ => null
            };

            if (user == null) return;

            try
            {
                CurrentSessionEmail = user.Email ?? "Авторизован";

                // 1. Получаем ID текущего железа из памяти
                Guid appDeviceId = await _deviceSettingsService.GetDeviceIdAsync();

                _logger.LogInfo($"[Сессия -> VM] Связываем пользователя {user.Email} с девайсом {appDeviceId}");

                // 2. Модифицируем сущность: вшиваем ID и сбрасываем флаг синхронизации для ОРМ
                user.DeviceId = appDeviceId;
                user.IsSynced = 0; // Строго 0! Чтобы SiberNet Engine понял: нужен PUSH на сервер!

                // 3. Сохраняем обновленного юзера в локальный SQLite
                await _userService.UpdateUserAsync(user);

                // 4. Если галочка "Запомнить меня" стоит — пишем сессию в DeviceSettings для автовхода
                if (IsRememberMeChecked)
                {
                    await _deviceSettingsService.SaveSessionAsync(user.Id, user.Email!, user.Password!);
                }

                _logger.LogInfo("[Сессия -> VM] Локальный SQLite обновлен. Синхронизатор подхватит апдейт автоматически.");

                // 5. Пинаем координатор фоновой синхронизации, чтобы он утащил чистую сущность в Postgres на сервер!
                _eventBus.Publish(this, new ISiberNetSyncCoordinator.UserPendingSync(user));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Сессия -> VM] Ошибка привязки аккаунта к устройстве: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LogoutDeviceAsync()
        {
            await _deviceSettingsService.ClearSessionAsync();
            CurrentSessionEmail = "Гостевой режим";
            _eventBus.Publish(this, new IUserVm.LogoutClient());
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserSessionActivated);
            _eventBus.Unsubscribe<IUserVm.UserSignedUp>(OnUserSessionActivated);
        }
    }
}
