using Sereno.Core.Acceso;
using Xunit;

namespace Sereno.Tests.Acceso
{
    public class ReglasContrasenaTests
    {
        [Theory]
        [InlineData("", NivelFuerza.Vacia)]
        [InlineData("abc123", NivelFuerza.Debil)]           // menos de 8 caracteres
        [InlineData("abcdefgh", NivelFuerza.Debil)]          // 8 caracteres pero un solo punto (solo letras)
        [InlineData("abcdefg1", NivelFuerza.Aceptable)]      // 8 caracteres, letras y dígito
        [InlineData("abcdefgh1!", NivelFuerza.Fuerte)]       // 10 caracteres, letras, dígito y símbolo
        public void Evaluar_ClasificaLaFuerzaSegunLosCriterios(string contrasena, NivelFuerza esperado)
        {
            Assert.Equal(esperado, ReglasContrasena.Evaluar(contrasena));
        }

        [Theory]
        [InlineData(NivelFuerza.Vacia, "sin completar")]
        [InlineData(NivelFuerza.Debil, "débil")]
        [InlineData(NivelFuerza.Aceptable, "aceptable")]
        [InlineData(NivelFuerza.Fuerte, "fuerte")]
        public void Describir_DevuelveElTextoParaCadaNivel(NivelFuerza nivel, string esperado)
        {
            Assert.Equal(esperado, ReglasContrasena.Describir(nivel));
        }

        [Fact]
        public void ValidarLongitud_ConLaMinimaRequerida_DevuelveNull()
        {
            Assert.Null(ReglasContrasena.ValidarLongitud("12345678"));
        }

        [Fact]
        public void ValidarLongitud_ConUnCaracterDeMenos_UsaSingular()
        {
            Assert.Equal("Falta 1 carácter para llegar a 8.", ReglasContrasena.ValidarLongitud("1234567"));
        }

        [Fact]
        public void ValidarLongitud_ConVariosCaracteresDeMenos_UsaPlural()
        {
            Assert.Equal("Faltan 3 caracteres para llegar a 8.", ReglasContrasena.ValidarLongitud("12345"));
        }

        [Fact]
        public void ValidarRepeticion_ConLasDosIguales_DevuelveNull()
        {
            Assert.Null(ReglasContrasena.ValidarRepeticion("clave123", "clave123"));
        }

        [Fact]
        public void ValidarRepeticion_ConLasDosDistintas_DevuelveMensaje()
        {
            Assert.Equal("Las dos contraseñas tienen que ser iguales.",
                ReglasContrasena.ValidarRepeticion("clave123", "otra-clave"));
        }

        [Fact]
        public void ValidarRepeticion_ConLaRepetidaVacia_DevuelveMensaje()
        {
            Assert.Equal("Las dos contraseñas tienen que ser iguales.",
                ReglasContrasena.ValidarRepeticion("clave123", ""));
        }
    }
}
