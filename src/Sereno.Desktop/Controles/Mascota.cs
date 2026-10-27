using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Sereno.Desktop.Controles
{
    public enum ReaccionMascota
    {
        /// <summary>Volvemos a verla en un rato: se atenúa, no desaparece de golpe.</summary>
        Pospuesta,

        /// <summary>Se cierra: se achica rápido, como apagándose.</summary>
        Descartada,

        /// <summary>Arranca la pausa: un pulso breve hacia arriba.</summary>
        PausaIniciada,
    }

    /// <summary>
    /// Lucero: la chispa que acompaña los avisos de Sereno. No es un personaje con cara, en la
    /// línea de los indicadores de otros asistentes. El halo cambia de tono según la hora del día
    /// (más pálido de día, más cálido de noche) y respira suavemente con Animar en true; en false
    /// (bajo estímulo, animaciones de Windows desactivadas o alto contraste) queda quieta.
    /// </summary>
    public sealed class Mascota : Canvas
    {
        private readonly Path _chispa;
        private readonly ScaleTransform _escala = new(1, 1);

        public Mascota()
        {
            Width = 48;
            Height = 48;

            var halo = new Ellipse { Width = 48, Height = 48 };
            var brillo = new RadialGradientBrush();
            Color tono = ColorDelHalo(DateTime.Now.TimeOfDay);
            brillo.GradientStops.Add(new GradientStop(Color.FromArgb(120, tono.R, tono.G, tono.B), 0));
            brillo.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
            halo.Fill = brillo;
            SetLeft(halo, 0);
            SetTop(halo, 0);
            Children.Add(halo);

            _chispa = new Path
            {
                Data = Geometry.Parse("M24,4 C26,16 32,22 44,24 C32,26 26,32 24,44 C22,32 16,26 4,24 C16,22 22,16 24,4 Z"),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = _escala,
            };
            _chispa.SetResourceReference(Shape.FillProperty, "Acento");
            Children.Add(_chispa);
        }

        /// <summary>Activa o detiene la respiración suave de la chispa.</summary>
        public bool Animar
        {
            set
            {
                if (!value)
                {
                    _escala.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    _escala.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    _escala.ScaleX = 1;
                    _escala.ScaleY = 1;
                    return;
                }

                var pulso = new DoubleAnimation(0.88, 1.1, TimeSpan.FromSeconds(1.3))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                };
                _escala.BeginAnimation(ScaleTransform.ScaleXProperty, pulso);
                _escala.BeginAnimation(ScaleTransform.ScaleYProperty, pulso);
            }
        }

        /// <summary>Reacción breve antes de cerrarse. Llamar solo cuando hay animaciones activas.</summary>
        public void Reaccionar(ReaccionMascota reaccion)
        {
            var duracion = TimeSpan.FromMilliseconds(200);
            switch (reaccion)
            {
                case ReaccionMascota.Pospuesta:
                    _chispa.BeginAnimation(OpacityProperty, new DoubleAnimation(0.5, duracion));
                    break;
                case ReaccionMascota.Descartada:
                    _escala.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.55, duracion));
                    _escala.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.55, duracion));
                    break;
                case ReaccionMascota.PausaIniciada:
                    _escala.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.3, duracion));
                    _escala.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.3, duracion));
                    break;
            }
        }

        /// <summary>Más pálida al mediodía, más cálida a medianoche; transición continua entre las dos.</summary>
        private static Color ColorDelHalo(TimeSpan horaDelDia)
        {
            double angulo = horaDelDia.TotalHours / 24.0 * 2 * Math.PI;
            double calidez = (1 + Math.Cos(angulo)) / 2; // 1 a medianoche, 0 al mediodía

            var dia = Color.FromRgb(0xF3, 0xDC, 0xAE);
            var noche = Color.FromRgb(0xF0, 0xB8, 0x5A);
            return Color.FromRgb(
                Interpolar(dia.R, noche.R, calidez),
                Interpolar(dia.G, noche.G, calidez),
                Interpolar(dia.B, noche.B, calidez));
        }

        private static byte Interpolar(byte desde, byte hasta, double t) =>
            (byte)(desde + (hasta - desde) * t);
    }
}
