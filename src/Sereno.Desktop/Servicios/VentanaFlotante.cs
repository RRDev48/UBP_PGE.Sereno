using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Ventana que no roba el foco ni al mostrarse ni al hacer clic (WS_EX_NOACTIVATE), y que no
    /// aparece en Alt+Tab (WS_EX_TOOLWINDOW). ShowActivated=false cubre solo el primer caso.
    /// </summary>
    public static class VentanaFlotante
    {
        private const int GwlExstyle = -20;
        private const long WsExNoactivate = 0x08000000L;
        private const long WsExToolwindow = 0x00000080L;

        public static void Aplicar(Window ventana)
        {
            ventana.ShowActivated = false;
            ventana.SourceInitialized += (_, _) =>
            {
                IntPtr hwnd = new WindowInteropHelper(ventana).Handle;
                long estilo = GetWindowLongPtr(hwnd, GwlExstyle).ToInt64();
                SetWindowLongPtr(hwnd, GwlExstyle, new IntPtr(estilo | WsExNoactivate | WsExToolwindow));
            };
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int indice);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int indice, IntPtr valor);
    }
}
