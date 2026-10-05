using System;
using System.Globalization;
using System.Windows.Data;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Calcula el alto de línea de un texto como su tamaño de letra por el factor de interlineado.
    /// Con factor 0 (espaciado desactivado) devuelve NaN, que WPF interpreta como alto automático.
    /// </summary>
    public sealed class InterlineadoConvertidor : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double tamano && values[1] is double factor && factor > 0)
                return tamano * factor;
            return double.NaN;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
