using System;
using System.IO;
using System.Linq;
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
    }
}
