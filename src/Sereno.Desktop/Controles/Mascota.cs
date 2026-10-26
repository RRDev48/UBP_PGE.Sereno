using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Sereno.Desktop.Controles
{
    /// <summary>
    /// Destello: la mascota que recuerda cuánto falta para terminar el bloque. Una chispa simple,
    /// no un personaje con cara, en la línea de los indicadores de otros asistentes. Con Animar en
    /// true respira suavemente; en false (bajo estímulo, animaciones de Windows desactivadas o
    /// alto contraste) queda quieta.
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
            brillo.GradientStops.Add(new GradientStop(Color.FromArgb(120, 240, 184, 90), 0));
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
    }
}
