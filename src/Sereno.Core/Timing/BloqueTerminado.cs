using Sereno.Core.Events;

namespace Sereno.Core.Timing
{
    /// <summary>Se publica una sola vez por bloque, cuando se cumple la duración configurada.</summary>
    public sealed record BloqueTerminado(int Minutos) : IEvent;
}
