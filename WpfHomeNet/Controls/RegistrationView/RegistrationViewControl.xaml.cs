using HomeNetCore.Data.Interfaces;
using HomeNetCore.Services.UsersServices;
using System.Windows.Controls;

namespace WpfHomeNet.Controls.RegistrationView
{
    /// <summary>
    /// Interaction logic for RegistrationViewControl.xaml
    /// </summary>
    public partial class RegistrationViewControl : UserControl
    {
       IUserRepository _userRepository;
        public RegistrationViewControl(IUserRepository userRepository)
        {
            InitializeComponent(); 
            _userRepository = userRepository;
            DataContext = new RegistrationViewModel(new RegisterService(_userRepository));
        }
    }
}
