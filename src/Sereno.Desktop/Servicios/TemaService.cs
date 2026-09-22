using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace Sereno.Desktop.Servicios
{
    /// <summary>Aplica el tema claro u oscuro según la configuración de Windows.</summary>
    public static class TemaService
    {
        private const string ClaveRegistro = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        /// <summary>Falso si Windows tiene desactivadas las animaciones o el contraste alto activo.</summary>
        public static bool AnimacionesActivas =>
            SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast;

        public static bool WindowsUsaTemaOscuro()
        {
            try
            {
                using RegistryKey? clave = Registry.CurrentUser.OpenSubKey(ClaveRegistro);
                return clave?.GetValue("AppsUseLightTheme") is int valor && valor == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Aplicar(Application app)
        {
            string archivo = WindowsUsaTemaOscuro() ? "Oscuro.xaml" : "Claro.xaml";
            var nuevo = new ResourceDictionary { Source = new Uri("pack://application:,,,/Temas/" + archivo, UriKind.Absolute) };

            var diccionarios = app.Resources.MergedDictionaries;
            var anterior = diccionarios.FirstOrDefault(d => d.Source is not null &&
                (d.Source.OriginalString.EndsWith("Claro.xaml", StringComparison.OrdinalIgnoreCase) ||
                 d.Source.OriginalString.EndsWith("Oscuro.xaml", StringComparison.OrdinalIgnoreCase)));

            if (anterior is not null) diccionarios.Remove(anterior);
            diccionarios.Insert(0, nuevo);
        }
    }
}
