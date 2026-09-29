using HomeNetCore.Enums.Navigation;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IWidescreenService
    {
        // 🔥 ОДИН ТУПОЙ СНАРЯД С БУЛОМ: true = шире, false = обратно!
        public record ToggleWidescreen(bool IsVisible);
    }
}
