using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Sereno.Core.Acceso;
using Sereno.Core.Modelos;
using Sereno.Desktop.Servicios;
using Sereno.Platform.Storage;

namespace Sereno.Desktop.Vistas
{
    /// <summary>Recuperación del acceso en tres pasos: clave, nueva contraseña y confirmación.</summary>
    public partial class RecuperarWindow : Window
    {
        private static readonly string[] NombresPasos = { "Clave", "Nueva contraseña", "Listo" };

        private readonly AlmacenPerfiles _almacen;
        private readonly Perfil _perfil;
        private bool _formateando;

        public event EventHandler? VolverSolicitado;

        public RecuperarWindow(AlmacenPerfiles almacen, Perfil perfil)
        {
            InitializeComponent();
            _almacen = almacen;
            _perfil = perfil;

            BajadaPaso1.Text = $"Escribí la clave de recuperación que guardaste al crear el perfil de {perfil.Nombre}.";
            Clave.CajaTexto.TextChanged += Clave_TextChanged;
            Nueva.ContrasenaCambiada += (_, _) => Nueva.LimpiarError();
            Repetir.ContrasenaCambiada += (_, _) => Repetir.LimpiarError();

            IrAPaso(0);
            Loaded += (_, _) => Clave.EnfocarCampo();
        }

        private void IrAPaso(int paso)
        {
            Paso1.Visibility = paso == 0 ? Visibility.Visible : Visibility.Collapsed;
            Paso2.Visibility = paso == 1 ? Visibility.Visible : Visibility.Collapsed;
            Paso3.Visibility = paso == 2 ? Visibility.Visible : Visibility.Collapsed;

            // Enter ejecuta la acción principal del paso visible.
            Verificar.IsDefault = paso == 0;
            Guardar.IsDefault = paso == 1;
            IrALogin.IsDefault = paso == 2;

            Pasos.Configurar(NombresPasos, paso);
        }

        /// <summary>Agrupa la clave de a 4 mientras se escribe y la pasa a mayúsculas.</summary>
        private void Clave_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_formateando) return;
            _formateando = true;

            TextBox caja = Clave.CajaTexto;
            string formateada = ClaveRecuperacion.Formatear(ClaveRecuperacion.Normalizar(caja.Text));
            if (formateada != caja.Text)
            {
                caja.Text = formateada;
                caja.CaretIndex = formateada.Length;
            }
            Clave.LimpiarError();

            _formateando = false;
        }

        private async void Verificar_Click(object sender, RoutedEventArgs e)
        {
            string clave = ClaveRecuperacion.Normalizar(Clave.Texto);
            if (clave.Length == 0)
            {
                MostrarErrorClave("Escribí tu clave de recuperación.");
                return;
            }
            if (clave.Length < ClaveRecuperacion.Longitud)
            {
                int faltan = ClaveRecuperacion.Longitud - clave.Length;
                MostrarErrorClave(faltan == 1
                    ? "La clave tiene 16 caracteres; falta 1."
                    : $"La clave tiene 16 caracteres; faltan {faltan}.");
                return;
            }

            bool valida;
            Verificar.IsEnabled = false;
            Cursor = Cursors.Wait;
            try
            {
                valida = await Task.Run(() => _almacen.VerificarClave(_perfil, clave));
            }
            finally
            {
                Verificar.IsEnabled = true;
                Cursor = null;
            }

            if (!valida)
            {
                MostrarErrorClave($"Esta clave no corresponde al perfil de {_perfil.Nombre}.");
                Clave.CajaTexto.SelectAll();
                return;
            }

            IrAPaso(1);
            Nueva.EnfocarCampo();
        }

        private void MostrarErrorClave(string mensaje)
        {
            Clave.MostrarError(mensaje);
            Clave.EnfocarCampo();
        }

        private async void Guardar_Click(object sender, RoutedEventArgs e)
        {
            string nueva = Nueva.Contrasena;
            string repetida = Repetir.Contrasena;
            bool enfocado = false;

            if (ReglasContrasena.ValidarLongitud(nueva) is string errorLongitud)
            {
                Nueva.MostrarError(errorLongitud);
                Nueva.EnfocarCampo();
                enfocado = true;
            }
            if (ReglasContrasena.ValidarRepeticion(nueva, repetida) is string errorRepeticion)
            {
                Repetir.MostrarError(errorRepeticion);
                if (!enfocado) Repetir.EnfocarCampo();
                enfocado = true;
            }
            if (enfocado) return;

            Guardar.IsEnabled = false;
            Cursor = Cursors.Wait;
            try
            {
                await Task.Run(() => _almacen.CambiarContrasena(_perfil, nueva));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Nueva.MostrarError("No se pudo guardar la contraseña. Revisá los permisos de la carpeta de Sereno.");
                return;
            }
            finally
            {
                Guardar.IsEnabled = true;
                Cursor = null;
            }

            Nueva.Limpiar();
            Repetir.Limpiar();
            ExitoTexto.Text = $"El perfil de {_perfil.Nombre} está listo.";
            IrAPaso(2);
            Accesibilidad.Anunciar(ExitoTexto);
            IrALogin.Focus();
        }

        private void OtraClave_Click(object sender, RoutedEventArgs e)
        {
            Nueva.Limpiar();
            Repetir.Limpiar();
            IrAPaso(0);
            Clave.CajaTexto.SelectAll();
            Clave.EnfocarCampo();
        }

        private void Volver_Click(object sender, RoutedEventArgs e) =>
            VolverSolicitado?.Invoke(this, EventArgs.Empty);
    }
}
