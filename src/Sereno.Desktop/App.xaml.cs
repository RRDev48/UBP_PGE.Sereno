using System;
using System.IO;
using System.Threading;
using System.Windows;
using Sereno.Core.Events;
using Sereno.Core.History;
using Sereno.Core.Modelos;
using Sereno.Core.Timing;
using Sereno.Desktop.Servicios;
using Sereno.Desktop.Vistas;
using Sereno.Platform.Speech;
using Sereno.Platform.Storage;
using Sereno.Platform.Timing;
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
        private readonly EventBus _bus = new();
        private MotorDeBloques _motor = null!;
        private AvisoFinDeBloque _aviso = null!;
        private RecordatorioMascota _recordatorio = null!;
        private readonly SessionHistory _historial = new();
        private LectorVoz _lector = null!;
        private VozModerna _vozModerna = null!;
        private SubscriptionToken? _avisoFinDeBloque;
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

            // Tema del sistema hasta que se lean las preferencias guardadas.
            TemaService.Aplicar(this, new PreferenciasVisuales());

            _almacen = new AlmacenPerfiles(Rutas.Base);
            _bandeja = new BandejaService(ObtenerStreamDelIcono());
            _bandeja.CierreDeSesionSolicitado += Bandeja_CierreDeSesionSolicitado;
            _bandeja.SalidaSolicitada += (_, _) => Shutdown();
            _bandeja.ConfiguracionSolicitada += (_, _) => Mostrar(new ConfiguracionWindow(_almacen));
            _motor = new MotorDeBloques(new BlockTimer(new RelojMonotono(), _bus));
            _motor.EstadoCambio += (_, _) => _bandeja.ActualizarEstado(_motor.Estado, _motor.MinutosRestantes);
            _recordatorio = new RecordatorioMascota(_motor);
            _recordatorio.Recordatorio += (_, minutos) => MostrarRecordatorio(minutos);
            _bandeja.PausaSolicitada += (_, _) => MostrarPausa();
            _bandeja.ResumenSolicitado += (_, _) => Mostrar(new ResumenWindow(_historial));
            _lector = new LectorVoz();
            _vozModerna = new VozModerna();
            _aviso = new AvisoFinDeBloque(_historial, _almacen, _bandeja, _lector, _vozModerna);
            _aviso.PausaSolicitada += (_, _) => MostrarPausa();
            _avisoFinDeBloque = _bus.Subscribe<BloqueTerminado>(e =>
            {
                _historial.Registrar(TipoEntrada.BloqueCompletado, e.Minutos);
                _aviso.Mostrar(e.Minutos);
            });

            var splash = new SplashWindow(_almacen);
            splash.CargaCompleta += (_, _) =>
            {
                TemaService.Aplicar(this, _almacen.ObtenerPreferencias());
                Decidir();
            };
            Mostrar(splash);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _lector?.Dispose();
            _vozModerna?.Dispose();
            _motor?.Dispose();
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
            login.AdministrarPerfilesSolicitado += (_, _) => MostrarAdministrarPerfiles();
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

        private void MostrarPausa() => Mostrar(new PausaWindow(_almacen.ObtenerDuraciones().MinutosPausa, _historial));

        /// <summary>
        /// No pasa por Mostrar(): es un aviso flotante que convive con lo que esté abierto,
        /// como AvisoBreveWindow, no una pantalla que reemplaza a la anterior.
        /// </summary>
        private void MostrarRecordatorio(int minutosRestantes)
        {
            bool animar = TemaService.AnimacionesActivas && !_almacen.ObtenerPreferenciasAviso().BajoEstimulo;
            new RecordatorioWindow(minutosRestantes, animar).Show();
        }

        private void MostrarAdministrarPerfiles()
        {
            var administrar = new AdministrarPerfilesWindow(_almacen);
            administrar.VolverSolicitado += (_, _) => Decidir();
            Mostrar(administrar);
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
            _motor.Iniciar(_almacen.ObtenerDuraciones().MinutosBloque);
            CerrarVentanaActual();
            bool animar = TemaService.AnimacionesActivas && !_almacen.ObtenerPreferenciasAviso().BajoEstimulo;
            new AvisoBreveWindow("Sereno está activo",
                "Queda en la bandeja del sistema. Hacé clic derecho en el ícono para ver las opciones.", animar).Show();

            // Acá se conecta el resto de Sereno (temporizador de pausas, historial del perfil).
        }

        private void Bandeja_CierreDeSesionSolicitado(object? sender, PerfilEventArgs e)
        {
            _motor.Detener();
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
