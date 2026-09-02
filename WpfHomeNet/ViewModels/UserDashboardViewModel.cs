using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using HomeNetCore.Models;
using WpfHomeNet.Messaging;


namespace WpfHomeNet.ViewModels
{
    public partial class UserDashboardViewModel : ObservableObject
    {
        private readonly EventBus _eventBus;

        // Текущий авторизованный пользователь
        [ObservableProperty] private UserEntity? _currentUser;

        // Коллекция друзей, которую мы потом свяжем с Postgres/Dapper
        [ObservableProperty] private ObservableCollection<string> _friends = new();

        public UserDashboardViewModel(EventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // 🧼 ЧИСТОТА: Больше никакого Application.Current в подписках!
            _eventBus.Subscribe<UserLoggedMessage>(msg => OnUserAuthenticated(msg.User));
            _eventBus.Subscribe<UserRegisteredMessage>(msg => OnUserAuthenticated(msg.User));
        }

        private void OnUserAuthenticated(UserEntity user)
        {
            if (user == null) return;

            // 🔥 Перенаправляем обновление свойств в UI-поток через WPF диспетчер
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentUser = user;

                // Временные тестовые данные для проверки визуала
                Friends.Clear();
                Friends.Add("● Иван (В сети)");
                Friends.Add("● Мария (В сети)");
                Friends.Add("○ Алексей (Оффлайн)");
            });
        }


       
    }
}
