using HomeNetCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HomeSocialNetwork
{
    public partial class AddUserDialog : Window
    {   public string FirstName => FirstNameTextBox.Text;
        public string LastName => LastNameTextBox.Text;
        public string PhoneNumber => PhoneNumberTextBox.Text;
        public string Email => EmailTextBox.Text;
        public string Password => PasswordBox.Password;
  
        
        public AddUserDialog()
        {
            InitializeComponent();            
        }
            
        // Свойства для получения данных из формы
      
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация обязательных полей
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                MessageBox.Show(
                    "Пожалуйста, укажите имя (FirstName).",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                FirstNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                MessageBox.Show(
                    "Пожалуйста, укажите email.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                EmailTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show(
                    "Пожалуйста, укажите пароль.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                PasswordBox.Focus();
                return;
            }

            // Если все проверки пройдены — подтверждаем действие
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

}
