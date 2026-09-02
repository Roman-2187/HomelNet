using HomeNetCore.Models;

namespace WpfHomeNet.Messaging
{
    // Общий контракт для любого успешного входа в систему
    public interface IAuthSuccessMessage
    {
        UserEntity User { get; }
    }
}
