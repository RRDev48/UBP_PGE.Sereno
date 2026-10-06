using System;
using System.Collections.Generic;
using Sereno.Core.Events;
using Xunit;

namespace Sereno.Tests.Events
{
    public class EventBusTests
    {
        private sealed record PausaIniciada(int Minutos) : IEvent;
        private sealed record BloqueTerminado(string Nombre) : IEvent;

        [Fact]
        public void Publish_SinSuscriptores_NoProduceError()
        {
            var bus = new EventBus();

            bus.Publish(new PausaIniciada(5));
        }

        [Fact]
        public void Publish_EntregaElEventoAlSuscriptorDelMismoTipo()
        {
            var bus = new EventBus();
            var recibidos = new List<int>();
            bus.Subscribe<PausaIniciada>(e => recibidos.Add(e.Minutos));

            bus.Publish(new PausaIniciada(5));
            bus.Publish(new BloqueTerminado("foco"));

            Assert.Equal(new[] { 5 }, recibidos);
        }

        [Fact]
        public void Dispose_DelToken_DejaDeRecibirEventosDeInmediato()
        {
            var bus = new EventBus();
            int llamadas = 0;
            SubscriptionToken token = bus.Subscribe<PausaIniciada>(_ => llamadas++);

            bus.Publish(new PausaIniciada(5));
            token.Dispose();
            bus.Publish(new PausaIniciada(5));

            Assert.Equal(1, llamadas);
        }

        [Fact]
        public void Dispose_DelToken_NoAfectaAOtrosSuscriptores()
        {
            var bus = new EventBus();
            int otro = 0;
            SubscriptionToken token = bus.Subscribe<PausaIniciada>(_ => { });
            bus.Subscribe<PausaIniciada>(_ => otro++);

            token.Dispose();
            bus.Publish(new PausaIniciada(5));

            Assert.Equal(1, otro);
        }

        [Fact]
        public void Dispose_LlamadoDosVeces_NoFalla()
        {
            var bus = new EventBus();
            SubscriptionToken token = bus.Subscribe<PausaIniciada>(_ => { });

            token.Dispose();
            token.Dispose();
        }

        [Fact]
        public void ExcepcionEnUnSuscriptor_NoImpideQueLosDemasReciban()
        {
            var bus = new EventBus();
            int recibidosPorElSegundo = 0;
            bus.Subscribe<PausaIniciada>(_ => throw new InvalidOperationException("falla"));
            bus.Subscribe<PausaIniciada>(_ => recibidosPorElSegundo++);

            AggregateException error = Assert.Throws<AggregateException>(() => bus.Publish(new PausaIniciada(5)));

            Assert.Equal(1, recibidosPorElSegundo);
            Assert.Single(error.InnerExceptions);
        }

        [Fact]
        public void EventQueue_DespachaEnOrdenFifo()
        {
            var bus = new EventBus();
            var orden = new List<string>();
            bus.Subscribe<BloqueTerminado>(e => orden.Add(e.Nombre));
            var cola = new EventQueue();

            cola.Encolar(new BloqueTerminado("primero"));
            cola.Encolar(new BloqueTerminado("segundo"));
            cola.Encolar(new BloqueTerminado("tercero"));
            cola.Despachar(bus);

            Assert.Equal(new[] { "primero", "segundo", "tercero" }, orden);
            Assert.Equal(0, cola.Pendientes);
        }
    }
}
