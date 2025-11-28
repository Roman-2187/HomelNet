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
              
                LogWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                LogWindow.Left = Application.Current.MainWindow.Left + 1005;
                LogWindow.Top = Application.Current.MainWindow.Top + 0;
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
                      
           LogWindow.Close();            
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
                                  
            if (LogWindow.IsVisible)
            {                
                LogWindow.Hide();
                CenterMainAndHideLogs();
            }
            else
            {                
                ShowWindowLogs(LogWindow);                
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

            var deleteWindow = new DeleteUserDialog(mainVm.Users, UserService, Logger)
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
                Status.SetStatus("Обновление...");
                
                await Task.Delay(2000); 
                await MainVm.LoadUsersAsync();
                Status.SetStatus("Список обновлён");
                await Task.Delay(2000);

               
               
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка обновления: {ex.Message}");

                HandleException(ex);
            }
        }



        private async void AddUsersList_Click(object sender, RoutedEventArgs e)
        {
            var testData = new TestUserList().GetUsers();
       
            try
            {
                foreach (var user in testData)
                {

                    if (await UserService.EmailExistsAsync(user?.Email))
                    {
                        throw new DuplicateEmailException(user?.Email);
                    }

                    await UserService.AddUserAsync(user);
                }
            }
            catch (DuplicateEmailException ex)
            {

                Logger?.LogWarning("Существующий имейл ");

                MessageBox.Show(ex.GetExistEmail(), string.Empty,
                MessageBoxButton.OK, MessageBoxImage.Information);
            }

            catch (Exception ex)
            {
                HandleException(ex);
            }
        }



        private async Task ExecuteAddUserOperation(UserEntity? newUser, Button button)
        {
            try
            {
                                               
                if (await UserService.EmailExistsAsync(newUser?.Email))
                {
                    throw new DuplicateEmailException(newUser?.Email);
                }

                await UserService.AddUserAsync(newUser);

                Logger.LogInformation($"Пользователь {newUser?.FirstName} добавлен");

                await RefreshDataAsync();
                
                Status.SetStatus("Пользователь добавлен");

                await Task.Delay(2000);

                Status.SetStatus($"Загружено {MainVm.Users.Count}");
            }
            catch (DuplicateEmailException ex)
            {
                Logger?.LogWarning("Существующий имейл ");

                MessageBox.Show(ex.GetExistEmail(),string.Empty,
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



    

