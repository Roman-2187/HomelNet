using CommunityToolkit.Mvvm.ComponentModel; // Нано-штамповщик свойств ✨
using CommunityToolkit.Mvvm.Input;        // Нано-штамповщик команд 🚀
using HomeNetCore.Models;
using HomeNetCore.Services.DeleteService;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    // ОБЯЗАТЕЛЬНО пишем partial, чтобы Студия дорисовала кишки! 🧼
    public partial class DeletionUsersViewModel : FormViewModelBase
    {
        #region Поля (Штамповочный цех автоматических свойств) 🦾
        private readonly DeleteService _deleteService;

        [ObservableProperty]
        private ObservableCollection<UserEntity>? _mainUsersList; 

        [ObservableProperty]
        private UserEntity? _selectedUser; 

        [ObservableProperty]
        private string _targetUserId = string.Empty; 

        [ObservableProperty]
        private bool _canDelete; 
        #endregion

        #region ХИТРЫЕ ХУКИ: Реакция на изменение полей (Твоя логика перенесена сюда!) ⚙️

        // Этот метод сам вызовется внутри скрытого сеттера, когда выберут юзера в таблице!
        partial void OnSelectedUserChanged(UserEntity? value)
        {
            if (value != null)
            {         
                TargetUserId = value.DisplayInfo.ToString();
                CanDelete = true;
            }
        }

        // Этот метод сам вызовется внутри скрытого сеттера, когда изменится текст в поле поиска!
        partial void OnTargetUserIdChanged(string value)
        {
            // Сообщаем кнопке поиска, что текст изменился
            SearchCommand.NotifyCanExecuteChanged();

            // ХИТРЫЙ ХАК: Проверяем, можно ли активировать кнопку удаления
            if (int.TryParse(value, out int parsedId) && parsedId > 0)
            {
                CanDelete = true;
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                CanDelete = false;
            }

            // Принудительно заставляем кнопку удаления пересчитать свой статус!
            DeleteCommand.NotifyCanExecuteChanged();
        }

        // Этот метод сам вызовется, когда флаг CanDelete изменится
        partial void OnCanDeleteChanged(bool value)
        {
            // Кнопка удаления мгновенно разблокируется в UI!
            DeleteCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Конструктор
        public DeletionUsersViewModel(DeleteService deleteService, EventBus eventBus) : base(eventBus)
        {
            _deleteService = deleteService ?? throw new ArgumentNullException(nameof(deleteService));
            ControlVisibility = Visibility.Collapsed; //
            SubmitButtonText = "Удалить"; //
            StatusMessage = "Введите ID "; //

            // ПРАВКА СТАРТА: Сразу принудительно шлём в автобус сигнал, что мы скрыты!
            _eventBus.Publish(new FormVisibilityChangedMessage(this.GetType(), Visibility.Collapsed));

            // СЛУШАЕМ АВТОБУС: Если открылась любая ДРУГАЯ форма — мы мгновенно тушимся
            _eventBus.Subscribe<FormVisibilityChangedMessage>(msg =>
            {
                if (msg.FormType != this.GetType() && msg.Visibility == Visibility.Visible)
                {
                    ResetForm();
                    ControlVisibility = Visibility.Collapsed;
                }
            });

            _eventBus.Subscribe<UsersListRefreshedMessage>(msg =>
            {
                if (msg.Users != null)
                {
                    MainUsersList = msg.Users; 
                }
            });
        }
        #endregion

        #region НАНО-КОМАНДЫ (Идеальные имена для генератора от Microsoft! 🧼)

        [RelayCommand(CanExecute = nameof(CanExecuteSearch))]
        private async Task SearchAsync() // Было: SearchCommandAsync. Теперь сгенерирует ровно SearchCommand! 🦾
        {
            StatusMessage = "Поиск пользователя в базе данных...";
            CanDelete = false;
            var (isSuccess, message, _) = await _deleteService.SearchUserAsync(TargetUserId);
            StatusMessage = message;
            CanDelete = isSuccess;
        }
        private bool CanExecuteSearch() => !string.IsNullOrWhiteSpace(TargetUserId);


        [RelayCommand(CanExecute = nameof(CanExecuteDelete))]
        private async Task DeleteAsync() // Было: DeleteCommandAsync. Теперь сгенерирует ровно DeleteCommand! 🔥
        {
            int id = SelectedUser?.Id ?? (int.TryParse(TargetUserId, out int parsedId) ? parsedId : -1);
            if (id == -1) return;
            StatusMessage = $"Удаление пользователя с ID {id}...";
            var (isSuccess, message) = await _deleteService.DeleteUserAsync(id);
            StatusMessage = message;
            if (isSuccess)
            {
                var userToRemove = MainUsersList?.FirstOrDefault(u => u.Id == id);
                if (userToRemove != null)
                {
                    Application.Current.Dispatcher.Invoke(() => MainUsersList?.Remove(userToRemove));
                }
                _eventBus.Publish(new UserDeletedMessage(id));
                ResetForm();
            }
        }
        private bool CanExecuteDelete() => CanDelete;


        [RelayCommand]
        private void Cancel() 
        {
            ResetForm();
            ControlVisibility = Visibility.Collapsed;
            _eventBus.Publish(new FormVisibilityChangedMessage(this.GetType(), Visibility.Collapsed));
        }

        #endregion

        #region Логика работы
        private void ResetForm()
        {
            TargetUserId = string.Empty;
            StatusMessage = "Введите ID ";
            CanDelete = false;
            SelectedUser = null;
        }
        #endregion

        
    }
}
