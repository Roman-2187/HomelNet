using System.ComponentModel;
using System.Windows.Input;



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

        public AdminMenuViewModel()
           
        {           
            ToggleLogWindowCommand = new RelayCommand(ExecuteToggleLogWindow);        
        }


        public void ConnectToMainViewModel(MainViewModel mainVm) => MainVm = mainVm;
       
        private void ExecuteToggleLogWindow(object? parameter)
        {
            MainVm.LogVm.ShowLogWindowDelegate?.Invoke();
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
 
    }
}
