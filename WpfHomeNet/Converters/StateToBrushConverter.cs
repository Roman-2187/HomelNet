using HomeNetCore.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfHomeNet.Converters
{
    public class StateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ValidationState state ? state switch
            {
                ValidationState.None => Brushes.DarkBlue,
                ValidationState.Success => Brushes.DarkGreen,             
                ValidationState.Error => Brushes.Red,
                 ValidationState.Info => Brushes.DarkSlateGray,             
            _ => Brushes.Gray
            } : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

}
