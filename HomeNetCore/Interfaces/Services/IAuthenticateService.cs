using HomeNetCore.Models;
using HomeNetCore.Models.Validation;


namespace HomeNetCore.Interfaces
{
    public interface IAuthenticateService
    {
        /// <summary>
        /// 🔥 Вложенный вердикт авторизации. 
        /// Доступен через IAuthenticateService.Verdict
        /// </summary>
        public record Verdict(bool IsSuccess, List<ValidationResult> Messages, UserEntity? User = null);

        // Теперь метод возвращает четкую структуру вместо безликого кортежа!
        Task<Verdict> CheckUserAsync(UserEntity userInput);
    }
}
