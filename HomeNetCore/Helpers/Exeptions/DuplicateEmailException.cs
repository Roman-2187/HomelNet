namespace HomeNetCore.Helpers.Exceptions
{
    public class DuplicateEmailException : Exception
    {
        public string Email { get; }

        // Основной конструктор: только email → сообщение генерируется автоматически
        public DuplicateEmailException(string email)
            : base($"Email {email} уже существует")
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }

        // Конструктор с внутренним исключением (message генерируется автоматически)
        public DuplicateEmailException(string email, Exception innerException)
            : base($"Email {email} уже существует", innerException)
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }

        // Конструктор с явным сообщением (опционально)
        public DuplicateEmailException(string email, string message)
            : base(message)
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }

        // Конструктор с message + innerException (опционально)
        public DuplicateEmailException(string email, string message, Exception innerException)
            : base(message, innerException)
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }

        // Метод для получения пользовательского сообщения
        public string GetUserMessage()
        {
            return $"Email {Email} уже существует. Используйте другой email.";
        }
    }




}