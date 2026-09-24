using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class SeedUsersViewModel : FormViewModelBase
    {
        private readonly IUserService _userService;

        [ObservableProperty]
        private string _seedStatusText = "🔋 Система генерации готова к сидингу 10 пользователей...";

        [ObservableProperty]
        private bool _isSeedInProgress;

        public SeedUsersViewModel(IEventBus eventBus, IUserService userService, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

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
                        EventBus.Publish(this, new IUsersTableViewModel.Added(user));
                        addedCount++;
                    }
                }

                SeedStatusText = addedCount > 0
                    ? $"⚔️ Дисциплина наведена! База успешно заселена. Добавлено: {addedCount} юзеров."
                    : "🔒 Сидинг отклонён: Все тестовые пользователи уже находятся в SQLite.";

                EventBus.Publish(this, new IStatusBarViewModel.TextChanged(SeedStatusText));
            }
            catch (Exception ex)
            {
                SeedStatusText = $"❌ Критический сбой сидинга: {ex.Message}";
                EventBus.Publish(this, new IStatusBarViewModel.TextChanged(SeedStatusText));
            }
            finally
            {
                IsSeedInProgress = false;
            }
        }
    }
}
