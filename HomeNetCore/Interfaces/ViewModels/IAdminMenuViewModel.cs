using HomeNetCore.Interfaces.Events;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IAdminMenuViewModel
    {
      
      
        // Сигнал: Админ хочет переключиться на панель пользователей
        public record UserTableRequested : IAdminMenuViewModel;

        // Сигнал-приказ: "Сделай отчёт и покажи на экране!"
        public record ReportGenerationRequested ;

        // 📢 Сигнал: "Админ открыл форму удаления, приготовиться!"
        public record DeleteFormRequested;

        public record DeleteFormCloseRequested;
    }
}
