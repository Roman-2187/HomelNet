using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WpfHomeNet
{
    public partial class DeleteUserDialog : Window
    {
        private readonly ObservableCollection<UserEntity> _users;
        private readonly UserService _userService;
        private readonly ILogger _logger;
        private UserEntity? _selectedUser; 
        public Action<string, Brush>? OnStatusUpdated;

        public DeleteUserDialog(
            ObservableCollection<UserEntity> users,
            UserService userService,
            ILogger logger)
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
                MessageBox.Show("Введите корректный ID");
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
                        container.Tag = "Found";
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
                            container.Tag = "Found";
                            container.Focus();
                        }
                        else
                        {
                            Dispatcher?.BeginInvoke(new Action(() =>
                            {
                                container = userListBox.ItemContainerGenerator.ContainerFromItem(_selectedUser) as ListBoxItem;
                                if (container != null)
                                {
                                    container.Tag = "Found";
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
                    MessageBox.Show("Пользователь не найден");
                    // Блокируем кнопку — пользователя нет
                    yesButton.IsEnabled = false;
                }

                userIdTextBox.Text = "Введите ID";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
                // В случае ошибки — блокируем кнопку
                yesButton.IsEnabled = false;
            }
        }




        private void userListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Если выделение изменилось НЕ из-за поиска по ID — блокируем кнопку
            if (userListBox.SelectedItem != _selectedUser)
            {
                yesButton.IsEnabled = false;
            }
            // Иначе (если SelectedItem == _selectedUser) — оставляем кнопку активной
            // (это могло произойти из-за прокрутки/виртуализации, но пользователь тот же)
        }






        private void userIdTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Если текст — это подсказка, очищаем поле и делаем текст чёрным
            if (userIdTextBox.Text == "Введите ID")
            {
                userIdTextBox.Text = "";
                userIdTextBox.Foreground = Brushes.Black;
            }
        }

        private void userIdTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Если поле пустое, возвращаем подсказку и серый цвет
            if (string.IsNullOrWhiteSpace(userIdTextBox.Text))
            {
                userIdTextBox.Text = "Введите ID";
                userIdTextBox.Foreground = Brushes.DarkSlateGray;
            }
        }


        // Метод для сброса выделений
        private void ClearHighlights()
        {
            foreach (var item in userListBox.Items)
            {
                ListBoxItem? container = userListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (container != null)
                {
                    container.Tag = null; // Снимаем метку
                }
            }
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

                // 1. Удаляем из БД
                await _userService.DeleteUserAsync(selectedUser.Id);

                // 2. Удаляем из локальной коллекции (это обновит UI!)
                _users.Remove(selectedUser);

                _logger.LogInformation($"Пользователь {selectedUser.FirstName} успешно удалён");

                // 3. Сбрасываем выделение
                userListBox.SelectedItem = null;
                yesButton.IsEnabled = false;

                // 4. Показываем статус
                OnStatusUpdated?.Invoke($"Пользователь с ID {selectedUser.Id} удалён", Brushes.Green);

                userIdTextBox.Text = "Введите ID";


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
                OnStatusUpdated?.Invoke($"Ошибка: {ex.Message}", Brushes.Red);
            }
        }




      




        private void NoButton_Click(object sender, RoutedEventArgs e) => Close();
       

      




    }



}
