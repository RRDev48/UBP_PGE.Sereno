using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sereno.Desktop.Controles
{
    /// <summary>Luciérnaga: la mascota que recuerda cuánto falta para terminar el bloque.</summary>
    public sealed class Mascota : Canvas
    {
        public Mascota()
        {
            Width = 48;
            Height = 48;

            var halo = new Ellipse { Width = 48, Height = 48 };
            var brillo = new RadialGradientBrush();
            brillo.GradientStops.Add(new GradientStop(Color.FromArgb(140, 240, 184, 90), 0));
            brillo.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
            halo.Fill = brillo;
            SetLeft(halo, 0);
            SetTop(halo, 0);
            Children.Add(halo);

            Children.Add(CrearAla(izquierda: true));
            Children.Add(CrearAla(izquierda: false));

            var cuerpo = new Ellipse { Width = 26, Height = 22 };
            cuerpo.SetResourceReference(Shape.FillProperty, "Acento");
            SetLeft(cuerpo, 11);
            SetTop(cuerpo, 13);
            Children.Add(cuerpo);

            Children.Add(CrearOjo(izquierdo: true));
            Children.Add(CrearOjo(izquierdo: false));

            var sonrisa = new Path
            {
                Data = Geometry.Parse("M 18,24 Q 24,27 30,24"),
                StrokeThickness = 1.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            };
            sonrisa.SetResourceReference(Shape.StrokeProperty, "TextoPrimario");
            Children.Add(sonrisa);
        }

        private static Ellipse CrearAla(bool izquierda)
        {
            var ala = new Ellipse
            {
                Width = 16,
                Height = 10,
                Opacity = 0.4,
                RenderTransform = new RotateTransform(izquierda ? -20 : 20),
            };
            ala.SetResourceReference(Shape.FillProperty, "TextoSecundario");
            SetLeft(ala, izquierda ? 2 : 30);
            SetTop(ala, 10);
            return ala;
        }

        private static Ellipse CrearOjo(bool izquierdo)
        {
            var ojo = new Ellipse { Width = 3, Height = 3 };
            ojo.SetResourceReference(Shape.FillProperty, "TextoPrimario");
            SetLeft(ojo, izquierdo ? 17 : 25);
            SetTop(ojo, 19);
            return ojo;
        }
    }
}
