using Sereno.Core.History;
using Xunit;

namespace Sereno.Tests.History
{
    public class SessionHistoryTests
    {
        [Fact]
        public void RegistrarAccion_ConservaElOrdenDeLasAcciones()
        {
            var historial = new SessionHistory();

            historial.RegistrarAccion(AccionAviso.Pospuesto, 25);
            historial.RegistrarAccion(AccionAviso.Descartado, 50);

            Assert.Equal(
                new[]
                {
                    new AccionAvisoRegistrada(AccionAviso.Pospuesto, 25),
                    new AccionAvisoRegistrada(AccionAviso.Descartado, 50),
                },
                historial.Acciones);
        }

        [Fact]
        public void Acciones_SesionNueva_EstaVacia()
        {
            Assert.Empty(new SessionHistory().Acciones);
        }
    }
}
