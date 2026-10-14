using HomeNetCore.Models;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IContactsVm
    {
        /// <summary> Клик по другу в левом списке </summary>
       

        // Было: public record FriendSelected(UserEntity Friend);
        // Надо сделать:
        public record FriendSelected(UserEntity Friend, Guid CurrentUserId);

    }
}
