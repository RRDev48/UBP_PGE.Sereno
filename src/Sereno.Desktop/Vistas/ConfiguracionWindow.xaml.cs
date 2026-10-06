using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Sereno.Core.Modelos;
using Sereno.Core.Timing;
using Sereno.Desktop.Servicios;
using Sereno.Platform.Storage;

namespace Sereno.Desktop.Vistas
{
    /// <summary>Configuración: duraciones, lectura, canal del aviso y datos del usuario.</summary>
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

            PreferenciasVisuales visuales = _almacen.ObtenerPreferencias();
            FuenteLectura.IsChecked = visuales.FuenteLectura;
            AltoContraste.IsChecked = visuales.AltoContraste;
            EspaciadoAmplio.IsChecked = visuales.EspaciadoAmplio;
            Escala.SelectedIndex = visuales.Escala switch { 125 => 1, 150 => 2, _ => 0 };

            PreferenciasAviso aviso = _almacen.ObtenerPreferenciasAviso();
            (aviso.Canal switch
            {
                CanalAviso.SoloHablado => CanalHablado,
                CanalAviso.VisualYHablado => CanalAmbos,
                CanalAviso.SoloSistema => CanalSistema,
                _ => CanalVisual,
            }).IsChecked = true;
            BajoEstimulo.IsChecked = aviso.BajoEstimulo;

            UbicacionDatos.Text = $"Tus datos están en {Rutas.Base}. No salen de esta computadora.";

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

            var visuales = new PreferenciasVisuales
            {
                FuenteLectura = FuenteLectura.IsChecked == true,
                AltoContraste = AltoContraste.IsChecked == true,
                EspaciadoAmplio = EspaciadoAmplio.IsChecked == true,
                Escala = Escala.SelectedItem is ComboBoxItem { Tag: string etiqueta } && int.TryParse(etiqueta, out int valor)
                    ? valor
                    : PreferenciasVisuales.EscalaPorDefecto,
            };

            var aviso = new PreferenciasAviso { Canal = CanalElegido(), BajoEstimulo = BajoEstimulo.IsChecked == true };

            try
            {
                _almacen.GuardarDuraciones(minutosBloque, minutosPausa);
                _almacen.GuardarPreferencias(visuales);
                _almacen.GuardarPreferenciasAviso(aviso);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Bloque.MostrarError("No se pudo guardar la configuración. Revisá los permisos de la carpeta de Sereno.");
                return;
            }

            TemaService.Aplicar(Application.Current, visuales);
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e) => Close();

        private void Exportar_Click(object sender, RoutedEventArgs e)
        {
            var dialogo = new SaveFileDialog
            {
                Title = "Exportar datos de Sereno",
                FileName = "sereno-datos.txt",
                Filter = "Texto (*.txt)|*.txt",
            };
            if (dialogo.ShowDialog(this) != true) return;

            try
            {
                File.WriteAllText(dialogo.FileName, _almacen.ExportarTexto(), new UTF8Encoding(false));
                MessageBox.Show(this, "Los datos se exportaron correctamente.", "Sereno",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, "No se pudo exportar. Elegí otra carpeta o revisá los permisos.", "Sereno",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PedirBorrado_Click(object sender, RoutedEventArgs e) => PanelBorrado.Visibility = Visibility.Visible;

        private void CancelarBorrado_Click(object sender, RoutedEventArgs e) => PanelBorrado.Visibility = Visibility.Collapsed;

        private void ConfirmarBorrado_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _almacen.BorrarTodo();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, "No se pudieron borrar todos los datos. Revisá los permisos de la carpeta de Sereno.",
                    "Sereno", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Application.Current.Shutdown();
        }

        private CanalAviso CanalElegido()
        {
            if (CanalHablado.IsChecked == true) return CanalAviso.SoloHablado;
            if (CanalAmbos.IsChecked == true) return CanalAviso.VisualYHablado;
            if (CanalSistema.IsChecked == true) return CanalAviso.SoloSistema;
            return CanalAviso.SoloVisual;
        }

        private static bool EsEntero(string texto, out int valor) =>
            int.TryParse(texto.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out valor);
    }
}
