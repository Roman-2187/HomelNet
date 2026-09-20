namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IAdminMenuViewModel
    {
        public record VisibilityChanged(bool IsVisible);
        public record ToggleAnimation(IMainViewModel MainViewModel);

        // Сигнал: Админ хочет переключиться на панель пользователей
        public record UserTableRequested : IAdminMenuViewModel;
    }
}
