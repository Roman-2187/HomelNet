using HomeSocialNetwork;
using WpfHomeNet.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HomeNetCore.Models;

namespace WpfHomeNet
{
    public partial class MainWindow
    {
        private void ShowScrollViewerButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.ShowScrollViewer();
            else
                Debug.WriteLine("DataContext не является MainViewModel!");
        }
        private async void ShowUser_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {

                vm.ScrollViewerVisibility = Visibility.Visible;
            }
            else
            {
                Debug.WriteLine("DataContext не является MainViewModel. Проверьте привязку в XAML.");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void UsersGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddUserDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() != true) return;

            var newUser = new UserEntity
            {
                FirstName = dialog.FirstName,
                LastName = dialog.LastName,
                PhoneNumber = dialog.PhoneNumber,
                Email = dialog.Email,
                Password = dialog.Password
            };

            var button = (Button)sender;
            button.IsEnabled = false;

            try
            {
                // Делаем асинхронным!
                await _userService.AddUserAsync(newUser);
                _logger.LogInformation("Пользователь добавлен");

                await Task.Delay(2000);

                _status.SetStatus("Пользователь добавлен");

            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось добавить пользователя: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // 1. Показываем статус "Обновление..."
            _status.SetStatus("Обновление...");

            // 2. Ждём 2 секунды
            await Task.Delay(2000);

            try
            {
                // 3. Обновляем статус на "Список обновлён"
                _status.SetStatus("Список обновлён");

                // 4. Ждём ещё 2 секунды
                await Task.Delay(2000);

                // 5. Загружаем пользователей
                await _mainVm.LoadUsersAsync();
            }
            catch (Exception ex)
            {
                // В случае ошибки показываем сообщение
                _status.SetStatus($"Ошибка обновления: {ex.Message}");
            }
        }


    }
}



    

