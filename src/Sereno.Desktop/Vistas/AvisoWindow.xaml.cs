using System;
using System.Windows;
using System.Windows.Threading;
using Sereno.Desktop.Servicios;

namespace Sereno.Desktop.Vistas
{
    /// <summary>
    /// Aviso al terminar un bloque. Aparece en la esquina inferior derecha, no toma el foco, no
    /// figura en la barra de tareas ni en Alt+Tab, y se cierra solo a los 10 segundos.
    /// </summary>
    public partial class AvisoWindow : Window
    {
        private const int MargenPantalla = 16;
        private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(10);

        public AvisoWindow(int minutos)
        {
            InitializeComponent();
            TituloTexto.Text = "Bloque terminado";
            CuerpoTexto.Text = $"Tomá una pausa. Completaste {minutos} minutos de foco.";
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
