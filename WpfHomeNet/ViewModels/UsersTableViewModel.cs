using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Collections.ObjectModel;
using System.Windows;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public partial class UsersTableViewModel : FormViewModelBase
    {
        
        private UserService _userService;

        public ObservableCollection<UserEntity> Users { get; private set; } = new();

        public UsersTableViewModel(EventBus eventBus, UserService userService) : base(eventBus)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            InitializeBusSubscriptions();

            Task.Run(async () =>
            {
                _eventBus.Publish(new StatusTextChangedMessage("Синхронизация с базой данных HomeNet..."));

                // 1. Честно читаем базу данных напрямую в фоновом потоке
                var list = await _userService.GetAllUsersAsync();

                await Task.Delay(1000); // Наша кинематографичная пауза 🎬

                // 2. Возвращаемся в UI-поток и безопасно забиваем коллекцию
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Users.Clear();
                    foreach (var user in list)
                    {
                        Users.Add(user);
                    }

                    // Кормим статус-бар финальными циферками
                    _eventBus.Publish(new UsersListRefreshedMessage(Users));
                    _eventBus.Publish(new StatusTextChangedMessage("База данных успешно синхронизирована."));
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
            _eventBus.Subscribe<UserRegisteredMessage>(msg =>
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

