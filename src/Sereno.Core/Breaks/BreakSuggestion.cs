using System.Collections.Generic;

namespace Sereno.Core.Breaks
{
    /// <summary>
    /// Micropausa propuesta. Si Pasos está vacío es una acción única descrita por Descripcion;
    /// si no, es una rutina de pasos ordenados.
    /// </summary>
    public sealed record BreakSuggestion(
        string Nombre,
        string Icono,
        int Segundos,
        string Descripcion,
        IReadOnlyList<BreakStep> Pasos)
    {
        public bool EsRutina => Pasos.Count > 0;
    }
}
