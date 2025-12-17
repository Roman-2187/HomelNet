using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Controls;
using WpfHomeNet.UiHelpers;



namespace WpfHomeNet.ViewModels
{
    public class AdminMenuViewModel : INotifyPropertyChanged
    {       
        private MainViewModel? _mainVm;
        public MainViewModel MainVm => _mainVm ?? throw new InvalidOperationException("_mainVm не инициализирован");
       
        private readonly LogQueueManager _logQueueManager;

  
        public ICommand ToggleLogWindowCommand { get; }


        // Свойство для текста кнопки
        private string _toggleButtonText = "Показать лог";
        public string ToggleButtonText
        {
            get => _toggleButtonText;
            set
            {
                _toggleButtonText = value;
                OnPropertyChanged(nameof(ToggleButtonText));
            }
        }

        public AdminMenuViewModel( LogQueueManager logQueueManager)
           
        {
            _logQueueManager = logQueueManager;

            ToggleLogWindowCommand = new RelayCommand(ExecuteToggleLogWindow); 
        }


        public void ConnectToMainViewModel(MainViewModel mainVm) => _mainVm = mainVm;
       
        private void ExecuteToggleLogWindow(object? parameter)
        {           
            MainVm.ToggleLogWindow();

            _logQueueManager.SetReady();
 
            ToggleButtonText = MainVm.LogVm.IsVisible
                ? "Скрыть лог"
                : "Показать лог";
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
 
    }
}
