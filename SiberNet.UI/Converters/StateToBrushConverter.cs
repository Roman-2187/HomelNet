using HomeNetCore.Enums;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;



namespace SiberNet.UI.Converters
{
    public class StateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ValidationState state ? state switch
            {
                // Обычное состояние (статичное) — теперь неоновый Cyan #00F0FF
                ValidationState.None => new SolidColorBrush(Color.FromRgb(0x00, 0xF0, 0xFF)),
                ValidationState.Info => new SolidColorBrush(Color.FromRgb(0x00, 0xF0, 0xFF)),

                // Успешная валидация — сочный неоновый зеленый #00FF66
                ValidationState.Success => new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x66)),

                // Ошибка — статичный плотный малиновый #FFF23E5C
                ValidationState.Error => new SolidColorBrush(Color.FromRgb(0xF2, 0x3E, 0x5C)),

                _ => Brushes.Gray
            } : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}


