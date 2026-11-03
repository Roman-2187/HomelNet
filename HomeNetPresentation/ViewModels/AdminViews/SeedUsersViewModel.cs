using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.ViewModels.AdminZone;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetPresentation.Services;
using HomeNetPresentation.TestData;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    /// <summary>
    /// Вьюмодель модуля автоматического сидинга базы данных SiberNet.
    /// Наследуется от дженерик-базы с указанием конкретного менеджера навигации админки.
    /// </summary>
    public partial class SeedUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IUserRepo _userRepository;

        [ObservableProperty]
        private string _seedStatusText = "🔋 Система генерации готова к сидингу 50 пользователей...";

        [ObservableProperty]
        private bool _isSeedInProgress;

        public SeedUsersViewModel(IEventBus eventBus, IUserRepo userRepository, AdminNavigationManager navigation)
            : base(eventBus, navigation)
        {
           _userRepository = userRepository;
            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК 🧼

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            if (msg == null) return;

            if (msg.ActiveTab == AdminSubTab.SeedUsers)
            {
                SeedStatusText = "🔋 Система генерации готова к сидингу  пользователей...";
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
                var testUsers = DbSeedData.GetGeneratedUsers();

                foreach (var user in testUsers)
                {
                    bool emailExists = await _userRepository.EmailExistsAsync(user.Email);
                    if (!emailExists)
                    {
                        await _userRepository.InsertUserAsync(user);

                        
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

        #region 🛡️ СТЕРЕЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        }

        #endregion
    }
}
