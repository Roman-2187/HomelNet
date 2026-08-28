using HomeNetCore.Models;
using System.Collections.ObjectModel;


namespace HomeNetCore.Services.ListUsersServise
{
    public class ListUsersService
    {
        private readonly UserService _userService;

        // Глобальный список, доступный всем модулям приложения
        public ObservableCollection<UserEntity> Users { get; private set; } = new();

        public ListUsersService(UserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        // Централизованная загрузка, которую раньше делал MainViewModel
        public async Task RefreshUsersAsync()
        {
            var list = await _userService.GetAllUsersAsync();

           
            Users.Clear();
            foreach (var user in list)
            {
                Users.Add(user);
            }
        }

    }
}
