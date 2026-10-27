using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Sereno.Desktop.Servicios;

namespace Sereno.Desktop.Vistas
{
    /// <summary>
    /// Confirmación breve sin acciones: aparece en la esquina, no toma el foco y se cierra sola.
    /// Se usa para confirmar que Sereno inició la sesión. Con animar en true, Lucero respira
    /// mientras está visible y la ventana se despide con un fundido, en vez de desaparecer de golpe.
    /// </summary>
    public partial class AvisoBreveWindow : Window
    {
        private const int MargenPantalla = 16;
        private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DuracionFundido = TimeSpan.FromMilliseconds(180);

        private readonly bool _animar;

        public AvisoBreveWindow(string titulo, string cuerpo, bool animar)
        {
            InitializeComponent();
            _animar = animar;
            Opacity = animar ? 0 : 1;

            TituloTexto.Text = titulo;
            CuerpoTexto.Text = cuerpo;
            MascotaIcono.Animar = animar;
            VentanaFlotante.Aplicar(this);

            var cierre = new DispatcherTimer { Interval = Duracion };
            cierre.Tick += (_, _) =>
            {
                cierre.Stop();
                CerrarConFundido();
            };
            cierre.Start();
        }

        private void Ventana_Loaded(object sender, RoutedEventArgs e)
        {
            Rect area = SystemParameters.WorkArea;
            Left = area.Right - ActualWidth - MargenPantalla;
            Top = area.Bottom - ActualHeight - MargenPantalla;

            if (_animar)
                BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, DuracionFundido));

            Accesibilidad.Anunciar(TituloTexto);
        }

        private void CerrarConFundido()
        {
            if (!_animar)
            {
                Close();
                return;
            }

            var desaparicion = new DoubleAnimation(Opacity, 0, DuracionFundido);
            desaparicion.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, desaparicion);
        }
    }
}
