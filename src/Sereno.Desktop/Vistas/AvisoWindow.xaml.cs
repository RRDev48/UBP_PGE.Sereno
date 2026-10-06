using System;
using System.Windows;
using System.Windows.Threading;
using Sereno.Core.History;
using Sereno.Desktop.Servicios;

namespace Sereno.Desktop.Vistas
{
    /// <summary>
    /// Aviso al terminar un bloque. Aparece en la esquina inferior derecha, no toma el foco, no
    /// figura en la barra de tareas ni en Alt+Tab, y se cierra sola a los 10 segundos si nadie
    /// actúa. Se opera con clic o con los atajos globales de Ctrl+Alt+P y Ctrl+Alt+D.
    /// </summary>
    public partial class AvisoWindow : Window
    {
        private const int MargenPantalla = 16;
        private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(10);

        private readonly Action<AccionAviso> _alAccion;
        private bool _resuelto;

        public AvisoWindow(int minutos, Action<AccionAviso> alAccion)
        {
            InitializeComponent();
            _alAccion = alAccion;
            TituloTexto.Text = "Bloque terminado";
            CuerpoTexto.Text = $"Tomá una pausa. Completaste {minutos} minutos de foco.";
            VentanaFlotante.Aplicar(this);
            _ = new AtajosGlobales(
                this,
                () => Resolver(AccionAviso.Pospuesto),
                () => Resolver(AccionAviso.Descartado));

            var cierre = new DispatcherTimer { Interval = Duracion };
            cierre.Tick += (_, _) =>
            {
                cierre.Stop();
                Close();
            };
            cierre.Start();
        }

        private void Posponer_Click(object sender, RoutedEventArgs e) => Resolver(AccionAviso.Pospuesto);

        private void Descartar_Click(object sender, RoutedEventArgs e) => Resolver(AccionAviso.Descartado);

        private void Ventana_Loaded(object sender, RoutedEventArgs e)
        {
            Rect area = SystemParameters.WorkArea;
            Left = area.Right - ActualWidth - MargenPantalla;
            Top = area.Bottom - ActualHeight - MargenPantalla;
            Accesibilidad.Anunciar(TituloTexto);
        }

        private void Resolver(AccionAviso accion)
        {
            if (_resuelto) return;
            _resuelto = true;
            _alAccion(accion);
            Close();
        }
    }
}
