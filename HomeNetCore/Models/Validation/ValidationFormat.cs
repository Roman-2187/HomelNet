using System.Text.RegularExpressions;
namespace HomeNetCore.Services.UsersServices
{
    public class ValidationFormat
    {
        public bool IsValidEmailFormat(string email)
        {
            
            return new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").IsMatch(email.Trim());
        }

        public bool ValidatePasswordFormat(string password)
        {
            return new Regex(@"^(?=.*[a-zA-Z])(?=.*\d)[A-Za-z\d]{8,}$").IsMatch(password);
        }

        public bool ValidateUserNameFormat(string userName)
        {
            return new Regex( @"[a-zA-Za-яА-Я]{3,}").IsMatch(userName.Trim());
        }


        public bool ValidatePhoneFormat(string phone)
        {
            return new Regex(@"^\+?\d{10,15}$").IsMatch(phone);
        }
    }

}
