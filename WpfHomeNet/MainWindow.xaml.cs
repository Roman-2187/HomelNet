using HomeNetCore.Services;
using HomeSocialNetwork;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.ViewModels;

namespace WpfHomeNet
{
    
    public partial class MainWindow : Window
    {
        
        private UserService? _userService;
        private MainViewModel? _mainVm;

        public MainWindow()
        {                  
           

            var app = (App)Application.Current;
            _userService = app.UserService;

            _mainVm = app.MainVm; 

            _mainVm.LogWindow= app.LogWindow;
            _mainVm.AdminMenuViewModel.ConnectMainWindow(this);
            DataContext = _mainVm;
             InitializeComponent(); 
           
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Закрываем LogWindow (если он открыт как отдельное окно)
            if (_mainVm.LogWindow != null)
            {
                _mainVm.LogWindow.Close();
            }

            // 2. Закрываем главное окно
            this.Close();
        }



       


        private void WindowDrag_MouseDown(object sender, MouseButtonEventArgs e) => this.DragMove();
    }
}