namespace HomeNetCore.Models.Validation
{
    
       
        public record AuthenticationVerdict(bool IsSuccess, List<ValidationResult> Messages, UserEntity? User = null);

        /// <summary>
        /// 🔥 НАШ КРАСИВЫЙ РЕКОРД ДЛЯ СОСТАВА ДЕЛЕГАЦИИ ВАЛИДАЦИИ! 🧾🛸
        /// </summary>
        /// <param name="IsValid"></param>
        /// <param name="Results"></param>
        /// <param name="VerifiedUser"></param>
        public record RegistrationVerdict(bool IsValid, List<ValidationResult> Results, UserEntity? VerifiedUser = null);


    /// <summary>
    /// Сигнал о переключении активного чата.
    /// Кладётся в шину при клике на родственника или на "Общий чат".
    /// MainViewModel ловит его и принудительно перезагружает историю сообщений для выбранного экрана.
    /// </summary>
    /// <param name="TargetId">ID пользователя для ЛС, либо null, если это Общий чат</param>
    /// <param name="ChatType">Строгий маркер: "personal" или "group"</param>
    public record ChatSelectedMessage(int? TargetId, string ChatType);

    //



    /// <summary>
    /// Сигнал о прилёте нового сообщения. 
    /// Абсолютно лёгкий: вместо тяжелых байт передаёт только имя сохранённого файла.
    /// </summary>
    public record MessageReceivedMessage(
        int MessageId,
        int SenderId,
        string Text,
        string ChatType,
        string? FilePath = null); // Просто текстовый путь/имя файла, если картинка есть




}
