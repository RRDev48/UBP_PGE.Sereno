using System.Collections.Generic;
using System.Linq;

namespace Sereno.Core.History
{
    public enum AccionAviso
    {
        Pospuesto,
        Descartado,
    }

    public enum TipoEntrada
    {
        BloqueCompletado,
        PausaTomada,
        AvisoPospuesto,
        AvisoDescartado,
    }

    public sealed record EntradaSesion(TipoEntrada Tipo, int Minutos);

    /// <summary>
    /// Registro de lo que pasó en la sesión actual. Deshacer quita la última entrada registrada.
    /// Vive solo en memoria: no se persiste entre sesiones.
    /// </summary>
    public sealed class SessionHistory
    {
        private readonly Pila<EntradaSesion> _pila = new();

        /// <summary>Entradas de la más reciente a la más antigua.</summary>
        public IReadOnlyList<EntradaSesion> Entradas => _pila.DelMasReciente();

        public void Registrar(TipoEntrada tipo, int minutos) => _pila.Apilar(new EntradaSesion(tipo, minutos));

        public void RegistrarAviso(AccionAviso accion, int minutosBloque) => Registrar(
            accion == AccionAviso.Pospuesto ? TipoEntrada.AvisoPospuesto : TipoEntrada.AvisoDescartado,
            minutosBloque);

        public bool DeshacerUltima() => _pila.Desapilar(out _);

        public int Contar(TipoEntrada tipo) => Entradas.Count(e => e.Tipo == tipo);
    }
}
