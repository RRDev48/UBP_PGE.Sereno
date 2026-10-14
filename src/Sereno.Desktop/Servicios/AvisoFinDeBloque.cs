using System;
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
    /// (riesgo R3) y desiste a los 5 minutos. Posponer lo vuelve a mostrar a los 5 minutos.
    /// La voz se usa solo si el canal la incluye y hay una voz en español; descartar la corta.
    /// </summary>
    public sealed class AvisoFinDeBloque
    {
        private static readonly TimeSpan Reintento = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan Plazo = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan EsperaPosponer = TimeSpan.FromMinutes(5);

        private readonly SessionHistory _historial;
        private readonly AlmacenPerfiles _almacen;
        private readonly BandejaService _bandeja;
        private readonly LectorVoz _lector;
        private readonly DispatcherTimer _reintento = new() { Interval = Reintento };
        private readonly DispatcherTimer _posponer = new() { Interval = EsperaPosponer };
        private int _minutos;
        private DateTime _vence;
        private AvisoWindow? _ventanaActual;

        public AvisoFinDeBloque(SessionHistory historial, AlmacenPerfiles almacen, BandejaService bandeja, LectorVoz lector)
        {
            _historial = historial;
            _almacen = almacen;
            _bandeja = bandeja;
            _lector = lector;
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
                _lector.Hablar($"Bloque terminado. Tomá una pausa. Completaste {minutos} minutos de foco.");

            if (CanalesAviso.UsaVentana(canal))
            {
                // Refuerzo: notificación nativa del sistema, además de la ventana propia.
                // Es accesible por defecto y respeta "No molestar", a diferencia del overlay (US-B6).
                _bandeja.MostrarAviso("Bloque terminado", $"Completaste {minutos} minutos de foco.");
                Intentar();
            }
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
            _ventanaActual = new AvisoWindow(_minutos, _almacen.ObtenerPreferenciasAviso().SegundosVisible, Resolver);
            _ventanaActual.Show();
        }

        private void Resolver(AccionAviso accion)
        {
            _historial.RegistrarAviso(accion, _minutos);
            if (accion == AccionAviso.Descartado)
                _lector.Callar();
            else
                _posponer.Start();
        }
    }
}
