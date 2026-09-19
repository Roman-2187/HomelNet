namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IAdminMenuViewModel
    {
        public record VisibilityChanged(bool IsVisible);
        public record ToggleAnimation(IMainViewModel MainViewModel);
    }
}
