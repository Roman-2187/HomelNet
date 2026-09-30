using System;
using HomeNet.DI;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;

namespace HomeNetAvalonia.Infrastructure
{
    public class ComponentHub
    {
        public ComponentHub()
        {
        }

        // 🔥 ТЕПЕРЬ ВСЕ СВОЙСТВА ЗАЩИЩЕНЫ: Если контейнер еще не собран, приложение НЕ упадет!
        public MainViewModel MainViewModel => GetViewModelSafe<MainViewModel>();
        public TitleBarViewModel TitleBarViewModel => GetViewModelSafe<TitleBarViewModel>();
        public StatusBarViewModel StatusBarViewModel => GetViewModelSafe<StatusBarViewModel>();

        public AuthenticationViewModel AuthenticationViewModel => GetViewModelSafe<AuthenticationViewModel>();
        public RegistrationViewModel RegistrationViewModel => GetViewModelSafe<RegistrationViewModel>();
        public AdminViewModel AdminViewModel => GetViewModelSafe<AdminViewModel>();
        public UserViewModel UserProfileViewModel => GetViewModelSafe<UserViewModel>();

        public TableUsersViewModel TableUsersViewModel => GetViewModelSafe<TableUsersViewModel>();
        public DeleteUsersViewModel DeleteUsersViewModel => GetViewModelSafe<DeleteUsersViewModel>();
        public TerminalLogsViewModel TerminalLogsViewModel => GetViewModelSafe<TerminalLogsViewModel>();
        public InspectorViewModel InspectorViewModel => GetViewModelSafe<InspectorViewModel>();

        /// <summary>
        /// Безопасный перехватчик. Если Авалония дергает свойства до вызова Build контейнера,
        /// метод не выкинет Exception и не уронит приложение, а просто вернет null.
        /// </summary>
        private T GetViewModelSafe<T>() where T : class
        {
            try
            {
                // Проверяем твой статический провайдер ядра
                return AppBootstrapper.GetViewModel<T>();
            }
            catch
            {
                // Полная тишина при ранней инициализации XAML
                return null!;
            }
        }
    }
}
