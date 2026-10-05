using System;
using System.Windows.Threading;
using Sereno.Desktop.Vistas;
using Sereno.Platform.Notifications;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Muestra el aviso de fin de bloque. Si hay una app a pantalla completa o una presentación,
    /// espera a que termine en lugar de interrumpirla (riesgo R3). Si nunca se libera, desiste
    /// a los 5 minutos: un aviso demasiado tarde ya no sirve.
    /// </summary>
    public sealed class AvisoFinDeBloque
    {
        private static readonly TimeSpan Reintento = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan Plazo = TimeSpan.FromMinutes(5);

        private readonly DispatcherTimer _reintento = new() { Interval = Reintento };
        private int _minutos;
        private DateTime _vence;
        private AvisoWindow? _ventanaActual;

        public AvisoFinDeBloque()
        {
            _reintento.Tick += (_, _) => Intentar();
        }

        public void Mostrar(int minutos)
        {
            _minutos = minutos;
            _vence = DateTime.UtcNow + Plazo;
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
            _ventanaActual = new AvisoWindow(_minutos);
            _ventanaActual.Show();
        }
    }
}
