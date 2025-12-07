using System.Globalization;
using System.Windows.Data;

namespace WpfHomeNet.Converters
{
    public class HeightBattonCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double height = System.Convert.ToDouble(value);

            // Получаем процент из параметра (по умолчанию 25%)
            double percent = 0.25; // default
            if (parameter is string paramStr && double.TryParse(paramStr, out double parsedPercent))
            {
                percent = parsedPercent / 100.0; // преобразуем проценты в долю
            }

            // Зона 1: высота ≤ 60 px → радиус = percent% от высоты, но не менее 4 px
            if (height <= 60)
            {
                double radius = height * percent;
                return Math.Max(radius, 4.0); // минимум 4 px
            }

            // Зона 2: высота > 60 px → фиксированный максимум 12 px
            else
            {
                return 8.0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}






