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
        public void GuardarDuraciones_FueraDeRango_LanzaExcepcionSinEscribir()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _almacen.GuardarDuraciones(91, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => _almacen.GuardarDuraciones(25, 0));

            Assert.Equal((25, 5), _almacen.ObtenerDuraciones());
        }
    }
}
