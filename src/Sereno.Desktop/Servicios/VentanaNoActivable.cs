using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Hace que una ventana no tome el foco al mostrarse ni al recibir un clic (WS_EX_NOACTIVATE).
    /// Con ShowActivated=false solo no se activa al mostrarse; el estilo extendido cubre también el clic.
    /// </summary>
    public static class VentanaNoActivable
    {
        private const int GwlExstyle = -20;
        private const long WsExNoactivate = 0x08000000L;

        public static void Aplicar(Window ventana)
        {
            ventana.ShowActivated = false;
            ventana.SourceInitialized += (_, _) =>
            {
                IntPtr hwnd = new WindowInteropHelper(ventana).Handle;
                long estilo = GetWindowLongPtr(hwnd, GwlExstyle).ToInt64();
                SetWindowLongPtr(hwnd, GwlExstyle, new IntPtr(estilo | WsExNoactivate));
            };
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int indice);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int indice, IntPtr valor);
    }
}
