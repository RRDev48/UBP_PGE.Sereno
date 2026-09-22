using System.Linq;
using Sereno.Core.Acceso;
using Xunit;

namespace Sereno.Tests.Acceso
{
    public class ClaveRecuperacionTests
    {
        private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        [Fact]
        public void Generar_Devuelve16CaracteresDelAlfabetoPermitido()
        {
            string clave = ClaveRecuperacion.Generar();

            Assert.Equal(16, clave.Length);
            Assert.All(clave, c => Assert.Contains(c, Alfabeto));
        }

        [Theory]
        [InlineData("abcd-efgh-ijkl-mnop", "ABCDEFGHIJKLMNOP")]
        [InlineData("ABCD EFGH", "ABCDEFGH")]
        [InlineData("abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOP")]
        public void Normalizar_SacaGuionesPasaAMayusculasYCortaEn16(string entrada, string esperado)
        {
            string normalizada = ClaveRecuperacion.Normalizar(entrada);

            Assert.Equal(esperado, normalizada);
        }

        [Fact]
        public void Normalizar_TextoVacio_DevuelveVacio()
        {
            Assert.Equal(string.Empty, ClaveRecuperacion.Normalizar(string.Empty));
            Assert.Equal(string.Empty, ClaveRecuperacion.Normalizar(null));
        }

        [Fact]
        public void Formatear_AgrupaDeA4()
        {
            string formateada = ClaveRecuperacion.Formatear("ABCDEFGHIJKLMNOP");

            Assert.Equal("ABCD-EFGH-IJKL-MNOP", formateada);
        }
    }
}
