using HomeNetCore.Enums.Navigation;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface ITitleBarViewModel
    {
        // 📢 Сигнал запроса макро-навигации (например, отмена из AuthViewModel)
        public record MacroNavigation(MainTab TargetTab);

        // 📢 Сигнал точечной смены саб-таба (например, вход/регистрация)
        public record ClientTabChanged(ClientSubTab TargetSubTab);

        // 📢 Ответный сигнал: Навигатор переключил зону, шапка — обновляй UI!
        public record ZoneChanged(MainTab TargetTab, ClientSubTab ClientTab);
 
        public record ToggleAnimation(bool IsVisible);
    }
}
