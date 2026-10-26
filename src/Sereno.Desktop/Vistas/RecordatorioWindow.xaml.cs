using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Sereno.Desktop.Servicios;

namespace Sereno.Desktop.Vistas
{
    /// <summary>
    /// Recuerda, cada 5 minutos, cuánto falta para terminar el bloque. Aparece en la esquina, no
    /// toma el foco y se cierra sola a los pocos segundos. Con animar en false (bajo estímulo,
    /// animaciones de Windows desactivadas o alto contraste activo) no tiene fundido.
    /// </summary>
    public partial class RecordatorioWindow : Window
    {
        private const int MargenPantalla = 16;
        private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(6);
        private static readonly TimeSpan DuracionFundido = TimeSpan.FromMilliseconds(180);

        private readonly bool _animar;

        public RecordatorioWindow(int minutosRestantes, bool animar)
        {
            InitializeComponent();
            _animar = animar;
            Opacity = animar ? 0 : 1;

            TextoRestante.Text = minutosRestantes == 1
                ? "Queda 1 minuto para terminar el bloque."
                : $"Quedan {minutosRestantes} minutos para terminar el bloque.";
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

            Accesibilidad.Anunciar(TextoRestante);
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
