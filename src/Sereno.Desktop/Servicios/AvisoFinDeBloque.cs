using System;
using System.Windows.Threading;
using Sereno.Core.History;
using Sereno.Core.Modelos;
using Sereno.Desktop.Vistas;
using Sereno.Platform.Notifications;
using Sereno.Platform.Storage;
using Sereno.Platform.Tray;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Avisa al terminar un bloque por el canal elegido en la configuración y registra lo que la
    /// persona hace con el aviso. El canal visual espera si hay pantalla completa o presentación
    /// (riesgo R3) y desiste a los 5 minutos. Posponer lo vuelve a mostrar a los 5 minutos.
    /// </summary>
    public sealed class AvisoFinDeBloque
    {
        private static readonly TimeSpan Reintento = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan Plazo = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan EsperaPosponer = TimeSpan.FromMinutes(5);

        private readonly SessionHistory _historial;
        private readonly AlmacenPerfiles _almacen;
        private readonly BandejaService _bandeja;
        private readonly DispatcherTimer _reintento = new() { Interval = Reintento };
        private readonly DispatcherTimer _posponer = new() { Interval = EsperaPosponer };
        private int _minutos;
        private DateTime _vence;
        private AvisoWindow? _ventanaActual;

        public AvisoFinDeBloque(SessionHistory historial, AlmacenPerfiles almacen, BandejaService bandeja)
        {
            _historial = historial;
            _almacen = almacen;
            _bandeja = bandeja;
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
            Intentar();
        }

        private void Intentar()
        {
            CanalAviso canal = _almacen.ObtenerPreferenciasAviso().Canal;

            switch (canal)
            {
                case CanalAviso.SoloSistema:
                    _bandeja.MostrarAviso("Bloque terminado", $"Completaste {_minutos} minutos de foco.");
                    return;
                case CanalAviso.SoloHablado:
                    // La voz llega con US-B3; hasta entonces este canal no muestra nada.
                    return;
            }

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
            _ventanaActual = new AvisoWindow(_minutos, Resolver);
            _ventanaActual.Show();
        }

        private void Resolver(AccionAviso accion)
        {
            _historial.RegistrarAviso(accion, _minutos);
            if (accion == AccionAviso.Pospuesto)
                _posponer.Start();
        }
    }
}
