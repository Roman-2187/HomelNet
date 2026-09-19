using HomeNetCore.Enums.Navigation;
using System.ComponentModel;

namespace HomeNetCore.Interfaces.ViewModels
{
    
    public interface IMainViewModel : INotifyPropertyChanged
    {
        // Наш тумблер видимости логов, из-за которого сыпалась ошибка
        bool IsGlobalLoggerVisible { get; set; }

       


        public record CloseRequest();
        public record ToggleSize();
        public record MacroNavigation(MainTab TargetTab);
        public record ZoneChanged(MainTab TargetTab);
    }
}
