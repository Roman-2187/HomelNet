
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfHomeNet.Converters
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. Проверяем, что значение — bool
            if (value is not bool boolValue)
                return Visibility.Collapsed; // Дефолт, если не bool


            // 2. Инвертируем значение
            bool inverted = !boolValue;

            // 3. Возвращаем Visibility
            return inverted ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility != Visibility.Visible;
            return false;
        }
    }

}






