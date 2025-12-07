using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using System.Text.RegularExpressions;
namespace HomeNetCore.Services.UsersServices
{
    public class RegisterService(IUserRepository userRepository)
    {
        private readonly IUserRepository _userRepository = userRepository;

        public enum ValidationState
        {
            Success,
            Error
        }

        public record ValidationResult(ValidationState State, string Message);

        public async Task<ValidationResult> RegisterUserAsync(
            string email,
            string password,
            string userName,
            string phone)
        {
            // Проверяем email
            if (!await ValidateEmailAsync(email))
                return new ValidationResult(ValidationState.Error, "Неверный email или он уже существует");

            // Проверяем пароль
            if (!ValidatePassword(password))
                return new ValidationResult(ValidationState.Error, "Пароль не соответствует требованиям");

            // Проверяем имя
            if (!ValidateUserName(userName))
                return new ValidationResult(ValidationState.Error, "Некорректное имя пользователя");

            // Проверяем телефон
            if (!ValidatePhone(phone))
                return new ValidationResult(ValidationState.Error, "Некорректный формат телефона");

            // Сохраняем пользователя
            await _userRepository.InsertUserAsync(new UserEntity
            {
                Email = email,
                Password = password,
                FirstName = userName,
                PhoneNumber = phone
            });

            return new ValidationResult(ValidationState.Success, "Регистрация успешно завершена");
        }

        private async Task<bool> ValidateEmailAsync(string email)
        {
            if (!IsValidEmailFormat(email))
                return false;

            return !(await _userRepository.EmailExistsAsync(email));
        }

        private bool IsValidEmailFormat(string email)
        {
            return new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").IsMatch(email);
        }

        private bool ValidatePassword(string password)
        {
            return new Regex(@"^(?=.*[a-zA-Z])(?=.*\d)[A-Za-z\d]{8,}$").IsMatch(password);
        }

        private bool ValidateUserName(string userName)
        {
            return !string.IsNullOrEmpty(userName) &&
                   new Regex(@"^[a-zA-Z\s]+$").IsMatch(userName);
        }

        private bool ValidatePhone(string phone)
        {
            return new Regex(@"^\+?\d{10,15}$").IsMatch(phone);
        }
    }






}
