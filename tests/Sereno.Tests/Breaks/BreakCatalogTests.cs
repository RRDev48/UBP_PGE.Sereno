using System.Linq;
using Sereno.Core.Breaks;
using Xunit;

namespace Sereno.Tests.Breaks
{
    public class BreakCatalogTests
    {
        [Fact]
        public void Todas_TieneAlMenosSeisMicropausasConNombreIconoYDuracion()
        {
            Assert.True(BreakCatalog.Todas.Count >= 6);
            Assert.All(BreakCatalog.Todas, s =>
            {
                Assert.False(string.IsNullOrWhiteSpace(s.Nombre));
                Assert.False(string.IsNullOrWhiteSpace(s.Icono));
                Assert.True(s.Segundos > 0);
            });
        }

        [Fact]
        public void Todas_IncluyeElDescansoVisual20_20_20()
        {
            Assert.Contains(BreakCatalog.Todas, s => s.Nombre == "Descanso visual 20-20-20" && s.Segundos == 20);
        }

        [Fact]
        public void Para_FiltraLasMicropausasQueEntranEnLaPausaConfigurada()
        {
            var cortas = BreakCatalog.Para(1);

            Assert.All(cortas, s => Assert.True(s.Segundos <= 60));
            Assert.DoesNotContain(BreakCatalog.Todas, s => s.Segundos > 60 && cortas.Contains(s));
        }

        [Fact]
        public void Rutina_TieneSusPasosEnOrden()
        {
            BreakSuggestion rutina = BreakCatalog.Todas.Single(s => s.EsRutina);

            Assert.Equal(3, rutina.Pasos.Count);
            Assert.All(rutina.Pasos, p => Assert.True(p.Segundos > 0));
        }
    }
}
