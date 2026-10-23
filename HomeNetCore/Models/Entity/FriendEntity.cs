using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetCore.Models.Entity
{
    public partial class FriendEntity : BaseEntity
    {
        [ObservableProperty] private Guid _userId;
        [ObservableProperty] private Guid _friendId;
    }
}

