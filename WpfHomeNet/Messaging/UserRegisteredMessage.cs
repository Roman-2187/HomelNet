using HomeNetCore.Models;
using WpfHomeNet.Messaging;

public class UserRegisteredMessage : IAuthSuccessMessage
{
    public UserEntity User { get; }

    // 🔥 ДОБАВЛЯЕМ ФЛАГ АДМИНА
    public bool IsFromAdminPanel { get; }

    // Конструктор по умолчанию ставит false, чтобы обычный софт не сломался
    public UserRegisteredMessage(UserEntity user, bool isFromAdminPanel = false)
    {
        User = user;
        IsFromAdminPanel = isFromAdminPanel;
    }
}
