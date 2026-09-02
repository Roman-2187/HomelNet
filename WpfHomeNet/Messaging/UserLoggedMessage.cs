using HomeNetCore.Models;
using WpfHomeNet.Messaging;

public class UserLoggedMessage : IAuthSuccessMessage
{
    public UserEntity User { get; }

    // 🔥 ДОБАВЛЯЕМ ФЛАГ: Если true — значит вход был инициирован админом
    public bool IsFromAdminPanel { get; }

    // Конструктор по умолчанию ставит false, чтобы обычный вход не сломался!
    public UserLoggedMessage(UserEntity user, bool isFromAdminPanel = false)
    {
        User = user;
        IsFromAdminPanel = isFromAdminPanel;
    }
}


