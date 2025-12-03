using HomeNetCore.Helpers.Exceptions;
using HomeNetCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WpfHomeNet.SubWindows;

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

                MessageBox.Show(ex.GetUserMessage(), string.Empty,
                MessageBoxButton.OK, MessageBoxImage.Information);

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







       



        private void ShowWindowLogs(LogWindow logWindow)
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

            const double margin = 20;
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

        // Метод включения синхронизации (чтобы не дублировать код)
        private void EnableSync()
        {
            this.LocationChanged += SyncLogWindowPosition;
            this.SizeChanged += SyncLogWindowPosition;
        }




        private void CenterMainAndHideLogs()
        {
            this.Left = 150;
            this.Top = 200;
            btnLogs.Content = "Показать логи";
        }
        
        
        
        
        
        
        
        
        
        private bool _isAnimating = false;
    }


   
    }
