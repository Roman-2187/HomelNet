using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    /// <summary>
    /// Вьюмодель модуля автоматического сидинга базы данных SiberNet.
    /// Наследуется от дженерик-базы с указанием конкретного менеджера навигации админки.
    /// </summary>
    public partial class SeedUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IUserService _userService;

        [ObservableProperty]
        private string _seedStatusText = "🔋 Система генерации готова к сидингу 50 пользователей...";

        [ObservableProperty]
        private bool _isSeedInProgress;

        public SeedUsersViewModel(IEventBus eventBus, IUserService userService, AdminNavigationManager navigation)
            : base(eventBus, navigation) // Передали строго типизированный навигатор в generic-базу
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            // 🔥 ПОДПИСКА ПО ФАКТАМ: Ловим изменение вкладок навигатора для сброса стейта формы
            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            if (msg == null) return;

            // 🔥 Если навигатор подтвердил повторное открытие вкладки сидинга — сбрасываем текст в дефолт
            if (msg.ActiveTab == AdminSubTab.SeedUsers)
            {
                SeedStatusText = "🔋 Система генерации готова к сидингу 50 пользователей...";
            }
        }

        #endregion

        [RelayCommand]
        private async Task ExecuteDataSeedingAsync()
        {
            if (IsSeedInProgress) return;

            IsSeedInProgress = true;
            SeedStatusText = "⚡ Запуск сидинга... Инициализация транзакций...";
            int addedCount = 0;

            try
            {
                // Вытягиваем наши заготовленные 50 тестовых аккаунтов
                var testUsers = DbSeedData.GetGeneratedUsers();

                foreach (var user in testUsers)
                {
                    bool emailExists = await _userService.CheckEmailExistsAsync(user.Email);
                    if (!emailExists)
                    {
                        await _userService.AddUserSecureAsync(user);

                        // Публикуем в автобус, чтобы живая таблица сразу дорисовывала юзера на лету!
                        _eventBus.Publish(this, new IUsersTableVm.Added(user));
                        addedCount++;
                    }
                }

                SeedStatusText = addedCount > 0
                    ? $"⚔️ Дисциплина наведена! База успешно заселена. Добавлено: {addedCount} юзеров."
                    : "🔒 Сидинг отклонён: Все тестовые пользователи уже находятся в SQLite.";

                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged(SeedStatusText));
            }
            catch (Exception ex)
            {
                SeedStatusText = $"❌ Критический сбой сидинга: {ex.Message}";
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged(SeedStatusText));
            }
            finally
            {
                IsSeedInProgress = false;
            }
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
