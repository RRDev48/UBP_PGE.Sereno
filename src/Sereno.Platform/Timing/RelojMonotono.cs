using System;
using System.Diagnostics;
using Sereno.Core.Timing;

namespace Sereno.Platform.Timing
{
    /// <summary>Reloj monótono del sistema. Nunca retrocede, a diferencia de la hora de pared.</summary>
    public sealed class RelojMonotono : IClock
    {
        private readonly long _origen = Stopwatch.GetTimestamp();

        public TimeSpan Ahora => Stopwatch.GetElapsedTime(_origen);
    }
}
