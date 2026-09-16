using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sereno.Controles
{
    /// <summary>Candado + texto al pie de las ventanas de acceso.</summary>
    public sealed class AvisoPrivacidad : Border
    {
        private readonly TextBlock _texto;

        public AvisoPrivacidad()
        {
            BorderThickness = new Thickness(0, 1, 0, 0);
            SetResourceReference(BorderBrushProperty, "Separador");
            Margin = new Thickness(0, 18, 0, 0);
            Padding = new Thickness(0, 14, 0, 0);

            var candado = new Path
            {
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                StrokeThickness = 2,
                Margin = new Thickness(0, 2, 10, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Data = Application.Current.TryFindResource("GeoCandado") as Geometry,
            };
            candado.SetResourceReference(Shape.StrokeProperty, "TextoSecundario");

            _texto = new TextBlock { FontSize = 13, TextWrapping = TextWrapping.Wrap };
            _texto.SetResourceReference(TextBlock.ForegroundProperty, "TextoSecundario");

            var fila = new DockPanel();
            DockPanel.SetDock(candado, Dock.Left);
            fila.Children.Add(candado);
            fila.Children.Add(_texto);
            Child = fila;
        }

        public string Texto
        {
            get => _texto.Text;
            set => _texto.Text = value;
        }
    }
}
