using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Fondo Mica de Windows 11 (DWMWA_SYSTEMBACKDROP_TYPE), el mismo que usan Configuración y
    /// Sesiones de concentración nativas. Se omite en Windows 10, con alto contraste activo (en
    /// Windows o en las preferencias de Sereno) o si la persona desactivó los efectos de
    /// transparencia en Windows; en esos casos la ventana queda con el fondo opaco de siempre.
    /// </summary>
    public static class VentanaMica
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaSystemBackdropType = 38;
        private const int DwmsbtMainWindow = 2;
        private const int BuildMinimo = 22621; // Windows 11 22H2: primera versión con DWMWA_SYSTEMBACKDROP_TYPE.

        public static void Aplicar(Window ventana, bool temaOscuro)
        {
            if (!Soportado()) return;

            ventana.Background = Brushes.Transparent;

            void AplicarAtributos(object? sender, EventArgs e)
            {
                IntPtr hwnd = new WindowInteropHelper(ventana).Handle;
                if (hwnd == IntPtr.Zero) return;

                // OJO: no tocar HwndSource.CompositionTarget.BackgroundColor acá. Ponerlo en
                // Transparent (sin AllowsTransparency, que requeriría sacar el marco nativo)
                // deja el área cliente en blanco: DWM acepta el backdrop pero WPF no compone
                // nada sobre él. Alcanza con Window.Background para que Mica se vea detrás.
                int oscuro = temaOscuro ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref oscuro, sizeof(int));
                int tipo = DwmsbtMainWindow;
                DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref tipo, sizeof(int));
            }

            if (PresentationSource.FromVisual(ventana) is not null)
                AplicarAtributos(null, EventArgs.Empty);
            else
                ventana.SourceInitialized += AplicarAtributos;
        }

        public static bool Soportado() =>
            Environment.OSVersion.Version.Build >= BuildMinimo
            && !SystemParameters.HighContrast
            && !TemaService.AltoContrasteActivo
            && TransparenciaHabilitada();

        /// <summary>Config. > Accesibilidad > Efectos visuales > "Efectos de transparencia".</summary>
        private static bool TransparenciaHabilitada()
        {
            try
            {
                using RegistryKey? clave = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return clave?.GetValue("EnableTransparency") is not int valor || valor != 0;
            }
            catch (Exception)
            {
                return true;
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int valor, int tamano);
    }
}
