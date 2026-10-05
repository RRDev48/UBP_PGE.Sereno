using System;

namespace Sereno.Core.Timing
{
    /// <summary>
    /// Tiempo transcurrido desde un origen arbitrario, sin saltos hacia atrás. El núcleo no usa
    /// el reloj del sistema: así los tests avanzan un bloque de 25 minutos sin esperar.
    /// </summary>
    public interface IClock
    {
        TimeSpan Ahora { get; }
    }
}
