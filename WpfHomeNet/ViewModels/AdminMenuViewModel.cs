using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static WpfHomeNet.ViewModels.LogViewModel;



namespace WpfHomeNet.ViewModels
{
    public class AdminMenuViewModel : INotifyPropertyChanged
    {       
        private MainViewModel? _mainVm;

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


        public AdminMenuViewModel()
        {
            ToggleLogWindowCommand = new RelayCommand(ExecuteToggleLogWindow);

            UserTableViewCommand = new RelayCommand(parameter => ExecuteUserTableViewVisible());
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






        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
 
    }
}
