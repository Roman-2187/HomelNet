using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace HomeNetCore.Interfaces.ViewModels.MainZone
{
    
        public interface IMenuViewModel
        {
            bool IsMenuOpen { get; set; }
            IRelayCommand LogoutCommand { get; } // 🔥 Вот так
        }
    
}