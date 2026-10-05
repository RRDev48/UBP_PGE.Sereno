using System.Windows;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Factor de interlineado compartido por todos los textos. Es un único objeto en los recursos:
    /// cambiar Factor actualiza los textos que ya están en pantalla.
    /// </summary>
    public sealed class EspaciadoVisual : DependencyObject
    {
        public static readonly DependencyProperty FactorProperty = DependencyProperty.Register(
            nameof(Factor), typeof(double), typeof(EspaciadoVisual), new PropertyMetadata(0.0));

        public double Factor
        {
            get => (double)GetValue(FactorProperty);
            set => SetValue(FactorProperty, value);
        }
    }
}
