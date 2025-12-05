using HomeNetCore.Services;

namespace WpfHomeNet.UiHelpers
{
    public class AuthManager(UserService userService)
    {
        private readonly UserService _userService = userService;

        // Этап 1: Проверка email
        public async Task<bool> ValidateEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            if (!IsValidEmailFormat(email))
                return false;

            return await _userService.CheckEmailExistsAsync(email);
        }

        // Этап 2: Проверка пароля
        public async Task<(bool success, string? userName)> ValidatePasswordAsync(string savedEmail, string password)
        {
            if (string.IsNullOrEmpty(password))
                return (false, null);

            // Предполагаем, что ValidateCredentialsAsync теперь возвращает кортеж
            return await _userService.ValidateCredentialsAsync(savedEmail, password);
        }

        public bool IsValidEmailFormat(string email)
        {
            return new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").IsMatch(email);
        }        
    }


   
}
