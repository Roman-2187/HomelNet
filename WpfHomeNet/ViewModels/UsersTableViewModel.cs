using System.Collections.ObjectModel;
using System.Windows;
using HomeNetCore.Models;
using HomeNetCore.Services.ListUsersServise;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    /// <summary>
    /// Автономная ВьюМодель для управления логикой таблицы пользователей.
    /// Перехватывает события шины данных и обновляет коллекцию независимо от главного окна.
    /// </summary>
    public partial class UsersTableViewModel : FormViewModelBase
    {
        private readonly ListUsersService _listUsersService;

        /// <summary>
        /// Глобальный и актуальный список пользователей, привязанный прямо к UsersTableView.xaml.
        /// </summary>
        public ObservableCollection<UserEntity> Users => _listUsersService.Users;

        public UsersTableViewModel(EventBus eventBus, ListUsersService listUsersService) : base(eventBus)
        {
            _listUsersService = listUsersService ?? throw new ArgumentNullException(nameof(listUsersService));

            // Запускаем личные рельсы подписок для таблицы 🚂
            InitializeBusSubscriptions();

            // САМА СЕБЯ КОРМИТ: Таблица при рождении асинхронно пинает сервис базы данных 🚀
            Task.Run(async () =>
            {
                // Отправляем статус загрузки в шину (для нашего статус-бара!)
                _eventBus.Publish(new StatusTextChangedMessage("Синхронизация с базой данных HomeNet..."));

                // Качаем юзеров из SQLite напрямую в сервис
                await _listUsersService.RefreshUsersAsync();

                // Переходим в UI-поток и говорим XAML-таблице: «Обнови пиксели, данные прилетели!» 🧼
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(Users));
                });

                // Оповещаем статус-бар и всех остальных, что список готов и налит!
                _eventBus.Publish(new UsersListRefreshedMessage(_listUsersService.Users));
            });
        }


        /// <summary>
        /// Подписки на события шины, которые касаются ИСКЛЮЧИТЕЛЬНО изменения состава пользователей.
        /// </summary>
        private void InitializeBusSubscriptions()
        {
            // 1. Ловим сообщение об удалении пользователя из базы данных
            _eventBus.Subscribe<UserDeletedMessage>(msg =>
            {
                // Заходим в UI-поток один раз и делаем всё атомарно для потокобезопасности 🔒
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var userToRemove = Users.FirstOrDefault(u => u.Id == msg.UserId);
                    if (userToRemove != null)
                    {
                        Users.Remove(userToRemove);
                    }
                });
            });

            // 2. Ловим сообщение о добавлении нового пользователя
            _eventBus.Subscribe<UserAddedMessage>(msg =>
            {
                if (msg.User == null) return;

                // Безопасно добавляем в UI-потоке, чтобы интерфейс мгновенно отрисовал строку
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Add(msg.User);
                });
            });


            _eventBus.Subscribe<UsersListRefreshedMessage>(msg =>
            {
                // Переходим в UI-поток и пинаем XAML, чтобы он перечитал свойство Users! 🧼
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(Users));
                });
            });
        }
    }
}
