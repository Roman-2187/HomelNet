using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SiberNet.UI.Converters
{
    public class ChamferGeometryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || !(values[0] is double) || !(values[1] is double))
                return Geometry.Empty;

            double width = (double)values[0];
            double height = (double)values[1];

            if (width <= 0 || height <= 0)
                return Geometry.Empty;

            // 👑 ДИНАМИЧЕСКИЙ РАЗМЕР ФАСКИ: читаем из третьего параметра (Binding к Tag)
            double baseChamfer = 8.0;
            if (values.Length > 2 && values[2] != null && double.TryParse(values[2].ToString(), out double t) && t > 0)
            {
                baseChamfer = t;
            }

            // Читаем чистый отступ из ConverterParameter
            double offset = 0.0;
            if (parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double p))
            {
                offset = p;
            }

            // Математика идеального параллельного переноса 45 градусов
            double c = baseChamfer - (offset * (Math.Sqrt(2) - 1.0));
            if (c < 0) c = 0;

            double minX = offset;
            double minY = offset;
            double maxX = width - offset;
            double maxY = height - offset;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(minX + c, minY), true, true);

                // 1. Верхний правый угол
                ctx.LineTo(new Point(maxX - c, minY), true, false);
                ctx.LineTo(new Point(maxX, minY + c), true, false);

                // 2. Нижний правый угол
                ctx.LineTo(new Point(maxX, maxY - c), true, false);
                ctx.LineTo(new Point(maxX - c, maxY), true, false);

                // 3. Нижний левый угол
                ctx.LineTo(new Point(minX + c, maxY), true, false);
                ctx.LineTo(new Point(minX, maxY - c), true, false);

                // 4. Левый верхний угол
                ctx.LineTo(new Point(minX, minY + c), true, false);
            }
            geometry.Freeze();
            return geometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
