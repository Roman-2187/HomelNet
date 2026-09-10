using HomeNetCore.Models;

namespace WpfHomeNet.Messaging
{
    // Класс-посылка: сообщает всей системе, что выбран конкретный собеседник 📦
    public class FriendSelectedMessage
    {
        public UserEntity Friend { get; }

        public FriendSelectedMessage(UserEntity friend)
        {
            Friend = friend;
        }
    }
}
