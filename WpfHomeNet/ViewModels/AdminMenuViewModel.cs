using HomeNetCore.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using static WpfHomeNet.ViewModels.LogViewModel;
using HomeNetCore.Models;



namespace WpfHomeNet.ViewModels
{
    public class AdminMenuViewModel : INotifyPropertyChanged
    {       
        private MainViewModel? _mainVm;
       
        private UserService _userService;

        // Сюда главная модель передаст команду на обновление списка
        public Action? OnDataSeeded { get; set; }


        public MainViewModel MainVm
        {
            get => _mainVm ?? throw new InvalidOperationException($"{nameof(_mainVm)} не инициализирован");
            set => _mainVm = value;
        }
       
        public ICommand ToggleLogWindowCommand { get; }

        public ICommand UserTableViewCommand { get; private set; }

        // Свойство для текста кнопки
        private string _toggleButtonText = "Показать лог";
        private string _tableButtonText = "Показать users";

        public string ToggleButtonText
        {
            get => _toggleButtonText;
            set
            {
                _toggleButtonText = value;
                OnPropertyChanged(nameof(ToggleButtonText));
            }
        }

        public string TableButtonText
        {
            get => _tableButtonText;
            set
            {
                _tableButtonText = value;
                OnPropertyChanged(nameof(TableButtonText));
            }
        }


        public AdminMenuViewModel(UserService userService)
        {
            ToggleLogWindowCommand = new RelayCommand(ExecuteToggleLogWindow);

            UserTableViewCommand = new RelayCommand(parameter => ExecuteUserTableViewVisible());

            _userService = userService;
           
        }

        public void ConnectToMainViewModel(MainViewModel mainVm) => MainVm = mainVm;

        private void ExecuteToggleLogWindow(object? parameter)
        {
            if (MainVm.LogVm.ShowLogWindowDelegate == null)
                return;

            var state = MainVm.LogVm.ShowLogWindowDelegate();

            ToggleButtonText = state == LogWindowState.Visible ? "Скрыть лог" : "Показать лог";
        }


        private void ExecuteUserTableViewVisible()
        {

            if (MainVm.PanelVisibility == Visibility.Collapsed)
            {
                MainVm.PanelVisibility = Visibility.Visible;
                TableButtonText = "Скрыть users";
            }
                
            else
            {
                MainVm.PanelVisibility = Visibility.Collapsed;

                TableButtonText = "Показать users";
            }
        }

        // Команда для кнопки
        public ICommand SeedDataCommand => new RelayCommand(async _ => await ExecuteSeedDataAsync());

        private async Task ExecuteSeedDataAsync()
        {
            int addedCount = 0;

            try
            {
                foreach (var user in DbSeedData.Users)
                {
                    // Проверяем email на уникальность перед заливкой
                    bool emailExists = await _userService.CheckEmailExistsAsync(user.Email);

                    if (!emailExists)
                    {
                        await _userService.AddUserAsync(user);
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    // Дергаем за ниточку Главную модель, чтобы она перечитала базу данных!
                    OnDataSeeded?.Invoke();
                }
                else
                {
                    // Если добавлено 0 (нажали второй раз) — пишем в дебаг, что все уже там
                    System.Diagnostics.Debug.WriteLine("Все пользователи уже добавлены!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка генерации: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
