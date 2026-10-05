using System;
using System.Collections.Generic;
using Sereno.Core.Events;
using Sereno.Core.Timing;
using Xunit;

namespace Sereno.Tests.Timing
{
    public class BlockTimerTests
    {
        private sealed class RelojFalso : IClock
        {
            public TimeSpan Ahora { get; set; } = TimeSpan.FromHours(10);

            public void Avanzar(TimeSpan cuanto) => Ahora += cuanto;
        }

        private static (BlockTimer timer, RelojFalso reloj, List<BloqueTerminado> eventos) Preparar()
        {
            var reloj = new RelojFalso();
            var bus = new EventBus();
            var eventos = new List<BloqueTerminado>();
            bus.Subscribe<BloqueTerminado>(eventos.Add);
            return (new BlockTimer(reloj, bus), reloj, eventos);
        }

        [Fact]
        public void Verificar_AntesDeLaDuracion_NoPublicaNada()
        {
            var (timer, reloj, eventos) = Preparar();
            timer.Iniciar(25);

            reloj.Avanzar(TimeSpan.FromMinutes(24) + TimeSpan.FromSeconds(59));
            timer.Verificar();

            Assert.Empty(eventos);
            Assert.Equal(BlockState.EnCurso, timer.Estado);
        }

        [Fact]
        public void Verificar_AlCumplirLaDuracion_PublicaUnaVezConLosMinutos()
        {
            var (timer, reloj, eventos) = Preparar();
            timer.Iniciar(25);

            reloj.Avanzar(TimeSpan.FromMinutes(25));
            timer.Verificar();
            timer.Verificar();

            Assert.Equal(new[] { new BloqueTerminado(25) }, eventos);
            Assert.Equal(BlockState.Detenido, timer.Estado);
        }

        [Fact]
        public void Verificar_DespuesDeUnaSuspension_PublicaUnSoloEventoSinAvisosAcumulados()
        {
            var (timer, reloj, eventos) = Preparar();
            timer.Iniciar(25);

            reloj.Avanzar(TimeSpan.FromHours(3));
            timer.Verificar();
            reloj.Avanzar(TimeSpan.FromMinutes(30));
            timer.Verificar();

            Assert.Single(eventos);
        }

        [Fact]
        public void Detener_ImpideElEventoDelBloqueActual()
        {
            var (timer, reloj, eventos) = Preparar();
            timer.Iniciar(25);

            timer.Detener();
            reloj.Avanzar(TimeSpan.FromMinutes(30));
            timer.Verificar();

            Assert.Empty(eventos);
        }

        [Fact]
        public void Iniciar_DuracionFueraDeRango_LanzaExcepcion()
        {
            var (timer, _, _) = Preparar();

            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Iniciar(4));
            Assert.Equal(BlockState.Detenido, timer.Estado);
        }
    }
}
