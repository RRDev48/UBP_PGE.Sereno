using Sereno.Core.Modelos;
using Xunit;

namespace Sereno.Tests.Modelos
{
    public class CanalesAvisoTests
    {
        [Theory]
        [InlineData(CanalAviso.SoloVisual, false, true)]
        [InlineData(CanalAviso.SoloHablado, true, false)]
        [InlineData(CanalAviso.VisualYHablado, true, true)]
        [InlineData(CanalAviso.SoloSistema, false, false)]
        public void UsaVozYUsaVentana_SegunElCanal(CanalAviso canal, bool voz, bool ventana)
        {
            Assert.Equal(voz, CanalesAviso.UsaVoz(canal));
            Assert.Equal(ventana, CanalesAviso.UsaVentana(canal));
        }

        [Fact]
        public void Efectivo_SinVozEnEspanol_CaeAVisual()
        {
            Assert.Equal(CanalAviso.SoloVisual, CanalesAviso.Efectivo(CanalAviso.SoloHablado, hayVozEnEspanol: false));
            Assert.Equal(CanalAviso.SoloVisual, CanalesAviso.Efectivo(CanalAviso.VisualYHablado, hayVozEnEspanol: false));
        }

        [Fact]
        public void Efectivo_ConVozEnEspanol_MantieneElCanal()
        {
            Assert.Equal(CanalAviso.SoloHablado, CanalesAviso.Efectivo(CanalAviso.SoloHablado, hayVozEnEspanol: true));
            Assert.Equal(CanalAviso.SoloSistema, CanalesAviso.Efectivo(CanalAviso.SoloSistema, hayVozEnEspanol: false));
        }
    }
}
