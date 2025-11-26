using HomeNetCore.Helpers;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Windows;
using System.Windows.Controls;

namespace WpfHomeNet
{
    public partial class DeleteUserWindow : Window
    {
        private readonly UserService? _userService;
        private List<UserEntity>? _allUsers; // Храним все пользователи
        private UserEntity? _selectedUser;
        ILogger _logger;
        public DeleteUserWindow(UserService userService,ILogger logger)
        {
            InitializeComponent();
            _userService = userService;
            _logger = logger;
            LoadAllUsers();
        }

        private async void LoadAllUsers()
        {
            if (_userService is null )
            {
                throw new InvalidOperationException(
                            $"Не инициализированы зависимости: " +
                            $"_userService: {_userService}");
            }
            try
            {
                _allUsers = await _userService.GetAllUsersAsync();
                userListBox.ItemsSource = _allUsers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
                Close();
            }
        }

        private async void SearchUser_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(userIdTextBox.Text, out int userId))
            {
                MessageBox.Show("Введите корректный ID");
                return;
            }

            try
            {
                if (_userService is null || _allUsers is null)
                {
                    throw new InvalidOperationException(
                                $"Не инициализированы зависимости: " +
                                $"_userService: {_userService}, _allUsers: {_allUsers}, ");
                }
                // Ищем пользователя в локальной коллекции
                _selectedUser = _allUsers.FirstOrDefault(u => u.Id == userId);

                if (_selectedUser != null)
                {
                    // Устанавливаем выбранный элемент
                    userListBox.SelectedItem = _selectedUser;
                }
                else
                {
                    MessageBox.Show("Пользователь не найден");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }


        private void userListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            yesButton.IsEnabled = userListBox.SelectedItem != null;
        }

        private async void YesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedUser = userListBox.SelectedItem as UserEntity;
                if (selectedUser == null)
                {
                    MessageBox.Show("Выберите пользователя для удаления");
                    return;
                }

                if (_userService is null )
                {
                    throw new InvalidOperationException(
                                $"Не инициализированы зависимости: " +
                                $"_userService: {_userService}");
                }

                await _userService.DeleteUserAsync(selectedUser.Id);

                _logger.LogInformation($"Пользователь {selectedUser.FirstName} | успешно удален");

                MessageBox.Show("Пользователь успешно удален");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }



}
