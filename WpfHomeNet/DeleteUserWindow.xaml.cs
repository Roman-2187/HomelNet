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
        private UserEntity? _selectedUser; // Может быть null, если пользователь не найден
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
                // 1. Сбрасываем все предыдущие выделения
                ClearHighlights();
                userListBox.SelectedItem = null;

                // 2. Ищем пользователя в общей коллекции (_users)
                _selectedUser = _users.FirstOrDefault(u => u.Id == userId);

                if (_selectedUser != null)
                {
                    // 3. Устанавливаем SelectedItem
                    userListBox.SelectedItem = _selectedUser;

                    // 4. Получаем контейнер ListBoxItem
                    ListBoxItem? container = userListBox.ItemContainerGenerator.ContainerFromItem(_selectedUser) as ListBoxItem;

                    if (container != null)
                    {
                        // 5. Помечаем как найденный
                        container.Tag = "Found";
                        container.Focus();
                    }
                    else
                    {
                        // 6. Если контейнер не создан (виртуализация), прокручиваем к элементу
                        userListBox.ScrollIntoView(_selectedUser);

                        // 7. Ждём создания контейнера
                        await Task.Delay(100);
                        container = userListBox.ItemContainerGenerator.ContainerFromItem(_selectedUser) as ListBoxItem;

                        if (container != null)
                        {
                            container.Tag = "Found";
                            container.Focus();
                        }
                        else
                        {
                            // 8. Пробуем ещё раз через Dispatcher
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
                userIdTextBox.Foreground = Brushes.Gray;
            }
        }


        // Метод для сброса выделений
        private void ClearHighlights()
        {
            foreach (var item in userListBox.Items)
            {
                ListBoxItem container = userListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (container != null)
                {
                    container.Tag = null; // Снимаем метку
                }
            }
        }


        private async Task ShowStatus(string message, Brush color, int durationSec = 3)
        {
            if (statusTextBlock == null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                statusTextBlock.Inlines.Clear();
                statusTextBlock.Inlines.Add(new Run(message) { Foreground = color });
                statusTextBlock.Opacity = 0;

                var appearAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(300));
                statusTextBlock.BeginAnimation(OpacityProperty, appearAnim);
            });

            await Task.Delay(durationSec * 100);

            await Dispatcher.InvokeAsync(() =>
            {
                var disappearAnim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(500));
                disappearAnim.Completed += (_, _) => statusTextBlock.Inlines.Clear();
                statusTextBlock.BeginAnimation(OpacityProperty, disappearAnim);
            });
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
