using WpfHomeNet.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace HomeSocialNetwork.Controls
{
    /// <summary>
    /// Interaction logic for UsersTableView.xaml
    /// </summary>
    public partial class UsersTableView : UserControl
    {
        private MainViewModel? _viewModel;

        public UsersTableView()
        {
            InitializeComponent();

            // 1. Проверяем DataContext сразу
            if (DataContext is MainViewModel vm)
            {
                _viewModel = vm;
            }
            else
            {
                // 2. Если не получилось — ждём события Loaded
                Loaded += UsersTableView_Loaded;
            }
        }

        private void UsersTableView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel vm)
                {
                    _viewModel = vm;
                    // Отписываемся только если успешно обработали
                    Loaded -= UsersTableView_Loaded;
                }
                else
                {
                    Debug.WriteLine($"DataContext имеет тип: {DataContext?.GetType().Name}, ожидается MainViewModel");
                    throw new InvalidOperationException("DataContext не установлен или имеет неверный тип!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в UsersTableView_Loaded: {ex.Message}");
                // Не отписываемся, чтобы попытаться снова при следующем Loaded
            }
        }

     
    }
}
