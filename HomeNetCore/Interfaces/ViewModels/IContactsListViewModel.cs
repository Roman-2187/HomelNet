using HomeNetCore.Models;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IContactsListViewModel
    {
        /// <summary> Клик по другу в левом списке </summary>
        public record FriendSelected(UserEntity Friend);
    }
}
