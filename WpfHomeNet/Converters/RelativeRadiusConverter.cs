using System.Globalization;
using System.Windows.Data;

namespace WpfHomeNet.Converters
{
    public class RelativeRadiusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 1 || values[0] is not double baseRadius)
                return 0.0;

            double factor = 1.0; // множитель по умолчанию
            if (parameter is string paramStr && double.TryParse(paramStr, out double parsedFactor))
                factor = parsedFactor;


            return baseRadius * factor;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }






}






