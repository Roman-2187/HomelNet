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
    /// Перехватывает события авторизации, управляет автовходом и очисткой сессии! 🧼
    /// </summary>
    public partial class DeviceSettingsViewModel : FormViewModelBase<UserNavigationManager>, IDisposable
    {
        private readonly IDeviceSettingsService _deviceSettingsService;
        private readonly ILogger _logger;
        private readonly IUserService _userService;

        [ObservableProperty] private bool _isRememberMeChecked = true;
        [ObservableProperty] private string _deviceIdDisplay = "Загрузка...";
        [ObservableProperty] private string _currentSessionEmail = "Гостевой режим";

        public DeviceSettingsViewModel(
            IDeviceSettingsService deviceSettingsService,
            IUserService userService,
            IEventBus eventBus,
            ILogger logger,
            UserNavigationManager navigation) : base(eventBus, navigation)
        {
            _deviceSettingsService = deviceSettingsService;
            _userService = userService;
            _logger = logger;

            // Подписываемся на успех входа/регистрации
            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnUserSessionActivated);
            _eventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSessionActivated);

            // 🔥 Обязательно слушаем разлогин, чтобы обнулить данные сессии
            _eventBus.Subscribe<IUserVm.LogoutClient>(OnLogoutRequested);
            // Оставляешь старые подписки как есть
            _eventBus.Subscribe<IUserVm.LogoutClient>(OnLogoutRequested);

            // 🔥 Для админа подписываешься через лямбду, передавая null вместо клиентского сообщения
            _eventBus.Subscribe<IAdminVm.LogoutAdmin>(_ => OnLogoutRequested(null!));


            // Инициализация железа
            Task.Run(LoadHardwareIdAsync);

            // 🔥 При старте ВьюМодели проверяем: если есть сохраненные данные — сразу ломимся в сервис
            Task.Run(TryAutoLoginAsync);
        }

        private async Task LoadHardwareIdAsync()
        {
            Guid deviceId = await _deviceSettingsService.GetDeviceIdAsync();
            DeviceIdDisplay = deviceId.ToString().ToUpper();
        }

        /// <summary>
        /// 🔥 ПРЯМОЙ АВТОВХОД: Если данные есть в SQLite настроек, генерируем событие входа напрямую
        /// </summary>
        /// <summary>
        /// 🔥 ПРЯМОЙ АВТОВХОД: Читаем DeviceSettingsEntity и генерируем событие входа напрямую
        /// </summary>
        private async Task TryAutoLoginAsync()
        {
            try
            {
                _logger.LogInfo("[AutoLogin] Проверяем наличие сохраненной сессии на устройстве...");

                // 🔥 1. Вызываем твой метод из сервиса настроек
                DeviceSettingsEntity? settings = await _deviceSettingsService.GetCurrentSettingsAsync();

                // Проверяем, есть ли там данные пользователя (не пустые ли они после ClearSession)
                if (settings == null ||
                    settings.UserId == Guid.Empty ||
                    string.IsNullOrEmpty(settings.Email) ||
                    string.IsNullOrEmpty(settings.Password))
                {
                    _logger.LogInfo("[AutoLogin] Чистый запуск. Сохраненной сессии пользователя нет.");
                    return;
                }

                _logger.LogInfo($"[AutoLogin] Найдена сессия для {settings.Email}. Напрямую запрашиваем User из локального SQLite...");

                // 2. Идем напрямую в сервис пользователей локального SQLite
                UserEntity? localUser = await _userService.GetByEmailAsync(settings.Email);

                // 3. Сверяем пароль для безопасности
                if (localUser != null && localUser.Password == settings.Password)
                {
                    _logger.LogInfo("[AutoLogin] Сессия валидна. Генерируем событие входа для всей экосистемы SiberNet.");

                    // 🔥 Просто пушим событие входа! Навигатор переключит на Messenger, а UserVM запишет ID.
                    _eventBus.Publish(this, new IUserVm.UserSignedIn(localUser));
                }
                else
                {
                    _logger.LogWarning("[AutoLogin] Данные сессии устарели или пользователь не найден.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AutoLogin] Сбой авто-авторизации: {ex.Message}");
            }
        }


        /// <summary>
        /// Ловит успешный вход/регистрацию (в том числе из TryAutoLoginAsync) и делает разовую привязку сессии.
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
                // Если мы уже обработали этого пользователя в текущей сессии, можно пропустить повторную запись,
                // но стандартная проверка на изменение DeviceId или флага IsSynced не повредит.
                if (CurrentSessionEmail == user.Email) return;

                CurrentSessionEmail = user.Email ?? "Авторизован";

                Guid appDeviceId = await _deviceSettingsService.GetDeviceIdAsync();
                _logger.LogInfo($"[Сессия -> VM] Связываем пользователя {user.Email} с девайсом {appDeviceId}");

                user.DeviceId = appDeviceId;
                user.IsSynced = 0; // Строго 0 для SiberNet Engine PUSH!

                // Обновляем сущность юзера в локальной БД
                await _userService.UpdateUserAsync(user);

                // Если галочка стоит — сохраняем сессию в настройки устройства для последующих автовходов
                if (IsRememberMeChecked)
                {
                    await _deviceSettingsService.SaveSessionAsync(user.Id, user.Email!, user.Password!);
                }

                _logger.LogInfo("[Сессия -> VM] Локальный SQLite обновлен. Синхронизатор подхватит апдейт автоматически.");



               
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Сессия -> VM] Ошибка при обработке активации сессии: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 Перехватчик ЛОГАУТА: Обнуляет записанные данные в текущей сессии
        /// </summary>
        private async void OnLogoutRequested(IUserVm.LogoutClient msg)
        {
            try
            {
                _logger.LogInfo("[Сессия -> VM] Поймали LogoutClient. Выжигаем сохраненную сессию из настроек.");

                // Стираем данные из таблицы настроек устройства
                await _deviceSettingsService.ClearSessionAsync();

                CurrentSessionEmail = "Гостевой режим";
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Сессия -> VM] Ошибка при очистке сессии при разлогине: {ex.Message}");
            }
        }


        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserSessionActivated);
            _eventBus.Unsubscribe<IUserVm.UserSignedUp>(OnUserSessionActivated);
            _eventBus.Unsubscribe<IUserVm.LogoutClient>(OnLogoutRequested);
        }
    }
}
