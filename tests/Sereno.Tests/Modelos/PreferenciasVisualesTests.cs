using Sereno.Core.Modelos;
using Xunit;

namespace Sereno.Tests.Modelos
{
    public class PreferenciasVisualesTests
    {
        [Theory]
        [InlineData(100, true)]
        [InlineData(125, true)]
        [InlineData(150, true)]
        [InlineData(110, false)]
        [InlineData(0, false)]
        public void EsEscalaValida_SoloAceptaLosTresPorcentajes(int escala, bool esperado)
        {
            Assert.Equal(esperado, PreferenciasVisuales.EsEscalaValida(escala));
        }

        [Fact]
        public void ValoresPorDefecto_NoActivanNingunaPreferencia()
        {
            var preferencias = new PreferenciasVisuales();

            Assert.False(preferencias.FuenteLectura);
            Assert.False(preferencias.AltoContraste);
            Assert.False(preferencias.EspaciadoAmplio);
            Assert.Equal(100, preferencias.Escala);
        }
    }
}
