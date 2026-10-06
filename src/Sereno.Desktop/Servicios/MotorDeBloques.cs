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
        private BlockState _ultimoEstado = BlockState.Detenido;
        private int _ultimosMinutos;

        public MotorDeBloques(BlockTimer timer)
        {
            _timer = timer;
            _despachador = Dispatcher.CurrentDispatcher;
            _tic = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _tic.Tick += (_, _) => Verificar();
            SystemEvents.PowerModeChanged += Energia_Cambio;
        }

        /// <summary>Se dispara cuando cambian el estado o los minutos restantes.</summary>
        public event EventHandler? EstadoCambio;

        public BlockState Estado => _timer.Estado;

        public int MinutosRestantes => (int)Math.Ceiling(_timer.TiempoRestante.TotalMinutes);

        public void Iniciar(int minutos)
        {
            _timer.Iniciar(minutos);
            _tic.Start();
            AvisarSiCambio();
        }

        public void Detener()
        {
            _timer.Detener();
            _tic.Stop();
            AvisarSiCambio();
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
            AvisarSiCambio();
        }

        private void AvisarSiCambio()
        {
            int minutos = MinutosRestantes;
            if (Estado == _ultimoEstado && minutos == _ultimosMinutos) return;

            _ultimoEstado = Estado;
            _ultimosMinutos = minutos;
            EstadoCambio?.Invoke(this, EventArgs.Empty);
        }
    }
}
