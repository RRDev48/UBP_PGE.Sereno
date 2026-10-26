using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Sereno.Core.History;
using Sereno.Core.Modelos;
using Sereno.Desktop.Vistas;
using Sereno.Platform.Notifications;
using Sereno.Platform.Speech;
using Sereno.Platform.Storage;
using Sereno.Platform.Tray;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Avisa al terminar un bloque por el canal elegido en la configuración y registra lo que la
    /// persona hace con el aviso. El canal visual espera si hay pantalla completa o presentación
    /// (riesgo R3) y desiste a los 5 minutos. Posponer lo vuelve a mostrar a los 5 minutos. Empezar
    /// pausa dispara <see cref="PausaSolicitada"/> para que la aplicación abra el catálogo de pausas.
    /// La voz se usa solo si el canal la incluye y hay una voz en español; descartar o empezar la
    /// pausa la cortan.
    /// </summary>
    public sealed class AvisoFinDeBloque
    {
        public event EventHandler? PausaSolicitada;

        private static readonly TimeSpan Reintento = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan Plazo = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan EsperaPosponer = TimeSpan.FromMinutes(5);

        private readonly SessionHistory _historial;
        private readonly AlmacenPerfiles _almacen;
        private readonly BandejaService _bandeja;
        private readonly LectorVoz _lector;
        private readonly VozModerna _vozModerna;
        private readonly DispatcherTimer _reintento = new() { Interval = Reintento };
        private readonly DispatcherTimer _posponer = new() { Interval = EsperaPosponer };
        private int _minutos;
        private DateTime _vence;
        private AvisoWindow? _ventanaActual;

        public AvisoFinDeBloque(
            SessionHistory historial, AlmacenPerfiles almacen, BandejaService bandeja, LectorVoz lector, VozModerna vozModerna)
        {
            _historial = historial;
            _almacen = almacen;
            _bandeja = bandeja;
            _lector = lector;
            _vozModerna = vozModerna;
            _reintento.Tick += (_, _) => Intentar();
            _posponer.Tick += (_, _) =>
            {
                _posponer.Stop();
                Mostrar(_minutos);
            };
        }

        public void Mostrar(int minutos)
        {
            _minutos = minutos;
            _vence = DateTime.UtcNow + Plazo;
            _posponer.Stop();

            CanalAviso canal = CanalesAviso.Efectivo(
                _almacen.ObtenerPreferenciasAviso().Canal, LectorVoz.HayVozEnEspanol());

            if (canal == CanalAviso.SoloSistema)
            {
                _bandeja.MostrarAviso("Bloque terminado", $"Completaste {minutos} minutos de foco.");
                return;
            }

            if (CanalesAviso.UsaVoz(canal))
                HablarPorElCanal($"Bloque terminado. Tomá una pausa. Completaste {minutos} minutos de foco.");

            if (CanalesAviso.UsaVentana(canal))
            {
                // Refuerzo: notificación nativa del sistema, además de la ventana propia.
                // Es accesible por defecto y respeta "No molestar", a diferencia del overlay (US-B6).
                _bandeja.MostrarAviso("Bloque terminado", $"Completaste {minutos} minutos de foco.");
                Intentar();
            }
        }

        /// <summary>Usa la voz moderna si hay una elegida; si falla o no hay, cae a la voz clásica.</summary>
        private void HablarPorElCanal(string texto)
        {
            string vozId = _almacen.ObtenerPreferenciasAviso().VozId;
            if (string.IsNullOrEmpty(vozId))
            {
                _lector.Hablar(texto);
                return;
            }

            _ = HablarModernaConFallbackAsync(vozId, texto);
        }

        private async Task HablarModernaConFallbackAsync(string vozId, string texto)
        {
            bool ok = await _vozModerna.HablarAsync(vozId, texto);
            if (!ok) _lector.Hablar(texto);
        }

        private void Intentar()
        {
            if (EstadoPresentacion.PantallaOcupada())
            {
                if (DateTime.UtcNow >= _vence)
                    _reintento.Stop();
                else
                    _reintento.Start();
                return;
            }

            _reintento.Stop();
            _ventanaActual?.Close();
            PreferenciasAviso preferencias = _almacen.ObtenerPreferenciasAviso();
            bool animar = TemaService.AnimacionesActivas && !preferencias.BajoEstimulo;
            _ventanaActual = new AvisoWindow(_minutos, preferencias.SegundosVisible, animar, Resolver);
            _ventanaActual.Show();
        }

        private void Resolver(AccionAviso accion)
        {
            _historial.RegistrarAviso(accion, _minutos);
            switch (accion)
            {
                case AccionAviso.Descartado:
                    _lector.Callar();
                    _vozModerna.Callar();
                    break;
                case AccionAviso.PausaIniciada:
                    _lector.Callar();
                    _vozModerna.Callar();
                    PausaSolicitada?.Invoke(this, EventArgs.Empty);
                    break;
                default:
                    _posponer.Start();
                    break;
            }
        }
    }
}
