using System;
using System.IO;
using System.Threading;
using System.Windows;
using Sereno.Core.Modelos;
using Sereno.Desktop.Servicios;
using Sereno.Desktop.Vistas;
using Sereno.Platform.Storage;
using Sereno.Platform.Tray;

namespace Sereno.Desktop
{
    /// <summary>
    /// Punto de entrada y coordinador del flujo de acceso.
    ///
    /// Las ventanas no se conocen entre sí: cada una dispara eventos (SesionIniciada,
    /// RecuperacionSolicitada, RegistroCompletado...) y la aplicación decide qué mostrar.
    ///
    ///   Splash ──► Crear perfil (primer uso)
    ///          ──► Iniciar sesión (hay perfil)
    ///          ──► Bandeja (el perfil abre sin contraseña)
    /// </summary>
    public partial class App : Application
    {
        private const string NombreMutex = "Sereno.InstanciaUnica";

        private Mutex? _instanciaUnica;
        private AlmacenPerfiles _almacen = null!;
        private BandejaService _bandeja = null!;
        private Window? _ventanaActual;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _instanciaUnica = new Mutex(true, NombreMutex, out bool esPrimera);
            if (!esPrimera)
            {
                MessageBox.Show("Sereno ya está abierto. Lo vas a encontrar en la bandeja del sistema.",
                    "Sereno", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            TemaService.Aplicar(this);

            _almacen = new AlmacenPerfiles(Rutas.Base);
            _bandeja = new BandejaService(ObtenerStreamDelIcono());
            _bandeja.CierreDeSesionSolicitado += Bandeja_CierreDeSesionSolicitado;
            _bandeja.SalidaSolicitada += (_, _) => Shutdown();

            var splash = new SplashWindow(_almacen);
            splash.CargaCompleta += (_, _) => Decidir();
            Mostrar(splash);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _bandeja?.Dispose();
            _instanciaUnica?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Sereno.Platform no conoce recursos empaquetados de WPF, así que el ícono de la
        /// bandeja se abre acá y se le pasa a BandejaService como Stream.
        /// </summary>
        private static Stream? ObtenerStreamDelIcono() =>
            GetResourceStream(new Uri("pack://application:,,,/Recursos/sereno.ico"))?.Stream;

        // ---------- Decisiones de navegación ----------

        private void Decidir()
        {
            var perfiles = _almacen.Perfiles;
            if (perfiles.Count == 0)
            {
                MostrarRegistro(primerUso: true);
                return;
            }

            Perfil perfil = _almacen.UltimoPerfil() ?? perfiles[0];
            if (perfil.AbrirSinContrasena)
                Entrar(perfil);
            else
                MostrarLogin(perfil);
        }

        private void MostrarLogin(Perfil perfil)
        {
            var login = new LoginWindow(_almacen, perfil);
            login.SesionIniciada += (_, a) => Entrar(a.Perfil);
            login.RecuperacionSolicitada += (_, a) => MostrarRecuperar(a.Perfil);
            login.CreacionSolicitada += (_, _) => MostrarRegistro(primerUso: false);
            login.UsoSinPerfilSolicitado += (_, _) => EntrarSinPerfil();
            Mostrar(login);
        }

        private void MostrarRegistro(bool primerUso)
        {
            var registro = new RegistroWindow(_almacen, primerUso);
            registro.RegistroCompletado += (_, a) => Entrar(a.Perfil);
            registro.VolverSolicitado += (_, _) => Decidir();
            registro.UsoSinPerfilSolicitado += (_, _) => EntrarSinPerfil();
            Mostrar(registro);
        }

        private void MostrarRecuperar(Perfil perfil)
        {
            var recuperar = new RecuperarWindow(_almacen, perfil);
            recuperar.VolverSolicitado += (_, _) => MostrarLogin(perfil);
            Mostrar(recuperar);
        }

        private void EntrarSinPerfil()
        {
            try
            {
                Entrar(_almacen.ObtenerCompartido());
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                MessageBox.Show("No se pudo preparar el perfil compartido. Revisá los permisos de la carpeta de Sereno.",
                    "Sereno", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>Sesión iniciada: Sereno pasa a la bandeja y no queda ninguna ventana abierta.</summary>
        private void Entrar(Perfil perfil)
        {
            _almacen.RecordarUltimo(perfil);
            _bandeja.Mostrar(perfil);
            CerrarVentanaActual();

            // Acá se conecta el resto de Sereno (temporizador de pausas, historial del perfil).
        }

        private void Bandeja_CierreDeSesionSolicitado(object? sender, PerfilEventArgs e)
        {
            _bandeja.Ocultar();
            MostrarAccesoTrasCerrarSesion(e.Perfil);
        }

        /// <summary>
        /// Al cerrar sesión siempre se muestra una pantalla de acceso, aunque el perfil
        /// tenga activado "abrir sin contraseña".
        /// </summary>
        private void MostrarAccesoTrasCerrarSesion(Perfil anterior)
        {
            if (!anterior.EsCompartido)
            {
                MostrarLogin(anterior);
                return;
            }

            var perfiles = _almacen.Perfiles;
            if (perfiles.Count == 0)
                MostrarRegistro(primerUso: true);
            else
                MostrarLogin(_almacen.UltimoPerfil() ?? perfiles[0]);
        }

        // ---------- Manejo de ventanas ----------

        private void Mostrar(Window nueva)
        {
            Window? anterior = _ventanaActual;
            _ventanaActual = nueva;
            nueva.Closed += Ventana_Closed;
            nueva.Show();
            nueva.Activate();
            anterior?.Close();
        }

        private void CerrarVentanaActual()
        {
            Window? anterior = _ventanaActual;
            _ventanaActual = null;
            anterior?.Close();
        }

        /// <summary>
        /// Si la persona cierra con la X la ventana visible y Sereno no está en la bandeja,
        /// la aplicación termina (si no, quedaría un proceso sin interfaz).
        /// </summary>
        private void Ventana_Closed(object? sender, EventArgs e)
        {
            if (!ReferenceEquals(sender, _ventanaActual)) return;
            _ventanaActual = null;
            if (!_bandeja.Visible) Shutdown();
        }
    }
}
