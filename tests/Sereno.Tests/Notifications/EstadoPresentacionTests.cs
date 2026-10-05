using Sereno.Platform.Notifications;
using Xunit;

namespace Sereno.Tests.Notifications
{
    public class EstadoPresentacionTests
    {
        [Theory]
        [InlineData(1, false)]  // QUNS_NOT_PRESENT
        [InlineData(2, true)]   // QUNS_BUSY
        [InlineData(3, true)]   // QUNS_RUNNING_D3D_FULL_SCREEN
        [InlineData(4, true)]   // QUNS_PRESENTATION_MODE
        [InlineData(5, false)]  // QUNS_ACCEPTS_NOTIFICATIONS
        [InlineData(6, false)]  // QUNS_QUIET_TIME
        [InlineData(7, false)]  // QUNS_APP
        public void EsOcupado_IdentificaLosEstadosQueNoConvienenInterrumpir(int estado, bool esperado)
        {
            Assert.Equal(esperado, EstadoPresentacion.EsOcupado(estado));
        }
    }
}
