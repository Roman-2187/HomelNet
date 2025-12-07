using HomeNetCore.Data.Interfaces;
namespace HomeNetCore.Services.UsersServices
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
       

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
          
        }

        public async Task<(bool success, string? userName)> LoginAsync(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return (false, null);

            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
                return (false, null);

            var isPasswordValid = email == user.Email;
            return (isPasswordValid, user.FirstName);
        }

        public bool ValidateEmailFormat(string email)
        {
            return new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").IsMatch(email);
        }
    }

   
   



}
