using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Sereno.Core.Modelos;
using Sereno.Platform.Storage;

namespace Sereno.Desktop.Vistas
{
    /// <summary>
    /// Inicio de sesión. No navega por su cuenta: dispara eventos y la aplicación decide
    /// qué ventana mostrar a continuación.
    /// </summary>
    public partial class LoginWindow : Window
    {
        private const int IntentosAntesDeSugerirRecuperacion = 3;

        private readonly AlmacenPerfiles _almacen;
        private Perfil _perfil;
        private int _intentosFallidos;

        public event EventHandler<PerfilEventArgs>? SesionIniciada;
        public event EventHandler<PerfilEventArgs>? RecuperacionSolicitada;
        public event EventHandler? CreacionSolicitada;
        public event EventHandler? UsoSinPerfilSolicitado;

        public LoginWindow(AlmacenPerfiles almacen, Perfil perfil)
        {
            InitializeComponent();
            _almacen = almacen;
            _perfil = perfil;

            Contrasena.ContrasenaCambiada += (_, _) => Contrasena.LimpiarError();
            MostrarPerfil();
            Loaded += (_, _) => Contrasena.EnfocarCampo();
        }

        private void MostrarPerfil()
        {
            InicialTexto.Text = _perfil.Inicial;
            NombreTexto.Text = _perfil.Nombre;
            AbrirSinContrasena.IsChecked = _perfil.AbrirSinContrasena;
            CambiarPerfil.Visibility = _almacen.Perfiles.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            AutomationProperties.SetName(Contrasena, $"Contraseña de {_perfil.Nombre}");
            _intentosFallidos = 0;
            Contrasena.Limpiar();
        }

        private async void IniciarSesion_Click(object sender, RoutedEventArgs e)
        {
            string contrasena = Contrasena.Contrasena;
            if (contrasena.Length == 0)
            {
                Contrasena.MostrarError("Escribí tu contraseña para continuar.");
                Contrasena.EnfocarCampo();
                return;
            }

            bool valida;
            IniciarSesion.IsEnabled = false;
            Cursor = Cursors.Wait;
            try
            {
                // PBKDF2 tarda unas décimas: se calcula fuera del hilo de la interfaz.
                valida = await Task.Run(() => _almacen.VerificarContrasena(_perfil, contrasena));
            }
            finally
            {
                IniciarSesion.IsEnabled = true;
                Cursor = null;
            }

            if (!valida)
            {
                _intentosFallidos++;
                Contrasena.MostrarError(MensajeContrasenaIncorrecta());
                Contrasena.SeleccionarTodo();
                return;
            }

            bool abrirSinContrasena = AbrirSinContrasena.IsChecked == true;
            if (_perfil.AbrirSinContrasena != abrirSinContrasena)
            {
                _perfil.AbrirSinContrasena = abrirSinContrasena;
                _almacen.Guardar(_perfil);
            }

            SesionIniciada?.Invoke(this, new PerfilEventArgs(_perfil));
        }

        private string MensajeContrasenaIncorrecta()
        {
            if (_intentosFallidos >= IntentosAntesDeSugerirRecuperacion)
                return "La contraseña no coincide. Si no la recordás, usá \"Olvidé mi contraseña\".";

            return Keyboard.IsKeyToggled(Key.CapsLock)
                ? "La contraseña no coincide. Bloq Mayús está activado."
                : "La contraseña no coincide. Revisá mayúsculas y probá de nuevo.";
        }

        private void CambiarPerfil_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu
            {
                PlacementTarget = CambiarPerfil,
                Placement = PlacementMode.Bottom,
            };

            foreach (Perfil perfil in _almacen.Perfiles)
            {
                var elegido = perfil;
                var item = new MenuItem
                {
                    Header = perfil.Nombre,
                    IsCheckable = true,
                    IsChecked = perfil.Id == _perfil.Id,
                    MinHeight = 36,
                };
                item.Click += (_, _) =>
                {
                    _perfil = elegido;
                    MostrarPerfil();
                    Contrasena.EnfocarCampo();
                };
                menu.Items.Add(item);
            }

            menu.IsOpen = true;
        }

        private void Olvide_Click(object sender, RoutedEventArgs e) =>
            RecuperacionSolicitada?.Invoke(this, new PerfilEventArgs(_perfil));

        private void CrearPerfil_Click(object sender, RoutedEventArgs e) =>
            CreacionSolicitada?.Invoke(this, EventArgs.Empty);

        private void SinPerfil_Click(object sender, RoutedEventArgs e) =>
            UsoSinPerfilSolicitado?.Invoke(this, EventArgs.Empty);
    }
}
