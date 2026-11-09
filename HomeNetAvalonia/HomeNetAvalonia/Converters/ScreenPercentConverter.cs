using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HomeNetAvalonia.Converters
{
    public class ScreenPercentConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // value — это размер (ширина или высота) экрана/окна
            if (value is double actualSize && parameter is string percentStr && double.TryParse(percentStr, out double percent))
            {
                return actualSize * (percent / 100.0);
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
