using System.Windows;
using System.Windows.Controls;
namespace HomeSocialNetwork
{
    public partial class AddUserDialog : Window
    {
        #region Свойства формы
        public string FirstName => FirstNameTextBox.Text;
        public string LastName => LastNameTextBox.Text;
        public string PhoneNumber => PhoneNumberTextBox.Text;
        public string Email => EmailTextBox.Text;
        public string Password => PasswordBox.Password;
        #endregion

        #region Константы сообщений
        private const string EmptyFirstNameMsg = "имя";
        private const string EmptyEmailMsg = "email";
        private const string EmptyPasswordMsg = "пароль";
        #endregion

        public AddUserDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FirstName))
                {
                    EmptyInput(EmptyFirstNameMsg);
                    FirstNameTextBox.Focus();
                    return;
                }

                if (!IsValidEmail(Email))
                {
                    EmptyInput(EmptyEmailMsg);
                    EmailTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    EmptyInput(EmptyPasswordMsg);
                    PasswordBox.Focus();
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Критическая ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) && email.Contains('@');

        private static void EmptyInput(string fieldName, string title = "Ошибка")
        {
            MessageBox.Show
                ($"Пожалуйста, укажите  {fieldName}",
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

}
