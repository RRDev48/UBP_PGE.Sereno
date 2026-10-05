using System;
using Sereno.Core.Events;

namespace Sereno.Core.Timing
{
    /// <summary>
    /// Mide un bloque de foco contra un IClock. No tiene temporizador propio: quien lo usa llama a
    /// Verificar periódicamente y al reanudar de una suspensión. Si el bloque venció mientras la
    /// máquina estaba suspendida, Verificar publica un único BloqueTerminado, sin avisos acumulados.
    /// </summary>
    public sealed class BlockTimer
    {
        private readonly IClock _reloj;
        private readonly EventBus _bus;
        private TimeSpan _inicio;
        private int _minutos;

        public BlockTimer(IClock reloj, EventBus bus)
        {
            _reloj = reloj;
            _bus = bus;
        }

        public BlockState Estado { get; private set; } = BlockState.Detenido;

        public void Iniciar(int minutos)
        {
            if (!Duraciones.EsBloqueValido(minutos))
                throw new ArgumentOutOfRangeException(nameof(minutos));

            _minutos = minutos;
            _inicio = _reloj.Ahora;
            Estado = BlockState.EnCurso;
        }

        public void Detener() => Estado = BlockState.Detenido;

        public void Verificar()
        {
            if (Estado != BlockState.EnCurso) return;
            if (_reloj.Ahora - _inicio < TimeSpan.FromMinutes(_minutos)) return;

            Estado = BlockState.Detenido;
            _bus.Publish(new BloqueTerminado(_minutos));
        }
    }
}
