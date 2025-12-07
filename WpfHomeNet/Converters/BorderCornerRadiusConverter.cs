using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfHomeNet.Converters
{
    public class BorderCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. Проверяем входное значение
            if (!(value is double width) || double.IsNaN(width) || width <= 0)
                return new CornerRadius(4); // Минимальный радиус


            // 2. Парсим параметр (ConverterParameter)
            double factor = 0.08; // Дефолтный коэффициент
            if (parameter is string param)
            {
                // Используем инвариантный формат (точка как разделитель)
                if (double.TryParse(param, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedFactor))
                    factor = parsedFactor;
            }
            else if (parameter is double d)
            {
                factor = d;
            }

            // 3. Вычисляем радиус
            double radius = Math.Max(4, width * factor);
            return new CornerRadius(radius);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


    


}
