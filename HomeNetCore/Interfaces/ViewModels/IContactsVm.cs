using HomeNetCore.Models;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IContactsVm
    {
        /// <summary> Клик по другу в левом списке </summary>
        public record FriendSelected(UserEntity Friend);
    }
}
