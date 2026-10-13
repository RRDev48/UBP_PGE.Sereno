using System;
using System.Windows;
using System.Windows.Threading;
using Sereno.Desktop.Servicios;

namespace Sereno.Desktop.Vistas
{
    /// <summary>
    /// Confirmación breve sin acciones: aparece en la esquina, no toma el foco y se cierra sola.
    /// Se usa para confirmar que Sereno inició la sesión.
    /// </summary>
    public partial class AvisoBreveWindow : Window
    {
        private const int MargenPantalla = 16;
        private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(5);

        public AvisoBreveWindow(string titulo, string cuerpo)
        {
            InitializeComponent();
            TituloTexto.Text = titulo;
            CuerpoTexto.Text = cuerpo;
            VentanaFlotante.Aplicar(this);

            var cierre = new DispatcherTimer { Interval = Duracion };
            cierre.Tick += (_, _) =>
            {
                cierre.Stop();
                Close();
            };
            cierre.Start();
        }

        private void Ventana_Loaded(object sender, RoutedEventArgs e)
        {
            Rect area = SystemParameters.WorkArea;
            Left = area.Right - ActualWidth - MargenPantalla;
            Top = area.Bottom - ActualHeight - MargenPantalla;
            Accesibilidad.Anunciar(TituloTexto);
        }
    }
}
