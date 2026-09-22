using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using Sereno.Desktop.Servicios;
using Sereno.Platform.Storage;

namespace Sereno.Desktop.Vistas
{
    /// <summary>
    /// Pantalla de carga. Lee los perfiles en segundo plano y avisa con CargaCompleta.
    /// Dura menos de 2 segundos; sin animaciones si Windows las tiene desactivadas.
    /// </summary>
    public partial class SplashWindow : Window
    {
        private readonly AlmacenPerfiles _almacen;

        public event EventHandler? CargaCompleta;

        public SplashWindow(AlmacenPerfiles almacen)
        {
            InitializeComponent();
            _almacen = almacen;

            Version? version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionTexto.Text = version is null ? string.Empty : $"v{version.Major}.{version.Minor}.{version.Build}";

            Loaded += SplashWindow_Loaded;
        }

        private async void SplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            bool animar = TemaService.AnimacionesActivas;
            Task carga = Task.Run(() => _almacen.Cargar());

            if (animar)
            {
                var avance = new DoubleAnimation(0, 90, TimeSpan.FromMilliseconds(1100))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                };
                Progreso.BeginAnimation(RangeBase.ValueProperty, avance);

                var respiracion = new DoubleAnimation(0.28, 0.12, TimeSpan.FromMilliseconds(1200))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                };
                Halo.BeginAnimation(OpacityProperty, respiracion);
            }

            CambiarEstado("Preparando tus pausas…");
            await Task.Delay(animar ? 500 : 0);
            CambiarEstado("Cargando tu configuración…");

            try
            {
                await Task.WhenAll(carga, Task.Delay(animar ? 700 : 100));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MostrarFalla();
                return;
            }

            Progreso.BeginAnimation(RangeBase.ValueProperty, null);
            Progreso.Value = 100;
            Halo.BeginAnimation(OpacityProperty, null);
            CambiarEstado("Listo");

            await Task.Delay(animar ? 200 : 0);
            CargaCompleta?.Invoke(this, EventArgs.Empty);
        }

        private void MostrarFalla()
        {
            Progreso.BeginAnimation(RangeBase.ValueProperty, null);
            Halo.BeginAnimation(OpacityProperty, null);
            Progreso.Visibility = Visibility.Collapsed;
            CambiarEstado("No se pudo abrir la carpeta de Sereno. Revisá los permisos de tu usuario.");
            Cerrar.Visibility = Visibility.Visible;
            Cerrar.Focus();
        }

        private void CambiarEstado(string texto)
        {
            EstadoTexto.Text = texto;
            Accesibilidad.Anunciar(EstadoTexto);
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}
