using HomeNetCore.Enums.Navigation;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface ITitleBarVm
    {
        // 🔥 ВОЗВРАЩАЕМ ДВА ПАРАМЕТРА: Полная макро-картина для шапки и триггеров XAML!
        public record MacroZoneChanged(MainTab ActiveZone, ClientSubTab ActiveClientTab);

        public record ToggleAnimation(bool IsVisible);
    }
}
