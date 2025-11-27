using HomeSocialNetwork;
using WpfHomeNet.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HomeNetCore.Models;
using HomeNetCore.Helpers.Exceptions;


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
                      
           _logWindow.Close();            
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
                                  
            if (_logWindow.IsVisible)
            {                
                _logWindow.Hide();
                CenterMainAndHideLogs();
            }
            else
            {                
                ShowWindowLogs(_logWindow);                
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
            var mainVm = (MainViewModel)DataContext;

            var deleteWindow = new DeleteUserDialog(mainVm.Users, _userService, _logger)
            {
                Owner = this
            };

            // Подключаем обработчик статуса
            deleteWindow.OnStatusUpdated = (message, color) =>
            {
                // Здесь вы можете:
                // - обновить TextBlock в главном окне
                // - вызвать метод ViewModel
                // - показать MessageBox
                mainVm.SetStatus(message) ; // Если в VM есть такое свойство
                
            };

            deleteWindow.ShowDialog();
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
            try
            {                
                _status.SetStatus("Обновление...");
                
                await Task.Delay(2000); 
                await _mainVm.LoadUsersAsync();
                _status.SetStatus("Список обновлён");
                await Task.Delay(2000);

               
               
            }
            catch (Exception ex)
            {
                _status.SetStatus($"Ошибка обновления: {ex.Message}");

                HandleException(ex);
            }
        }


        private async Task ExecuteAddUserOperation(UserEntity? newUser, Button button)
        {
            try
            {
                                               
                if (await _userService.EmailExistsAsync(newUser?.Email))
                {
                    throw new DuplicateEmailException(newUser?.Email);
                }

                await _userService.AddUserAsync(newUser);

                _logger.LogInformation($"Пользователь {newUser?.FirstName} добавлен");

                await RefreshDataAsync();
                
                _status.SetStatus("Пользователь добавлен");

                await Task.Delay(2000);

                _status.SetStatus($"Загружено {_mainVm.Users.Count}");
            }
            catch (DuplicateEmailException ex)
            {
                _logger?.LogWarning("Существующий имейл ");

                MessageBox.Show(ex.GetUserMessage(),string.Empty,
                MessageBoxButton.OK, MessageBoxImage.Information);
        
            }
            catch (Exception ex)
            {
                HandleException(ex);
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
        }

        #endregion
    }
}



    

