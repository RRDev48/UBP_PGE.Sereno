using System;
using Sereno.Core.Timing;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Avisa cada 5 minutos, mientras un bloque está en curso, cuánto tiempo queda. Se apoya en
    /// MotorDeBloques, que ya recalcula los minutos restantes; no agrega un temporizador propio.
    /// </summary>
    public sealed class RecordatorioMascota
    {
        private const int IntervaloMinutos = 5;

        private readonly MotorDeBloques _motor;
        private int? _duracionInicial;
        private int? _ultimoAviso;

        public RecordatorioMascota(MotorDeBloques motor)
        {
            _motor = motor;
            _motor.EstadoCambio += (_, _) => Verificar();
        }

        /// <summary>Minutos restantes en el momento del aviso.</summary>
        public event EventHandler<int>? Recordatorio;

        private void Verificar()
        {
            if (_motor.Estado != BlockState.EnCurso)
            {
                _duracionInicial = null;
                _ultimoAviso = null;
                return;
            }

            int minutos = _motor.MinutosRestantes;
            _duracionInicial ??= minutos;

            // No avisa apenas arranca el bloque, aunque la duración sea múltiplo de 5.
            if (minutos <= 0 || minutos == _duracionInicial) return;
            if (minutos % IntervaloMinutos != 0 || minutos == _ultimoAviso) return;

            _ultimoAviso = minutos;
            Recordatorio?.Invoke(this, minutos);
        }
    }
}
