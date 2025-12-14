using HomeNetCore.Enums;
namespace HomeNetCore.Services.UsersServices
{
    public class ValidationResult
    {
        public TypeField Field { get; set; }
        public ValidationState State { get; set; } = ValidationState.None;
        public string Message { get; set; } = string.Empty;
        public bool IsInitialHint { get; }  // true для подсказок при загрузке

        public ValidationResult(TypeField field, string message, ValidationState state, bool isInitialHint = false)
        {
            Field = field;
            Message = message;
            State = state;
            IsInitialHint = isInitialHint;
        }

        public ValidationResult()
        {
            
        }
    }



}
