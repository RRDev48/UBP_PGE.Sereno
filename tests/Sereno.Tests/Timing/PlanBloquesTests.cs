using System;
using Sereno.Core.Timing;
using Xunit;

namespace Sereno.Tests.Timing
{
    public class PlanBloquesTests
    {
        [Theory]
        [InlineData(4, false)]
        [InlineData(5, true)]
        [InlineData(90, true)]
        [InlineData(91, false)]
        public void EsBloqueValido_RespetaLosLimites(int minutos, bool esperado)
        {
            Assert.Equal(esperado, Duraciones.EsBloqueValido(minutos));
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(30, true)]
        [InlineData(31, false)]
        public void EsPausaValida_RespetaLosLimites(int minutos, bool esperado)
        {
            Assert.Equal(esperado, Duraciones.EsPausaValida(minutos));
        }

        [Fact]
        public void CambiarBloque_ConUnBloqueEnCurso_NoReiniciaElBloqueActual()
        {
            var plan = new PlanBloques(25, 5);
            plan.IniciarBloque();

            plan.CambiarBloque(50);

            Assert.True(plan.BloqueEnCurso);
            Assert.Equal(25, plan.MinutosBloqueEnCurso);
            Assert.Equal(50, plan.MinutosBloque);
        }

        [Fact]
        public void IniciarBloque_DespuesDeUnCambio_UsaLaNuevaDuracion()
        {
            var plan = new PlanBloques(25, 5);
            plan.IniciarBloque();
            plan.CambiarBloque(50);
            plan.TerminarBloque();

            plan.IniciarBloque();

            Assert.Equal(50, plan.MinutosBloqueEnCurso);
        }

        [Fact]
        public void CambiarBloque_FueraDeRango_LanzaExcepcion()
        {
            var plan = new PlanBloques(25, 5);

            Assert.Throws<ArgumentOutOfRangeException>(() => plan.CambiarBloque(91));
            Assert.Equal(25, plan.MinutosBloque);
        }

        [Fact]
        public void CambiarPausa_FueraDeRango_LanzaExcepcion()
        {
            var plan = new PlanBloques(25, 5);

            Assert.Throws<ArgumentOutOfRangeException>(() => plan.CambiarPausa(0));
            Assert.Equal(5, plan.MinutosPausa);
        }
    }
}
