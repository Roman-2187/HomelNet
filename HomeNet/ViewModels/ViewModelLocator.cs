using HomeNetPresentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace WpfHomeNet.ViewModels
{
    public class ViewModelLocator
    {
        // Вытаскиваем живой провайдер сервисов прямо из нашего класса App! 🧼
        private static IServiceProvider Services => ((App)Application.Current).Services;

        // Чистые нано-провода для XAML! Напрямую из DI-контейнера! 🦾🛸




        public MainViewModel MainVm => Services.GetRequiredService<MainViewModel>();
        public RegistrationViewModel RegistrationVm => Services.GetRequiredService<RegistrationViewModel>();
        public AuthenticationViewModel LoginVm => Services.GetRequiredService<AuthenticationViewModel>();
        public AdminMenuViewModel AdminMenuVm => Services.GetRequiredService<AdminMenuViewModel>();
        public DeleteUsersViewModel DeleteUsersVm => Services.GetRequiredService<DeleteUsersViewModel>();
        public StatusBarViewModel StatusBarVm => Services.GetRequiredService<StatusBarViewModel>();
        // Прописываем мост для таблицы в Локаторе 🚀
        public UsersTableViewModel UsersTable => Services.GetRequiredService<UsersTableViewModel>();
        // Прописываем мост для личного кабинета в Локаторе 🚀
        public UserDashboardViewModel UserDashboardVm => Services.GetRequiredService<UserDashboardViewModel>();

        // Прописываем мост для вложенного чата в Локаторе 🚀
        

        public ChatViewModel ChatVm => Services.GetRequiredService<ChatViewModel>();


    }
}

