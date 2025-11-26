using HomeSocialNetwork;
using WpfHomeNet.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HomeNetCore.Models;
using System.Data.Common;

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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();

            if (_logWindow is null || _logger is null)
            {                
            throw new InvalidOperationException(
                        $"Не инициализированы зависимости: " +
                        $"_logWindow: {_logWindow}, _logger: {_logger}, ");
            }

            else
            {
                _logWindow.Close();
            }
        }

        private async void RemoveUser_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно удаления
            var deleteWindow = new DeleteUserWindow(_userService,_logger);
            deleteWindow.Owner = this;
            deleteWindow.ShowDialog();

           await _mainVm.LoadUsersAsync();

            try
            {
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }



        }


        private void UsersGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) 
            {            
                DragMove();
                if (_logWindow == null)
                {
                    throw new InvalidOperationException("Логическое нарушение: окно логов не было создано");
                }
                _logWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _logWindow.Left = Application.Current.MainWindow.Left + 1005;
                _logWindow.Top = Application.Current.MainWindow.Top + 0;
            }                        
        }


        #region методы добавления user AddUser_Click

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            // Проверка диалога
            var dialog = new AddUserDialog { Owner = this };
            if (dialog.ShowDialog() != true) return;

            // Создание нового пользователя
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
                await ExecuteAddUserOperation(newUser, button);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private async Task ExecuteAddUserOperation(UserEntity newUser, Button button)
        {
            // Проверка зависимостей
            if (_userService is null || _logger is null || _status is null)
            {
                throw new ArgumentNullException(
                    $"Не инициализированы зависимости: " +
                    $"_userService: {_userService}, " +
                    $"_logger: {_logger}, " +
                    $"_status: {_status}");
            }

            try
            {
                await _userService.AddUserAsync(newUser);

                _logger.LogInformation($"Пользователь: {newUser.FirstName} с email: {newUser.Email} успешно добавлен");

                await RefreshDataAsync();


                await Task.Delay(2000);
                _status.SetStatus("Пользователь добавлен");
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при добавлении пользователя", ex.Message);
                throw;
            }
        }

        private void HandleException(Exception ex)
        {
            if (ex is ArgumentNullException)
            {
                MessageBox.Show(
                    $"Критическая ошибка: {ex.Message}",
                    "Ошибка инициализации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(
                    $"Не удалось добавить пользователя: {ex.Message}",
                    "Ошибка выполнения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }


        // Создаем отдельный метод для обновления
        private async Task RefreshDataAsync()
        {
            // 1. Показываем статус "Обновление..."
            if (_status is null || _mainVm is null)
                throw new ArgumentNullException(
                     $"Не инициализированы зависимости: " +
                     $"_userService: {_status}, " +
                     $"_logger: {_mainVm} ");

            try
            {
                // Показываем статус "Обновление..."
                _status.SetStatus("Обновление...");

                // Ждём 2 секунды
                await Task.Delay(2000);

                // Обновляем статус
                _status.SetStatus("Список обновлён");

                // Ждём ещё 2 секунды
                await Task.Delay(2000);

                // Загружаем пользователей
                await _mainVm.LoadUsersAsync();
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                _status.SetStatus($"Ошибка обновления: {ex.Message}");
                HandleException(ex);
            }
        }


    }
}



    

