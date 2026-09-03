using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfHomeNet.Converters
{
    // Наш умный конвертер, который пересчитывает матрешку углов! 🧙‍♂️
    public class CornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CornerRadius baseRadius && parameter is string offsetParam)
            {
                // Считываем коэффициент смещения из параметра XAML (например, "-0.6")
                if (double.TryParse(offsetParam, NumberStyles.Any, CultureInfo.InvariantCulture, out double offset))
                {
                    // Пропорционально меняем все 4 угла (Слева-Вверху, Справа-Вверху, Справа-Внизу, Слева-Внизу)
                    double topLeft = Math.Max(0, baseRadius.TopLeft + offset);
                    double topRight = Math.Max(0, baseRadius.TopRight + offset);
                    double bottomRight = Math.Max(0, baseRadius.BottomRight + offset);
                    double bottomLeft = Math.Max(0, baseRadius.BottomLeft + offset);

                    return new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
                }
            }

            return value; // Если что-то пошло не так — возвращаем исходный радиус
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Нам не нужно конвертировать обратно
        }
    }
}
