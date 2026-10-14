using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.AdminViews;
using System;

namespace HomeNet.DI
{
    /// <summary>
    /// XAML-локатор вьюмоделей приложения. 
    /// Связывает разметку интерфейса Avalonia/WPF с нашим ручным контейнером Pure DI.
    /// </summary>
    public class ComponentHub
    {
        // Пустой конструктор обязателен, чтобы XAML-движок мог создать этот класс в ресурсах
        public ComponentHub() { }

        // Каждое свойство безопасно лезет в наш новый модульный корень
        public MainViewModel MainViewModel => GetViewModelSafe<MainViewModel>();
        public TitleBarViewModel TitleBarViewModel => GetViewModelSafe<TitleBarViewModel>();
        public SystemButtonsViewModel SystemButtonsViewModel => GetViewModelSafe<SystemButtonsViewModel>();
        public StatusBarViewModel StatusBarViewModel => GetViewModelSafe<StatusBarViewModel>();

        public AuthenticationViewModel AuthenticationViewModel => GetViewModelSafe<AuthenticationViewModel>();
        public RegistrationViewModel RegistrationViewModel => GetViewModelSafe<RegistrationViewModel>();
        public AdminViewModel AdminViewModel => GetViewModelSafe<AdminViewModel>();
        // Было: public UserViewModel UserProfileViewModel => GetViewModelSafe<UserViewModel>();
        // Стало:
        public UserViewModel UserViewModel => GetViewModelSafe<UserViewModel>();

        // 🔥 ВОТ ЭТОГО НЕ ХВАТАЛО! Добавляем модель списка контактов в локатор
        public ContactsViewModel ContactsViewModel => GetViewModelSafe<ContactsViewModel>();

        public ChatViewModel ChatViewModel => GetViewModelSafe<ChatViewModel>();


        public TableUsersViewModel TableUsersViewModel => GetViewModelSafe<TableUsersViewModel>();
        public DeleteUsersViewModel DeleteUsersViewModel => GetViewModelSafe<DeleteUsersViewModel>();
        public TerminalLogsViewModel TerminalLogsViewModel => GetViewModelSafe<TerminalLogsViewModel>();
        public InspectorViewModel InspectorViewModel => GetViewModelSafe<InspectorViewModel>();
        public SeedUsersViewModel SeedUsersViewModel => GetViewModelSafe<SeedUsersViewModel>();

        private T GetViewModelSafe<T>() where T : class
        {
            try
            {
                // Перенаправляем запрос в наш новый ручной Composition Root
                return AppCompositionRoot.Instance.GetViewModel<T>();
            }
            catch
            {
                // 🔥 СПАСЕНИЕ ДИЗАЙНЕРА: Если мы открыли окно в редакторе Visual Studio,
                // приложение еще не запущено, и Instance выбросит исключение.
                // Возвращаем null!, чтобы превью в Студии/Rider не падало и отображало верстку.
                return null!;
            }
        }
    }
}
