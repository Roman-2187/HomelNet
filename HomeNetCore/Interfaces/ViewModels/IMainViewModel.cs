using System.ComponentModel;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IMainViewModel : INotifyPropertyChanged
    {
       
        // Системные приказы для MainWindow.xaml.cs
        public record CloseRequest();
        public record ToggleSize();
    }
}
