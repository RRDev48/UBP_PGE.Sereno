using System;
using System.IO;
using System.Linq;
using Sereno.Core.Modelos;
using Sereno.Platform.Storage;
using Xunit;

namespace Sereno.Tests.Storage
{
    /// <summary>
    /// Cada test usa su propia carpeta temporal (nunca %APPDATA%) y la borra al terminar.
    /// </summary>
    public class AlmacenPerfilesTests : IDisposable
    {
        private readonly string _carpetaTemporal;
        private readonly AlmacenPerfiles _almacen;

        public AlmacenPerfilesTests()
        {
            _carpetaTemporal = Path.Combine(Path.GetTempPath(), "Sereno.Tests-" + Guid.NewGuid().ToString("N"));
            _almacen = new AlmacenPerfiles(_carpetaTemporal);
        }

        public void Dispose()
        {
            if (Directory.Exists(_carpetaTemporal))
                Directory.Delete(_carpetaTemporal, recursive: true);
        }

        [Fact]
        public void Crear_YVolverACargarEnOtraInstancia_RecuperaElPerfil()
        {
            _almacen.Crear("Lucía", "contrasena123");

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();

            Assert.Single(recargado.Perfiles);
            Assert.Equal("Lucía", recargado.Perfiles[0].Nombre);
        }

        [Fact]
        public void Crear_LaContrasenaVerificaCorrectamenteTrasRecargar()
        {
            _almacen.Crear("Lucía", "contrasena123");

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();
            var perfil = recargado.Perfiles[0];

            Assert.True(recargado.VerificarContrasena(perfil, "contrasena123"));
            Assert.False(recargado.VerificarContrasena(perfil, "otra-cosa"));
        }

        [Fact]
        public void CambiarContrasena_LaNuevaVerificaYLaViejaDejaDeHacerlo()
        {
            var (perfil, _) = _almacen.Crear("Lucía", "contrasena123");

            _almacen.CambiarContrasena(perfil, "nueva-contrasena");

            Assert.True(_almacen.VerificarContrasena(perfil, "nueva-contrasena"));
            Assert.False(_almacen.VerificarContrasena(perfil, "contrasena123"));
        }

        [Fact]
        public void Cargar_ConUnPerfilJsonCorrupto_NoRompeLaCargaDelResto()
        {
            _almacen.Crear("Lucía", "contrasena123");
            _almacen.Crear("Marco", "otraContrasena1");

            string carpetaPerfiles = Path.Combine(_carpetaTemporal, "Perfiles");
            string carpetaMarco = Directory.EnumerateDirectories(carpetaPerfiles)
                .Single(c => Path.GetFileName(c).StartsWith("marco", StringComparison.Ordinal));
            File.WriteAllText(Path.Combine(carpetaMarco, AlmacenPerfiles.ArchivoPerfil), "{ esto no es json válido");

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();

            Assert.Single(recargado.Perfiles);
            Assert.Equal("Lucía", recargado.Perfiles[0].Nombre);
        }

        [Fact]
        public void ObtenerDuraciones_SinConfiguracionGuardada_DevuelveLosValoresPorDefecto()
        {
            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();

            Assert.Equal((25, 5), recargado.ObtenerDuraciones());
        }

        [Fact]
        public void GuardarDuraciones_SeConservaAlReabrirLaAplicacion()
        {
            _almacen.GuardarDuraciones(40, 10);

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();

            Assert.Equal((40, 10), recargado.ObtenerDuraciones());
        }

        [Fact]
        public void RecordarUltimo_NoPierdeLasDuracionesGuardadas()
        {
            _almacen.GuardarDuraciones(40, 10);
            var (perfil, _) = _almacen.Crear("Lucía", "contrasena123");

            _almacen.RecordarUltimo(perfil);

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();
            Assert.Equal((40, 10), recargado.ObtenerDuraciones());
            Assert.Equal(perfil.Id, recargado.UltimoPerfil()?.Id);
        }

