using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sereno.Desktop.Controles
{
    /// <summary>Luna + "Sereno". Se repite arriba de cada ventana de acceso.</summary>
    public sealed class Marca : StackPanel
    {
        public Marca()
        {
            Orientation = Orientation.Horizontal;
            Margin = new Thickness(0, 0, 0, 22);

            var luna = new Path
            {
                Width = 22,
                Height = 22,
                Stretch = Stretch.Uniform,
                Data = Application.Current.TryFindResource("GeoLuna") as Geometry,
            };
            luna.SetResourceReference(Shape.FillProperty, "Acento");

            var nombre = new TextBlock
            {
                Text = "Sereno",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            nombre.SetResourceReference(TextBlock.FontFamilyProperty, "FuenteTitulo");

            Children.Add(luna);
            Children.Add(nombre);
        }
    }
}
