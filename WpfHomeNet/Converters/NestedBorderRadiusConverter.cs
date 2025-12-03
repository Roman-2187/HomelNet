using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfHomeNet.Converters
{
    public class NestedBorderRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double actualWidth || parameter is not string paramStr)
                return 0.0;

            var parts = paramStr.Split(',');
            if (parts.Length != 3)
                return 0.0;

            double baseFactor = double.Parse(parts[0]);
            double outerFactor = double.Parse(parts[1]);
            double innerFactor = double.Parse(parts[2]);

            double baseRadius = actualWidth * baseFactor;

            return new CornerRadius(
                topLeft: baseRadius * outerFactor,
                topRight: baseRadius * outerFactor,
                bottomLeft: baseRadius * innerFactor,
                bottomRight: baseRadius * innerFactor
            );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }






}






