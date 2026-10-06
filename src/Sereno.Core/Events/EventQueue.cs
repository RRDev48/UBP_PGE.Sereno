using System.Collections.Generic;

namespace Sereno.Core.Events
{
    /// <summary>Eventos encolados en orden de llegada, pendientes de despacho.</summary>
    public sealed class EventQueue
    {
        private readonly Queue<IEvent> _pendientes = new();

        public int Pendientes => _pendientes.Count;

        public void Encolar(IEvent evento) => _pendientes.Enqueue(evento);

        /// <summary>Publica los eventos en orden FIFO. Si un suscriptor falla, los restantes quedan en la cola.</summary>
        public void Despachar(EventBus bus)
        {
            while (_pendientes.Count > 0)
                bus.Publish(_pendientes.Dequeue());
        }
    }
}
