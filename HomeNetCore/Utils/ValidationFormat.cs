using System.Text.RegularExpressions;






namespace HomeNetCore.Utils
{
    /// <summary>
    /// Чистые статические утилиты для мгновенной проверки форматов строк (Regex-движок).
    /// Размещается в Ядре, так как используется сквозным образом в UI, Сервисах и Профиле.
    /// </summary>
    public static class ValidationFormat
    {
        // 🔥 Скомпилированные регулярки инициализируются один раз при старте приложения и работают со скоростью света
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex PasswordRegex = new(@"^(?=.*[a-zA-Z])(?=.*\d)[A-Za-z\d]{8,}$", RegexOptions.Compiled);
        private static readonly Regex UserNameRegex = new(@"^[a-zA-Zа-яА-Я0-9_]{3,20}$", RegexOptions.Compiled); // Ограничили длину логина от 3 до 20 символов
        private static readonly Regex PhoneRegex = new(@"^\+?\d{10,15}$", RegexOptions.Compiled);

        // 🛠️ ЗАДЕЛ ПОД РАСШИРЕННЫЙ ПРОФИЛЬ (Твоя будущая логика редактирования)
        private static readonly Regex StatusRegex = new(@"^[^\r\n]{0,100}$", RegexOptions.Compiled); // Статус до 100 символов, в одну строку
        private static readonly Regex UrlRegex = new(@"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$", RegexOptions.Compiled);

        public static bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());

        public static bool IsValidPassword(string password) =>
            !string.IsNullOrEmpty(password) && PasswordRegex.IsMatch(password);

        public static bool IsValidUserName(string userName) =>
            !string.IsNullOrWhiteSpace(userName) && UserNameRegex.IsMatch(userName.Trim());

        public static bool IsValidPhone(string phone) =>
            !string.IsNullOrWhiteSpace(phone) && PhoneRegex.IsMatch(phone.Trim());

        /// <summary> Проверка кастомного статуса в профиле (не более 100 символов, без переносов) </summary>
        public static bool IsValidStatus(string status) =>
            status == null || StatusRegex.IsMatch(status);

        /// <summary> Проверка ссылки на личный сайт или блог в профиле </summary>
        public static bool IsValidUrl(string url) =>
            string.IsNullOrWhiteSpace(url) || UrlRegex.IsMatch(url.Trim());
    }
}

