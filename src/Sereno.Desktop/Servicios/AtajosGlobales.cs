using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Atajos del sistema activos mientras la ventana está abierta. Permiten operar el aviso con
    /// teclado sin que la ventana tome el foco. Si otra app ya usa la combinación, se ignora.
    /// </summary>
    public sealed class AtajosGlobales : IDisposable
    {
        private const int WmHotkey = 0x0312;
        private const int IdPosponer = 1;
        private const int IdDescartar = 2;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModNoRepeat = 0x4000;
        private const uint VkP = 0x50;
        private const uint VkD = 0x44;

        private readonly Action _posponer;
        private readonly Action _descartar;
        private readonly HwndSourceHook _gancho;
        private HwndSource? _origen;
        private IntPtr _hwnd;

        public AtajosGlobales(Window ventana, Action posponer, Action descartar)
        {
            _posponer = posponer;
            _descartar = descartar;
            _gancho = Gancho;

            ventana.SourceInitialized += (_, _) =>
            {
                _hwnd = new WindowInteropHelper(ventana).Handle;
                _origen = HwndSource.FromHwnd(_hwnd);
                _origen?.AddHook(_gancho);
                RegisterHotKey(_hwnd, IdPosponer, ModControl | ModAlt | ModNoRepeat, VkP);
                RegisterHotKey(_hwnd, IdDescartar, ModControl | ModAlt | ModNoRepeat, VkD);
            };
            ventana.Closing += (_, _) => Dispose();
        }

        public void Dispose()
        {
            if (_hwnd == IntPtr.Zero) return;

            UnregisterHotKey(_hwnd, IdPosponer);
            UnregisterHotKey(_hwnd, IdDescartar);
            _origen?.RemoveHook(_gancho);
            _hwnd = IntPtr.Zero;
        }

        private IntPtr Gancho(IntPtr hwnd, int mensaje, IntPtr wParam, IntPtr lParam, ref bool manejado)
        {
            if (mensaje != WmHotkey) return IntPtr.Zero;

            switch (wParam.ToInt32())
            {
                case IdPosponer:
                    _posponer();
                    manejado = true;
                    break;
                case IdDescartar:
                    _descartar();
                    manejado = true;
                    break;
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modificadores, uint tecla);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    }
}
