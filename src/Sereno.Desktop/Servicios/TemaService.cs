using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Sereno.Core.Modelos;

namespace Sereno.Desktop.Servicios
{
    /// <summary>
    /// Aplica el tema (claro, oscuro o de alto contraste) y la fuente de lectura según las preferencias.
    /// Sin preferencias de contraste, sigue la configuración de Windows.
    /// </summary>
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

        public static void Aplicar(Application app, PreferenciasVisuales preferencias)
        {
            string archivo = preferencias.AltoContraste ? "AltoContraste.xaml"
                : WindowsUsaTemaOscuro() ? "Oscuro.xaml"
                : "Claro.xaml";
            var nuevo = new ResourceDictionary { Source = new Uri("pack://application:,,,/Temas/" + archivo, UriKind.Absolute) };

            var diccionarios = app.Resources.MergedDictionaries;
            var anterior = diccionarios.FirstOrDefault(d => d.Source is not null && EsDiccionarioDeTema(d.Source.OriginalString));

            if (anterior is not null) diccionarios.Remove(anterior);
            diccionarios.Insert(0, nuevo);

            AplicarFuente(app, preferencias.FuenteLectura);
            AplicarTamano(app, preferencias);
        }

        private static void AplicarTamano(Application app, PreferenciasVisuales preferencias)
        {
            int escala = PreferenciasVisuales.EsEscalaValida(preferencias.Escala)
                ? preferencias.Escala
                : PreferenciasVisuales.EscalaPorDefecto;
            double factorEscala = escala / 100.0;

            app.Resources["EscalaVisual"] = new ScaleTransform(factorEscala, factorEscala);
            var espaciado = (EspaciadoVisual)app.FindResource("EspaciadoVisual");
            espaciado.Factor = preferencias.EspaciadoAmplio ? 1.5 : 0.0;
        }

        private static bool EsDiccionarioDeTema(string ruta) =>
            ruta.EndsWith("Claro.xaml", StringComparison.OrdinalIgnoreCase) ||
            ruta.EndsWith("Oscuro.xaml", StringComparison.OrdinalIgnoreCase) ||
            ruta.EndsWith("AltoContraste.xaml", StringComparison.OrdinalIgnoreCase);

        private static void AplicarFuente(Application app, bool fuenteLectura)
        {
            if (!fuenteLectura)
            {
                app.Resources.Remove("FuenteBase");
                return;
            }
            app.Resources["FuenteBase"] = new FontFamily(
                new Uri("pack://application:,,,/Recursos/Fuentes/"), "./#Atkinson Hyperlegible");
        }
    }
}
