using System.Collections.ObjectModel;
using System.Windows;
using HomeNetCore.Models;
using HomeNetCore.Services.ListUsersServise;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public partial class UsersTableViewModel : FormViewModelBase
    {
        private readonly ListUsersService _listUsersService;

        public ObservableCollection<UserEntity> Users => _listUsersService.Users;

        public UsersTableViewModel(EventBus eventBus, ListUsersService listUsersService) : base(eventBus)
        {
            _listUsersService = listUsersService ?? throw new ArgumentNullException(nameof(listUsersService));

            InitializeBusSubscriptions();

            // САМА СЕБЯ КОРМИТ: Стартовый запуск с красивой задержкой
            Task.Run(async () =>
            {
                _eventBus.Publish(new StatusTextChangedMessage("Синхронизация с базой данных HomeNet..."));
                await Task.Delay(1000);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _listUsersService.RefreshUsersAsync().GetAwaiter().GetResult();
                    OnPropertyChanged(nameof(Users));

                    // Сразу кормим статус-бар при первой загрузке базы
                    _eventBus.Publish(new UsersListRefreshedMessage(Users));
                });
            });
        }

        private void InitializeBusSubscriptions()
        {
            // 1. Ловим сообщение об удалении пользователя
            _eventBus.Subscribe<UserDeletedMessage>(msg =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var userToRemove = Users.FirstOrDefault(u => u.Id == msg.UserId);
                    if (userToRemove != null)
                    {
                        string deletedName = $"{userToRemove.FirstName} {userToRemove.LastName}";
                        Users.Remove(userToRemove);

                        // Стреляем точной коллекцией сразу после удаления! 🚀
                        _eventBus.Publish(new UsersListRefreshedMessage(Users));

                        Task.Run(async () =>
                        {
                            await Task.Delay(1000);
                            _eventBus.Publish(new StatusTextChangedMessage($"Пользователь {deletedName} успешно удалён"));
                        });
                    }
                });
            });

            // 2. Ловим добавление нового пользователя
            _eventBus.Subscribe<UserAddedMessage>(msg =>
            {
                if (msg.User == null) return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Add(msg.User);
                    _eventBus.Publish(new UsersListRefreshedMessage(Users));
                });
            });

            // 3. ОТВЕТ НА ЗАПРОС: Статус-бар попросил обновить экран? На, держи! 🛸🧼
            _eventBus.Subscribe<RequestStatusRefreshMessage>(msg =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _eventBus.Publish(new UsersListRefreshedMessage(Users));
                });
            });
        }
    }
}

