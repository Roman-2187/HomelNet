using HomeNetCore.Helpers.Exceptions;
using HomeNetCore.Models;
using HomeSocialNetwork;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfHomeNet.SubWindows;
using WpfHomeNet.ViewModels;


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
                DragMove(); // Перемещаем главное окно

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



        private void ShowWindowLogs_Click(object sender, RoutedEventArgs e)
        {
            if (LogWindow.IsVisible)
            {
                // 1. Отписываемся от событий (даже если подписка была двойной)
                this.LocationChanged -= SyncLogWindowPosition;
                this.SizeChanged -= SyncLogWindowPosition;


                LogWindow.Hide();
                btnLogs.Content = "Показать логи";
            }
            else
            {
                LogWindow.Owner = this;

                // 2. Проверяем загрузку лог‑окна
                if (!LogWindow.IsLoaded)
                {
                    LogWindow.Loaded += (s, ev) =>
                    {
                        PositionLogWindowRelativeToMain(); // Позиционируем после загрузки
                        EnableSync(); // Включаем синхронизацию
                    };
                }

                // 3. Показываем окно (это запустит Loaded, если ещё не загружено)
                LogWindow.Show();


                // 4. Если окно уже загружено — сразу позиционируем и подписываемся
                if (LogWindow.IsLoaded)
                {
                    PositionLogWindowRelativeToMain();
                    EnableSync();
                }

                btnLogs.Content = "Скрыть логи";
            }
        }



        private void ShowUsers_Click(object sender, RoutedEventArgs e)
        {
            
            if (DataContext is MainViewModel vm)

            {    
                if (userTableView.Visibility == Visibility.Visible)
                {
                    HideUsersTable();
                  
                    ShowButton.Content = "Показать юзеров";
                }
                else
                {    ShowUsersTable();
                   
                    ShowButton.Content = "Скрыть юзеров";
                }
            }
            else
            {
                Debug.WriteLine("DataContext не является MainViewModel!");
            }
        }



        private async void AddUsersList_Click(object sender, RoutedEventArgs e)
        {
            var testData =  TestUserList.Users;
       
            try
            {
                foreach (var user in testData)
                {
                    await UserService.AddUserAsync(user);
                }
            }
            catch (DuplicateEmailException ex)
            {
                if (DataContext is MainViewModel vm)
                {
                   HideUsersTable(); ShowButton.Content = "Показать юзеров";
                }

                var messageBox = new MessageWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageBox.Show();
                messageBox.MessageText.Text = ex.GetUserMessage();              
            }

            catch (Exception ex)
            {
                HandleException(ex);
            }
        }     


        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
               HideUsersTable(); ShowButton.Content = "Показать юзеров";
            }
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

           HideUsersTable(); ShowButton.Content = "Показать юзеров";


            var deleteWindow = new DeleteUserDialog(mainVm.Users, UserService, Logger)
            {
                Owner = this,OnStatusUpdated = (message) =>                               
                    {
                        mainVm.SetStatus(message);
                    }
            };

            deleteWindow.ShowDialog();
        }


        private async void RefreshStatus_Click(object sender, RoutedEventArgs e)
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


        private void SyncLogWindowPosition(object sender, EventArgs e)
        {
            if (!LogWindow.IsLoaded || !LogWindow.IsVisible) return;

            LogWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            const double margin = 2;
            double targetLeft = this.Left + this.ActualWidth + margin;
            double targetTop = this.Top;

            var workArea = SystemParameters.WorkArea;

            LogWindow.Left = targetLeft;
            LogWindow.Top = targetTop;
        }

    }
}



    

