namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IAdminMenuViewModel
    {
      
      
        // Сигнал: Админ хочет переключиться на панель пользователей
        public record UserTableRequested : IAdminMenuViewModel;
    }
}
