using System;
using System.Collections.Generic;
using System.Linq;

namespace Sereno.Core.Events
{
    /// <summary>
    /// Despachador de eventos de dominio. Es propio y no usa eventos de .NET a propósito:
    /// así Sereno.Core no depende de la UI y se puede testear sin abrir ventanas.
    /// Una excepción en un suscriptor no impide que los demás reciban el evento; al final
    /// se relanza como AggregateException.
    /// </summary>
    public sealed class EventBus
    {
        private readonly object _candado = new();
        private readonly Dictionary<Type, List<Suscripcion>> _suscripciones = new();

        public SubscriptionToken Subscribe<T>(Action<T> manejador) where T : IEvent
        {
            var suscripcion = new Suscripcion(e => manejador((T)e));
            lock (_candado)
            {
                if (!_suscripciones.TryGetValue(typeof(T), out List<Suscripcion>? lista))
                {
                    lista = new List<Suscripcion>();
                    _suscripciones[typeof(T)] = lista;
                }
                lista.Add(suscripcion);
            }

            return new SubscriptionToken(() =>
            {
                suscripcion.Activa = false;
                lock (_candado)
                    _suscripciones[typeof(T)].Remove(suscripcion);
            });
        }

        public void Publish(IEvent evento)
        {
            List<Suscripcion> copia;
            lock (_candado)
            {
                if (!_suscripciones.TryGetValue(evento.GetType(), out List<Suscripcion>? lista))
                    return;
                copia = lista.ToList();
            }

            List<Exception>? errores = null;
            foreach (Suscripcion suscripcion in copia)
            {
                if (!suscripcion.Activa) continue;
                try
                {
                    suscripcion.Manejar(evento);
                }
                catch (Exception ex)
                {
                    (errores ??= new List<Exception>()).Add(ex);
                }
            }

            if (errores is not null)
                throw new AggregateException("Falló al menos un suscriptor del evento.", errores);
        }

        private sealed class Suscripcion
        {
            public Suscripcion(Action<IEvent> manejar) => Manejar = manejar;

            public Action<IEvent> Manejar { get; }
            public volatile bool Activa = true;
        }
    }
}
