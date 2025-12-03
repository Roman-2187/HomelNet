using HomeNetCore.Helpers;
using HomeNetCore.Helpers.Exceptions;
using HomeNetCore.Models;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using WpfHomeNet.SubWindows;
using WpfHomeNet.UiHelpers;

namespace WpfHomeNet
{
    public partial class MainWindow
    {
        private async Task ExecuteAddUserOperation(UserEntity newUser, Button button)
        {
            ArgumentNullException.ThrowIfNull(newUser.Email);

            try
            {
                if (await UserService.EmailExistsAsync(newUser.Email))
                {
                    throw new DuplicateEmailException(newUser.Email);
                }

                await UserService.AddUserAsync(newUser);

                Logger.LogInformation($"Пользователь {newUser?.FirstName} добавлен");

                Status.SetStatus($"Пользователь {newUser?.FirstName} добавлен"); await Task.Delay(2000);

                await RefreshDataAsync();

                Status.SetStatus($"Загружено {MainVm.Users.Count} пользователей");
            }
            catch (DuplicateEmailException ex)
            {
                Logger?.LogWarning("Существующий имейл ");

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


        private async Task RefreshDataAsync()
        {
            try
            {
                Status.SetStatus("Обновление...");
                await Task.Delay(1000);
                await MainVm.LoadUsersAsync();
                Status.SetStatus("Список обновлён");
                await Task.Delay(1000);
                Status.SetStatus($"Загружено {MainVm.Users.Count} пользователей");
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка обновления: {ex.Message}");

                _logger?.LogError($"Ошибка обновления: {ex.Message}");
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


                var message = new MessageWindow($"Критическая ошибка: {ex.Message}")
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                message.Show();

            }
        }


        /// <summary>
        /// перестают работать привязки к окну логов если использовать анимацию для смешения главного окна к левому краю экрана
        /// </summary>
        /// <param name="logWindow"></param>

        private void WindowLogsAnimation(LogWindow logWindow)
        {
            // Целевая позиция главного окна (левый край экрана)
            double targetMainLeft = SystemParameters.WorkArea.Left;

            // Анимация сдвига главного окна
            var mainAnim = new DoubleAnimation
            {
                To = targetMainLeft,
                Duration = TimeSpan.FromSeconds(0.9),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };


            // Запускаем анимацию главного окна
            this.BeginAnimation(Window.LeftProperty, mainAnim);
        }


        private void PositionLogWindowRelativeToMain()
        {
            LogWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            const double margin = 5;
            double targetLeft = this.Left + this.ActualWidth + margin;
            double targetTop = this.Top;

            var workArea = SystemParameters.WorkArea;

            // Защита от выхода за экран
            if (targetLeft + 600 > workArea.Right)
                targetLeft = workArea.Right - 600;

            if (targetTop + 600 > workArea.Bottom)
                targetTop = workArea.Bottom - 600;

            LogWindow.Left = targetLeft;
            LogWindow.Top = targetTop;
        }


        // Метод включения синхронизации (чтобы не дублировать код)
        private void EnableSync()
        {
            this.LocationChanged += SyncLogWindowPosition;
            this.SizeChanged += SyncLogWindowPosition;
        }
     

        private void ShowUsersTable()
        {
            userTableView.Visibility = Visibility.Visible;
        }
        private void HideUsersTable()
        {
            userTableView.Visibility = Visibility.Collapsed;
        }

    }
}
