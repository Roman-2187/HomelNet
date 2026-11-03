using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNet.Models.Shared.Entity
{
    public partial class FriendEntity : BaseEntity
    {
        [ObservableProperty] private Guid _userId;
        [ObservableProperty] private Guid _friendId;
    }
}