        [Fact]
        public void ObtenerPreferencias_SinConfiguracionGuardada_NoActivaNada()
        {
            var preferencias = _almacen.ObtenerPreferencias();

            Assert.False(preferencias.FuenteLectura);
            Assert.False(preferencias.AltoContraste);
        }

        [Fact]
        public void GuardarPreferencias_SeConservanAlReabrirLaAplicacion()
        {
            _almacen.GuardarPreferencias(new PreferenciasVisuales { FuenteLectura = true, AltoContraste = true });

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();

            PreferenciasVisuales preferencias = recargado.ObtenerPreferencias();
            Assert.True(preferencias.FuenteLectura);
            Assert.True(preferencias.AltoContraste);
        }

        [Fact]
        public void GuardarPreferencias_EscalaYEspaciadoSeConservanAlReabrir()
        {
            _almacen.GuardarPreferencias(new PreferenciasVisuales { EspaciadoAmplio = true, Escala = 150 });

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();

            PreferenciasVisuales preferencias = recargado.ObtenerPreferencias();
            Assert.True(preferencias.EspaciadoAmplio);
            Assert.Equal(150, preferencias.Escala);
        }

        [Fact]
        public void GuardarPreferencias_EscalaInvalida_LanzaExcepcionSinEscribir()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _almacen.GuardarPreferencias(new PreferenciasVisuales { Escala = 110 }));

