using System;
using System.Windows.Threading;
using Microsoft.Win32;
using Sereno.Core.Timing;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Impulsa el BlockTimer desde el hilo de la interfaz: revisa cada medio segundo y verifica
    /// al reanudar de una suspensión, sin esperar al siguiente tic.
    /// </summary>
    public sealed class MotorDeBloques : IDisposable
    {
        private readonly BlockTimer _timer;
        private readonly Dispatcher _despachador;
        private readonly DispatcherTimer _tic;

        public MotorDeBloques(BlockTimer timer)
        {
            _timer = timer;
            _despachador = Dispatcher.CurrentDispatcher;
            _tic = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _tic.Tick += (_, _) => Verificar();
            SystemEvents.PowerModeChanged += Energia_Cambio;
        }

        public void Iniciar(int minutos)
        {
            _timer.Iniciar(minutos);
            _tic.Start();
        }

        public void Detener()
        {
            _timer.Detener();
            _tic.Stop();
        }

        public void Dispose()
        {
            SystemEvents.PowerModeChanged -= Energia_Cambio;
            _tic.Stop();
        }

        private void Energia_Cambio(object? sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
                _despachador.BeginInvoke(new Action(Verificar));
        }

        private void Verificar()
        {
            _timer.Verificar();
            if (_timer.Estado != BlockState.EnCurso)
                _tic.Stop();
        }
    }
}
