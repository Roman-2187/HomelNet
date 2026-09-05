namespace HomeNetCore.Models.Validation
{
    
       
        public record AuthenticationVerdict(bool IsSuccess, List<ValidationResult> Messages, UserEntity? User = null);

        // 🔥 НАШ КРАСИВЫЙ РЕКОРД ДЛЯ СОСТАВА ДЕЛЕГАЦИИ ВАЛИДАЦИИ! 🧾🛸
        public record RegistrationVerdict(bool IsValid, List<ValidationResult> Results, UserEntity? VerifiedUser = null);

   
}
