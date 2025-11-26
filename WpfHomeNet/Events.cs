using HomeSocialNetwork;
using WpfHomeNet.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HomeNetCore.Models;
using HomeNetCore.Helpers.Exeptions;


namespace WpfHomeNet
{
    public partial class MainWindow
    {

        #region управление оконами

        /// <summary>
        /// таскальщик главного с окном логов с позиционированием справа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void WindowDrag_MouseDown(object sender, MouseButtonEventArgs e)
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



        /// <summary>
        /// закрывает главное окно одно временно с окном логов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="InvalidOperationException"></exception>
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

        #endregion



        private void ShowUsers_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                if (vm.ScrollViewerVisibility == Visibility.Visible)
                {
                    vm.HideScrollViewer();
                    ShowButton.Content = "Показать юзеров";
                }
                else
                {
                    vm.ShowScrollViewer();
                    ShowButton.Content = "Скрыть юзеров";
                }
            }
            else
            {
                Debug.WriteLine("DataContext не является MainViewModel!");
            }
        }

        private void ShowWindowLogs_Click(object sender, RoutedEventArgs e)
        {

            // Проверяем на null и бросаем исключение
            if (_logWindow == null)
            {
                throw new InvalidOperationException("Логическое нарушение: окно логов не было создано");
            }

            if (_logWindow.IsVisible)
            {
                // Скрываем логи и центрируем главное окно
                _logWindow.Hide();
                CenterMainAndHideLogs();
            }
            else
            {
                // Показываем логи и сдвигаем окна
                ShowLogsAndShift(_logWindow);
            }
        }

       
      

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

        private async void RemoveUser_Click(object sender, RoutedEventArgs e)
        {
            if (_userService is null || _logger is null)
            {
                throw new InvalidOperationException(
                            $"Не инициализированы зависимости: " +
                            $"_userService: {_userService}, _logger: {_logger}, ");
            }
            // Открываем окно удаления
            var deleteWindow = new DeleteUserWindow(_userService,_logger);
            deleteWindow.Owner = this;
            deleteWindow.ShowDialog();
            try
            {
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

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




        #region методы логики обработки пользователей добавление удаление обновление
        



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


        private async Task ExecuteAddUserOperation(UserEntity newUser, Button button)
        {
            try
            {
                // Проверяем зависимости
                if (_userService is null || _logger is null || _status is null)
                    throw new ArgumentNullException("Не инициализированы зависимости");

                // Предварительная проверка email
                if (await _userService.EmailExistsAsync(newUser.Email))
                {
                    throw new DuplicateEmailException(newUser.Email);
                }

                await _userService.AddUserAsync(newUser);

                _logger.LogInformation($"Пользователь {newUser.FirstName} добавлен");
                await RefreshDataAsync();
                _status.SetStatus("Пользователь добавлен");
            }
            catch (DuplicateEmailException ex)
            {
                HandleDuplicateEmail(ex);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void HandleDuplicateEmail(DuplicateEmailException ex)
        {
            _logger?.LogError($"Попытка регистрации существующего email: {ex.Email}");
            MessageBox.Show(
                $"Email {ex.Email} уже зарегистрирован в системе",
                "Ошибка регистрации",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
    }
}



    

