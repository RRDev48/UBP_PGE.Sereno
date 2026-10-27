using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Sereno.Core.History;
using Sereno.Desktop.Controles;
using Sereno.Desktop.Servicios;

namespace Sereno.Desktop.Vistas
{
    /// <summary>
    /// Aviso al terminar un bloque. Aparece en la esquina inferior derecha, no toma el foco, no
    /// figura en la barra de tareas ni en Alt+Tab, y se cierra sola pasado el tiempo configurado si nadie
    /// actúa. Se opera con clic o con los atajos globales de Ctrl+Alt+P y Ctrl+Alt+D.
    /// Con animar en false (modo bajo estímulo, animaciones de Windows desactivadas o alto
    /// contraste activo), aparece y desaparece sin transición.
    /// </summary>
    public partial class AvisoWindow : Window
    {
        private const int MargenPantalla = 16;
        private static readonly TimeSpan DuracionFundido = TimeSpan.FromMilliseconds(180);

        private readonly Action<AccionAviso> _alAccion;
        private readonly bool _animar;
        private readonly DispatcherTimer _cierreAutomatico;
        private bool _resuelto;

        public AvisoWindow(int minutos, int segundos, bool animar, Action<AccionAviso> alAccion)
        {
            InitializeComponent();
            _alAccion = alAccion;
            _animar = animar;
            Opacity = animar ? 0 : 1;

            TituloTexto.Text = "Bloque terminado";
            CuerpoTexto.Text = $"Tomá una pausa. Completaste {minutos} minutos de foco.";
            MascotaIcono.Animar = animar;
            VentanaFlotante.Aplicar(this);
            _ = new AtajosGlobales(
                this,
                () => Resolver(AccionAviso.Pospuesto),
                () => Resolver(AccionAviso.Descartado));

            _cierreAutomatico = new DispatcherTimer { Interval = TimeSpan.FromSeconds(segundos) };
            _cierreAutomatico.Tick += (_, _) =>
            {
                _cierreAutomatico.Stop();
                CerrarConFundido();
            };
            _cierreAutomatico.Start();
        }

        private void EmpezarPausa_Click(object sender, RoutedEventArgs e) => Resolver(AccionAviso.PausaIniciada);

        private void Posponer_Click(object sender, RoutedEventArgs e) => Resolver(AccionAviso.Pospuesto);

        private void Descartar_Click(object sender, RoutedEventArgs e) => Resolver(AccionAviso.Descartado);

        private void Ventana_Loaded(object sender, RoutedEventArgs e)
        {
            Rect area = SystemParameters.WorkArea;
            Left = area.Right - ActualWidth - MargenPantalla;
            Top = area.Bottom - ActualHeight - MargenPantalla;

            if (_animar)
                BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, DuracionFundido));

            Accesibilidad.Anunciar(TituloTexto);
        }

        private void Resolver(AccionAviso accion)
        {
            if (_resuelto) return;
            _resuelto = true;
            _cierreAutomatico.Stop();
            _alAccion(accion);

            if (_animar) MascotaIcono.Reaccionar(ReaccionPara(accion));
            CerrarConFundido();
        }

        private static ReaccionMascota ReaccionPara(AccionAviso accion) => accion switch
        {
            AccionAviso.Pospuesto => ReaccionMascota.Pospuesta,
            AccionAviso.PausaIniciada => ReaccionMascota.PausaIniciada,
            _ => ReaccionMascota.Descartada,
        };

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
