using System.Collections.Generic;

namespace Sereno.Core.History
{
    public enum AccionAviso
    {
        Pospuesto,
        Descartado,
    }

    public sealed record AccionAvisoRegistrada(AccionAviso Accion, int MinutosBloque);

    /// <summary>Acciones sobre los avisos de la sesión actual, en el orden en que ocurrieron.</summary>
    public sealed class SessionHistory
    {
        private readonly List<AccionAvisoRegistrada> _acciones = new();

        public IReadOnlyList<AccionAvisoRegistrada> Acciones => _acciones;

        public void RegistrarAccion(AccionAviso accion, int minutosBloque) =>
            _acciones.Add(new AccionAvisoRegistrada(accion, minutosBloque));
    }
}
