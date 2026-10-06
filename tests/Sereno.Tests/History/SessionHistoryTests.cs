using System.Linq;
using Sereno.Core.History;
using Xunit;

namespace Sereno.Tests.History
{
    public class SessionHistoryTests
    {
        [Fact]
        public void Entradas_DevuelveLaMasRecientePrimero()
        {
            var historial = new SessionHistory();

            historial.Registrar(TipoEntrada.BloqueCompletado, 25);
            historial.Registrar(TipoEntrada.PausaTomada, 5);

            Assert.Equal(
                new[] { new EntradaSesion(TipoEntrada.PausaTomada, 5), new EntradaSesion(TipoEntrada.BloqueCompletado, 25) },
                historial.Entradas);
        }

        [Fact]
        public void DeshacerUltima_QuitaSoloLaUltimaEntradaEnOrdenInverso()
        {
            var historial = new SessionHistory();
            historial.Registrar(TipoEntrada.BloqueCompletado, 25);
            historial.Registrar(TipoEntrada.PausaTomada, 5);

            Assert.True(historial.DeshacerUltima());
            Assert.Equal(new[] { new EntradaSesion(TipoEntrada.BloqueCompletado, 25) }, historial.Entradas);
            Assert.True(historial.DeshacerUltima());
            Assert.False(historial.DeshacerUltima());
        }

        [Fact]
        public void RegistrarAviso_MapeaLaAccionAlTipoDeEntrada()
        {
            var historial = new SessionHistory();

            historial.RegistrarAviso(AccionAviso.Pospuesto, 25);
            historial.RegistrarAviso(AccionAviso.Descartado, 25);

            Assert.Equal(1, historial.Contar(TipoEntrada.AvisoPospuesto));
            Assert.Equal(1, historial.Contar(TipoEntrada.AvisoDescartado));
        }

        [Fact]
        public void Contar_SesionNueva_EsCero()
        {
            Assert.Equal(0, new SessionHistory().Contar(TipoEntrada.BloqueCompletado));
        }
    }
}
