using HomeNet.Models.Shared.Entity;
using HomeNetCore.Validation;


namespace HomeNetCore.Interfaces.Services
{
    public interface IRegistrationService
    {
        /// <summary>
        /// 🔥 Вложенный вердикт регистрации.
        /// Доступен через IRegistrationService.Verdict
        /// </summary>
        public record Verdict(bool IsValid, List<ValidationResult> Results, UserEntity? VerifiedUser = null);

        Task<Verdict> RegisterUserAsync(UserEntity user);
    }
}
