using System;
using System.Threading;

namespace Sereno.Core.Events
{
    /// <summary>Devuelve el suscriptor al EventBus al llamar a Dispose. Llamarlo varias veces no hace nada extra.</summary>
    public sealed class SubscriptionToken : IDisposable
    {
        private Action? _darDeBaja;

        public SubscriptionToken(Action darDeBaja) => _darDeBaja = darDeBaja;

        public void Dispose()
        {
            Action? accion = Interlocked.Exchange(ref _darDeBaja, null);
            accion?.Invoke();
        }
    }
}
