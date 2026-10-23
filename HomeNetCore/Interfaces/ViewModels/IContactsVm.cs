using HomeNetCore.Models;
using HomeNetCore.Models.Entity;

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