            Assert.Equal(100, _almacen.ObtenerPreferencias().Escala);
        }

        [Fact]
        public void GuardarPreferenciasAviso_SeConservanAlReabrir()
        {
            _almacen.GuardarPreferenciasAviso(new PreferenciasAviso { Canal = CanalAviso.SoloSistema, BajoEstimulo = true });

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();

            PreferenciasAviso preferencias = recargado.ObtenerPreferenciasAviso();
            Assert.Equal(CanalAviso.SoloSistema, preferencias.Canal);
            Assert.True(preferencias.BajoEstimulo);
        }

        [Fact]
        public void ExportarTexto_IncluyePerfilesYConfiguracionSinSecretos()
        {
            var (perfil, _) = _almacen.Crear("Lucía", "contrasena123");

            string texto = _almacen.ExportarTexto();

            Assert.Contains("Lucía", texto);
            Assert.Contains("Bloque: 25 minutos", texto);
            Assert.DoesNotContain(perfil.HashContrasena, texto);
            Assert.DoesNotContain(perfil.HashClave, texto);
            Assert.DoesNotContain(perfil.SalContrasena, texto);
        }

        [Fact]
        public void BorrarTodo_EliminaLaCarpetaDeSereno()
        {
            _almacen.Crear("Lucía", "contrasena123");

            _almacen.BorrarTodo();

            Assert.False(Directory.Exists(_carpetaTemporal));
            Assert.Empty(_almacen.Perfiles);
        }

        [Fact]
        public void GuardarPreferenciasAviso_TiempoVisibleSeConservaAlReabrir()
        {
            _almacen.GuardarPreferenciasAviso(new PreferenciasAviso { SegundosVisible = 45 });

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();

            Assert.Equal(45, recargado.ObtenerPreferenciasAviso().SegundosVisible);
        }

        [Fact]
        public void GuardarPreferenciasAviso_TiempoVisibleFueraDeRango_LanzaExcepcion()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _almacen.GuardarPreferenciasAviso(new PreferenciasAviso { SegundosVisible = 5 }));
            Assert.Equal(10, _almacen.ObtenerPreferenciasAviso().SegundosVisible);
        }

        [Fact]
        public void RecordarUltimo_NoPierdeLasPreferenciasGuardadas()
        {
            _almacen.GuardarPreferencias(new PreferenciasVisuales { AltoContraste = true });
            var (perfil, _) = _almacen.Crear("Lucía", "contrasena123");

            _almacen.RecordarUltimo(perfil);

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();
            Assert.True(recargado.ObtenerPreferencias().AltoContraste);
        }

        [Fact]
        public void GuardarPreferenciasAviso_VozIdSeConservaAlReabrir()
        {
            _almacen.GuardarPreferenciasAviso(new PreferenciasAviso { VozId = "Microsoft Pablo" });

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();

            Assert.Equal("Microsoft Pablo", recargado.ObtenerPreferenciasAviso().VozId);
        }

        [Fact]
        public void GuardarPreferenciasAviso_AvisarDistraccionSeConservaAlReabrir()
        {
            _almacen.GuardarPreferenciasAviso(new PreferenciasAviso { AvisarDistraccion = true });

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();

            Assert.True(recargado.ObtenerPreferenciasAviso().AvisarDistraccion);
        }

        [Fact]
        public void RenombrarPerfil_CambiaElNombreYPersisteTrasRecargar()
        {
            var (perfil, _) = _almacen.Crear("Lucía", "contrasena123");

            _almacen.RenombrarPerfil(perfil, "Lu");

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();
            Assert.Equal("Lu", recargado.Perfiles.Single().Nombre);
        }

        [Fact]
        public void RenombrarPerfil_NoTocaLaContrasenaNiLaClave()
        {
            var (perfil, _) = _almacen.Crear("Lucía", "contrasena123");
            string hashAntes = perfil.HashContrasena;
            string hashClaveAntes = perfil.HashClave;

            _almacen.RenombrarPerfil(perfil, "Lu");

            Assert.Equal(hashAntes, perfil.HashContrasena);
            Assert.Equal(hashClaveAntes, perfil.HashClave);
            Assert.True(_almacen.VerificarContrasena(perfil, "contrasena123"));
        }

        [Fact]
        public void RenombrarPerfil_ConNombreVacio_LanzaExcepcion()
        {
            var (perfil, _) = _almacen.Crear("Lucía", "contrasena123");

            Assert.Throws<ArgumentException>(() => _almacen.RenombrarPerfil(perfil, "   "));
            Assert.Equal("Lucía", perfil.Nombre);
        }

        [Fact]
        public void RenombrarPerfil_ConNombreDuplicado_LanzaExcepcion()
        {
            var (perfil, _) = _almacen.Crear("Lucía", "contrasena123");
            _almacen.Crear("Marco", "otraContrasena1");

            Assert.Throws<InvalidOperationException>(() => _almacen.RenombrarPerfil(perfil, "marco"));
        }

        [Fact]
        public void EliminarPerfil_BorraLaCarpetaYDejaDeAparecer()
        {
            var (perfil, _) = _almacen.Crear("Lucía", "contrasena123");
            string carpeta = perfil.Carpeta;

            _almacen.EliminarPerfil(perfil);

            Assert.False(Directory.Exists(carpeta));
            Assert.Empty(_almacen.Perfiles);

            var recargado = new AlmacenPerfiles(_carpetaTemporal);
            recargado.Cargar();
            Assert.Empty(recargado.Perfiles);
        }

        [Fact]
        public void EliminarPerfil_NoAfectaAOtrosPerfiles()
        {
            var (lucia, _) = _almacen.Crear("Lucía", "contrasena123");
            _almacen.Crear("Marco", "otraContrasena1");

            _almacen.EliminarPerfil(lucia);

            Assert.Single(_almacen.Perfiles);
            Assert.Equal("Marco", _almacen.Perfiles[0].Nombre);
        }

        [Fact]
        public void EliminarPerfil_DelCompartido_LanzaExcepcion()
        {
            Perfil compartido = _almacen.ObtenerCompartido();

            Assert.Throws<InvalidOperationException>(() => _almacen.EliminarPerfil(compartido));
            Assert.True(Directory.Exists(compartido.Carpeta));
        }

        [Fact]
        public void GuardarDuraciones_FueraDeRango_LanzaExcepcionSinEscribir()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _almacen.GuardarDuraciones(91, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => _almacen.GuardarDuraciones(25, 0));

            Assert.Equal((25, 5), _almacen.ObtenerDuraciones());
        }
    }
}
