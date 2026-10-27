using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Sereno.Core.Timing;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Mientras un bloque está en curso, avisa si la ventana activa pasa a ser un navegador o Steam.
    /// No cierra ni minimiza nada, no guarda qué programa se usó: solo dispara Distraccion para que
    /// se muestre un recordatorio, igual que el de cada 5 minutos. Detecta el cambio de primer plano
    /// con SetWinEventHook, sin sondear.
    /// </summary>
    public sealed class DetectorDistraccion : IDisposable
    {
        private static readonly string[] AppsDistraccion =
        {
            "chrome", "msedge", "firefox", "opera", "opera_gx", "brave", "vivaldi", "iexplore", "steam",
        };

        private static readonly TimeSpan Enfriamiento = TimeSpan.FromMinutes(3);

        private readonly MotorDeBloques _motor;
        private readonly Func<bool> _habilitado;
        private readonly WinEventDelegate _callback;
        private readonly IntPtr _hook;
        private DateTime _ultimoAviso = DateTime.MinValue;

        /// <param name="habilitado">Se consulta en cada cambio de ventana: la preferencia puede cambiarse en Configuración.</param>
        public DetectorDistraccion(MotorDeBloques motor, Func<bool> habilitado)
        {
            _motor = motor;
            _habilitado = habilitado;
            _callback = AlCambiarPrimerPlano;
            _hook = SetWinEventHook(EventSystemForeground, EventSystemForeground, IntPtr.Zero,
                _callback, 0, 0, WineventOutofcontext);
        }

        /// <summary>Minutos restantes en el momento del aviso.</summary>
        public event EventHandler<int>? Distraccion;

        public void Dispose()
        {
            if (_hook != IntPtr.Zero) UnhookWinEvent(_hook);
        }

        private void AlCambiarPrimerPlano(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint eventThread, uint eventTime)
        {
            if (_motor.Estado != BlockState.EnCurso || !_habilitado()) return;
            if (hwnd == IntPtr.Zero) return;
            if (DateTime.UtcNow - _ultimoAviso < Enfriamiento) return;

            string? nombre = NombreDelProceso(hwnd);
            if (nombre is null || Array.IndexOf(AppsDistraccion, nombre.ToLowerInvariant()) < 0) return;

            _ultimoAviso = DateTime.UtcNow;
            Distraccion?.Invoke(this, _motor.MinutosRestantes);
        }

        private static string? NombreDelProceso(IntPtr hwnd)
        {
            try
            {
                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return null;
                using Process proceso = Process.GetProcessById((int)pid);
                return proceso.ProcessName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private const uint EventSystemForeground = 3;
        private const uint WineventOutofcontext = 0;

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint eventThread, uint eventTime);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}
