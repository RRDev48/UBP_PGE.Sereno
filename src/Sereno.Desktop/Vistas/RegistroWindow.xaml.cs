using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using Sereno.Core.Acceso;
using Sereno.Core.Modelos;
using Sereno.Desktop.Servicios;
using Sereno.Platform.Storage;

namespace Sereno.Desktop.Vistas
{
    /// <summary>Creación de un perfil local en dos pasos: datos y clave de recuperación.</summary>
    public partial class RegistroWindow : Window
    {
        private static readonly string[] NombresPasos = { "Tus datos", "Clave de recuperación" };

        private readonly AlmacenPerfiles _almacen;
        private Perfil? _perfil;
        private string _clave = string.Empty;

        public event EventHandler<PerfilEventArgs>? RegistroCompletado;
        public event EventHandler? VolverSolicitado;
        public event EventHandler? UsoSinPerfilSolicitado;

        /// <param name="primerUso">
        /// En el primer uso no hay perfiles a los que volver, así que se ofrece usar Sereno sin perfil.
        /// </param>
        public RegistroWindow(AlmacenPerfiles almacen, bool primerUso)
        {
            InitializeComponent();
            _almacen = almacen;

            Pasos.Configurar(NombresPasos, 0);
            YaTengoPerfil.Visibility = primerUso ? Visibility.Collapsed : Visibility.Visible;
            UsarSinPerfil.Visibility = primerUso ? Visibility.Visible : Visibility.Collapsed;

            Nombre.TextoCambiado += (_, _) => Nombre.LimpiarError();
            Contrasena.ContrasenaCambiada += (_, _) => ActualizarFuerza();
            Repetir.ContrasenaCambiada += (_, _) => Repetir.LimpiarError();

            Loaded += (_, _) => Nombre.EnfocarCampo();
        }

        private void ActualizarFuerza()
        {
            Contrasena.LimpiarError();
            NivelFuerza nivel = ReglasContrasena.Evaluar(Contrasena.Contrasena);
            Rectangle[] segmentos = { Segmento1, Segmento2, Segmento3 };
            for (int i = 0; i < segmentos.Length; i++)
                segmentos[i].SetResourceReference(Shape.FillProperty, i < (int)nivel ? "Acento" : "Separador");

            FuerzaTexto.Text = "Fuerza: " + ReglasContrasena.Describir(nivel);
            Accesibilidad.Anunciar(FuerzaTexto);
        }

        private async void Continuar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = Nombre.Texto.Trim();
            string contrasena = Contrasena.Contrasena;
            string repetida = Repetir.Contrasena;

            // Se validan todos los campos a la vez y el foco va al primero con problemas.
            Action? enfocar = null;

            if (nombre.Length == 0)
            {
                Nombre.MostrarError("Escribí un nombre para identificar el perfil.");
                enfocar ??= () => Nombre.EnfocarCampo();
            }
            else if (_almacen.ExisteNombre(nombre))
            {
                Nombre.MostrarError("Ya hay un perfil con ese nombre en esta computadora.");
                enfocar ??= () => Nombre.EnfocarCampo();
            }

            if (ReglasContrasena.ValidarLongitud(contrasena) is string errorLongitud)
            {
                Contrasena.MostrarError(errorLongitud);
                enfocar ??= () => Contrasena.EnfocarCampo();
            }

            if (ReglasContrasena.ValidarRepeticion(contrasena, repetida) is string errorRepeticion)
            {
                Repetir.MostrarError(errorRepeticion);
                enfocar ??= () => Repetir.EnfocarCampo();
            }

            if (enfocar is not null)
            {
                enfocar();
                return;
            }

            Continuar.IsEnabled = false;
            Cursor = Cursors.Wait;
            try
            {
                (_perfil, _clave) = await Task.Run(() => _almacen.Crear(nombre, contrasena));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Nombre.MostrarError("No se pudo guardar el perfil. Revisá los permisos de la carpeta de Sereno.");
                return;
            }
            finally
            {
                Continuar.IsEnabled = true;
                Cursor = null;
            }

            MostrarPaso2();
        }

        private void MostrarPaso2()
        {
            Contrasena.Limpiar();
            Repetir.Limpiar();

            ClaveTexto.Text = ClaveRecuperacion.Formatear(_clave);
            Paso1.Visibility = Visibility.Collapsed;
            Paso2.Visibility = Visibility.Visible;
            Pasos.Configurar(NombresPasos, 1);
            CopiarClave.Focus();
        }

        private void CopiarClave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ClaveRecuperacion.Formatear(_clave));
                MostrarEstado("Clave copiada al portapapeles.");
            }
            catch (COMException)
            {
                MostrarEstado("No se pudo usar el portapapeles. Probá de nuevo o guardá la clave como archivo.");
            }
        }

        private void GuardarClave_Click(object sender, RoutedEventArgs e)
        {
            if (_perfil is null) return;
            try
            {
                string ruta = _almacen.GuardarClaveEnArchivo(_perfil, _clave);
                MostrarEstado($"Clave guardada en {ruta}");
                AbrirCarpeta.Visibility = Visibility.Visible;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MostrarEstado("No se pudo crear el archivo. Copiá la clave y guardala en otro lugar.");
            }
        }

        private void AbrirCarpeta_Click(object sender, RoutedEventArgs e)
        {
            if (_perfil is null || !Directory.Exists(_perfil.Carpeta)) return;
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_perfil.Carpeta}\"") { UseShellExecute = true });
        }

        private void MostrarEstado(string texto)
        {
            EstadoClave.Text = texto;
            EstadoClave.Visibility = Visibility.Visible;
            Accesibilidad.Anunciar(EstadoClave);
        }

        private void ClaveGuardada_Changed(object sender, RoutedEventArgs e) =>
            Empezar.IsEnabled = ClaveGuardada.IsChecked == true;

        private void Empezar_Click(object sender, RoutedEventArgs e)
        {
            if (_perfil is not null)
                RegistroCompletado?.Invoke(this, new PerfilEventArgs(_perfil));
        }

        private void Volver_Click(object sender, RoutedEventArgs e) =>
            VolverSolicitado?.Invoke(this, EventArgs.Empty);

        private void SinPerfil_Click(object sender, RoutedEventArgs e) =>
            UsoSinPerfilSolicitado?.Invoke(this, EventArgs.Empty);
    }
}
