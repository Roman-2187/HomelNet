using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Services.UsersServices;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfHomeNet.SubWindows;

namespace WpfHomeNet
{
    public partial class DeleteUserDialog : Window
    {
        private const string DefaultInput = "Введите ID";
        private const string Found = "Found";
        private readonly ObservableCollection<UserEntity> _users;
        private readonly UserService _userService;
        private readonly ILogger _logger;
        private UserEntity? _selectedUser; 
        public Action<string>? OnStatusUpdated;

        public DeleteUserDialog(ObservableCollection<UserEntity> users, UserService userService, ILogger logger)
                                                                                     
        {
            InitializeComponent();
            _users = users;
            _userService = userService;
            _logger = logger;

            // Привязываем ListBox к общей коллекции
            userListBox.ItemsSource = _users;
        }

       

        private async void SearchUser_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(userIdTextBox.Text, out int userId))
            {
                MessageBox.Show(DefaultInput);
                return;
            }

            try
            {
                // 1. Сбрасываем всё: снимаем выделение, очищаем подсветку
                ClearHighlights();
                userListBox.SelectedItem = null;
                _selectedUser = null; // Сбрасываем найденного пользователя

                // 2. Ищем в коллекции
                _selectedUser = _users.FirstOrDefault(u => u.Id == userId);

                if (_selectedUser != null)
                {
                    // 3. Автоматически выделяем в ListBox
                    userListBox.SelectedItem = _selectedUser;

                    // 4. Находим контейнер для подсветки
                    ListBoxItem? container = userListBox.ItemContainerGenerator.ContainerFromItem(_selectedUser) as ListBoxItem;
                    if (container != null)
                    {
                        container.Tag = Found;
                        container.Focus();
                    }
                    else
                    {
                        // Если контейнер не создан (виртуализация)
                        userListBox.ScrollIntoView(_selectedUser);
                        await Task.Delay(100);
                        container = userListBox.ItemContainerGenerator.ContainerFromItem(_selectedUser) as ListBoxItem;
                        if (container != null)
                        {
                            container.Tag = Found;
                            container.Focus();
                        }
                        else
                        {
                            Dispatcher?.BeginInvoke(new Action(() =>
                            {
                                container = userListBox.ItemContainerGenerator.ContainerFromItem(_selectedUser) as ListBoxItem;
                                if (container != null)
                                {
                                    container.Tag = Found;
                                    container.Focus();
                                }
                            }));
                        }
                    }

                    // 5. АКТИВИРУЕМ кнопку — только потому что нашли по ID
                    yesButton.IsEnabled = true;
                }
                else
                {
                    

                    var messageBox = new MessageWindow
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    messageBox.Show();
                    messageBox.MessageText.Text = "Пользователь не найден";
                    // Блокируем кнопку — пользователя нет
                    yesButton.IsEnabled = false;
                }

                userIdTextBox.Text = DefaultInput;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
                // В случае ошибки — блокируем кнопку
                yesButton.IsEnabled = false;
            }
        }


        private void UserListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Если выделение изменилось НЕ из-за поиска по ID — блокируем кнопку
            if (userListBox.SelectedItem != _selectedUser)
            {
                yesButton.IsEnabled = false;
            }
        }


        private void UserIdTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (userIdTextBox.Text == DefaultInput)
            {
                userIdTextBox.Text = "";
                userIdTextBox.Foreground = Brushes.Black; // Явно задаём цвет
            }
        }


       private void UserIdTextBox_LostFocus(object sender, RoutedEventArgs e)
       {
            if (string.IsNullOrWhiteSpace(userIdTextBox.Text))
            {
                userIdTextBox.Text = DefaultInput;        
            }    
       }

      
        private async void YesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (userListBox.SelectedItem is not UserEntity selectedUser)
                {
                   

                    var messageBox = new MessageWindow
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    messageBox.Show();
                    messageBox.MessageText.Text ="Выберите пользователя для удаления" ;
                    return;
                }

                ArgumentNullException.ThrowIfNull(selectedUser.FirstName);
                // 1. Удаляем из БД
                await _userService.DeleteUserAsync(selectedUser.Id, selectedUser.FirstName);

                // 2. Удаляем из локальной коллекции (это обновит UI!)
                _users.Remove(selectedUser);

                
                // 3. Сбрасываем выделение
                userListBox.SelectedItem = null;
                yesButton.IsEnabled = false;

                // 4. Показываем статус
                OnStatusUpdated?.Invoke($"Пользователь {selectedUser.FirstName} с ID {selectedUser.Id} удалён");

                await Task.Delay(2500);

                OnStatusUpdated?.Invoke("Обновление...");
                await Task.Delay(1000);

                OnStatusUpdated?.Invoke("Список обновлён");
                await Task.Delay(1500);
                OnStatusUpdated?.Invoke($"Загружено {_users.Count} пользователей");

                _logger.LogInformation($"Загружено {_users.Count} пользователей");

               
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
                OnStatusUpdated?.Invoke($"Ошибка: {ex.Message}");
            }
        }


        private void UserIdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Если поле содержит подсказку — не ищем
                if (userIdTextBox.Text == DefaultInput)
                    return;

                SearchUser_Click(sender, e);
            }
        }


        private void NoButton_Click(object sender, RoutedEventArgs e) => Close();


        private void ClearHighlights()
        {
            foreach (var item in userListBox.Items)
            {
                if (userListBox.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem container)
                {
                    container.Tag = null; // Снимаем метку
                }
            }
        }
       
    }

}
