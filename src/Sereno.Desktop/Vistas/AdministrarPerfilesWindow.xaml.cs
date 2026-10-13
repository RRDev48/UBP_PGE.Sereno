using System;
using System.IO;
using System.Windows;
using Sereno.Core.Modelos;
using Sereno.Platform.Storage;

namespace Sereno.Desktop.Vistas
{
    /// <summary>Renombrar o eliminar perfiles de esta computadora, con confirmación para eliminar.</summary>
    public partial class AdministrarPerfilesWindow : Window
    {
        private readonly AlmacenPerfiles _almacen;
        private Perfil? _perfilEnEdicion;
        private Perfil? _perfilAEliminar;

        public event EventHandler? VolverSolicitado;

        public AdministrarPerfilesWindow(AlmacenPerfiles almacen)
        {
            InitializeComponent();
            _almacen = almacen;
            NuevoNombre.TextoCambiado += (_, _) => NuevoNombre.LimpiarError();
            Actualizar();
            Loaded += (_, _) => Lista.Focus();
        }

        private void Actualizar()
        {
            Lista.ItemsSource = null;
            Lista.ItemsSource = _almacen.Perfiles;
            VacioTexto.Visibility = _almacen.Perfiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Renombrar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not Perfil perfil) return;

            _perfilAEliminar = null;
            PanelEliminar.Visibility = Visibility.Collapsed;

            _perfilEnEdicion = perfil;
            NuevoNombre.Texto = perfil.Nombre;
            NuevoNombre.LimpiarError();
            PanelRenombrar.Visibility = Visibility.Visible;
            NuevoNombre.EnfocarCampo();
        }

        private void GuardarRenombre_Click(object sender, RoutedEventArgs e)
        {
            if (_perfilEnEdicion is null) return;

            try
            {
                _almacen.RenombrarPerfil(_perfilEnEdicion, NuevoNombre.Texto);
            }
            catch (ArgumentException)
            {
                NuevoNombre.MostrarError("Escribí un nombre para el perfil.");
                return;
            }
            catch (InvalidOperationException ex)
            {
                NuevoNombre.MostrarError(ex.Message);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                NuevoNombre.MostrarError("No se pudo guardar. Revisá los permisos de la carpeta de Sereno.");
                return;
            }

            PanelRenombrar.Visibility = Visibility.Collapsed;
            _perfilEnEdicion = null;
            Actualizar();
        }

        private void CancelarRenombre_Click(object sender, RoutedEventArgs e)
        {
            PanelRenombrar.Visibility = Visibility.Collapsed;
            _perfilEnEdicion = null;
        }

        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not Perfil perfil) return;

            _perfilEnEdicion = null;
            PanelRenombrar.Visibility = Visibility.Collapsed;

            _perfilAEliminar = perfil;
            EliminarTitulo.Text = $"¿Eliminar el perfil de {perfil.Nombre}?";
            PanelEliminar.Visibility = Visibility.Visible;
        }

        private void ConfirmarEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_perfilAEliminar is null) return;

            try
            {
                _almacen.EliminarPerfil(_perfilAEliminar);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, "No se pudo eliminar. Revisá los permisos de la carpeta de Sereno.",
                    "Sereno", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PanelEliminar.Visibility = Visibility.Collapsed;
            _perfilAEliminar = null;
            Actualizar();
        }

        private void CancelarEliminar_Click(object sender, RoutedEventArgs e)
        {
            PanelEliminar.Visibility = Visibility.Collapsed;
            _perfilAEliminar = null;
        }

        private void Volver_Click(object sender, RoutedEventArgs e) =>
            VolverSolicitado?.Invoke(this, EventArgs.Empty);
    }
}
