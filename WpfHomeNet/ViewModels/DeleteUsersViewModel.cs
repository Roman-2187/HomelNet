using HomeNetCore.Services;

using System;
using System.Windows;
using System.Windows.Input;


namespace WpfHomeNet.ViewModels
{
    public class DeleteUsersViewModel : FormViewModelBase
    {
        private readonly UserService _userService;
        private MainViewModel? _mainViewModel;



        public ICommand CancelCommand { get; }

        // Добавляем команду переключения видимости для меню!
        public ICommand ToggleDeleteUsersCommand { get; }

        public DeleteUsersViewModel(UserService userService)
        {
            _userService = userService;

            // По умолчанию форма скрыта
            ControlVisibility = Visibility.Collapsed;

            // Команда отмены (схлопывает форму)
            CancelCommand = new RelayCommand(
                execute: (obj) =>
                {
                    ResetForm();
                    ControlVisibility = Visibility.Collapsed;
                }
            );

            // Команда переключения (разворачивает форму, если она была скрыта)
            ToggleDeleteUsersCommand = new RelayCommand(
                execute: (parameter) =>
                {
                    if (ControlVisibility == Visibility.Collapsed)
                    {
                        ResetForm();
                        ControlVisibility = Visibility.Visible; // Показываем!
                    }
                    else
                    {
                        ResetForm();
                        ControlVisibility = Visibility.Collapsed; // Скрываем
                    }
                }
            );
        }

        public void ConnectToMainViewModel(MainViewModel mainVm) => _mainViewModel = mainVm;

        private void ResetForm()
        {
            StatusMessage = string.Empty;
            // Сюда потом допишем очистку userIdTextBox.Text = string.Empty;
        }
    }
}



