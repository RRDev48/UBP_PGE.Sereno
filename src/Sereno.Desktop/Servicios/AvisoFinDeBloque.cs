using System;
using System.Windows.Threading;
using Sereno.Core.History;
using Sereno.Desktop.Vistas;
using Sereno.Platform.Notifications;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Muestra el aviso de fin de bloque y registra lo que la persona hace con él.
    /// Si hay una app a pantalla completa o una presentación, espera a que termine en lugar de
    /// interrumpirla (riesgo R3); si nunca se libera, desiste a los 5 minutos.
    /// Posponer vuelve a mostrar el aviso a los 5 minutos.
    /// </summary>
    public sealed class AvisoFinDeBloque
    {
        private static readonly TimeSpan Reintento = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan Plazo = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan EsperaPosponer = TimeSpan.FromMinutes(5);

        private readonly SessionHistory _historial;
        private readonly DispatcherTimer _reintento = new() { Interval = Reintento };
        private readonly DispatcherTimer _posponer = new() { Interval = EsperaPosponer };
        private int _minutos;
        private DateTime _vence;
        private AvisoWindow? _ventanaActual;

        public AvisoFinDeBloque(SessionHistory historial)
        {
            _historial = historial;
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
            _historial.RegistrarAccion(accion, _minutos);
            if (accion == AccionAviso.Pospuesto)
                _posponer.Start();
        }
    }
}
