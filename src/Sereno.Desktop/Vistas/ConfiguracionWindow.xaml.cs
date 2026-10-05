using System;
using System.Globalization;
using System.IO;
using System.Windows;
using Sereno.Core.Modelos;
using Sereno.Core.Timing;
using Sereno.Desktop.Servicios;
using Sereno.Platform.Storage;

namespace Sereno.Desktop.Vistas
{
    /// <summary>Configuración de la duración de los bloques de foco y de las pausas.</summary>
    public partial class ConfiguracionWindow : Window
    {
        private readonly AlmacenPerfiles _almacen;

        public ConfiguracionWindow(AlmacenPerfiles almacen)
        {
            InitializeComponent();
            _almacen = almacen;

            var (minutosBloque, minutosPausa) = _almacen.ObtenerDuraciones();
            Bloque.Texto = minutosBloque.ToString(CultureInfo.InvariantCulture);
            Pausa.Texto = minutosPausa.ToString(CultureInfo.InvariantCulture);
            Bloque.TextoCambiado += (_, _) => Bloque.LimpiarError();
            Pausa.TextoCambiado += (_, _) => Pausa.LimpiarError();

            PreferenciasVisuales preferencias = _almacen.ObtenerPreferencias();
            FuenteLectura.IsChecked = preferencias.FuenteLectura;
            AltoContraste.IsChecked = preferencias.AltoContraste;

            Loaded += (_, _) => Bloque.EnfocarCampo();
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            bool bloqueValido = EsEntero(Bloque.Texto, out int minutosBloque) && Duraciones.EsBloqueValido(minutosBloque);
            bool pausaValida = EsEntero(Pausa.Texto, out int minutosPausa) && Duraciones.EsPausaValida(minutosPausa);

            if (!bloqueValido)
                Bloque.MostrarError("Escribí un número entre 5 y 90.");
            if (!pausaValida)
                Pausa.MostrarError("Escribí un número entre 1 y 30.");
            if (!bloqueValido)
            {
                Bloque.EnfocarCampo();
                return;
            }
            if (!pausaValida)
            {
                Pausa.EnfocarCampo();
                return;
            }

            var preferencias = new PreferenciasVisuales
            {
                FuenteLectura = FuenteLectura.IsChecked == true,
                AltoContraste = AltoContraste.IsChecked == true,
            };

            try
            {
                _almacen.GuardarDuraciones(minutosBloque, minutosPausa);
                _almacen.GuardarPreferencias(preferencias);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Bloque.MostrarError("No se pudo guardar la configuración. Revisá los permisos de la carpeta de Sereno.");
                return;
            }

            TemaService.Aplicar(Application.Current, preferencias);
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e) => Close();

        private static bool EsEntero(string texto, out int valor) =>
            int.TryParse(texto.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out valor);
    }
}
