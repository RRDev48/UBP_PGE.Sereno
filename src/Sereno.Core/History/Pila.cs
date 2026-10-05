using System.Collections.Generic;

namespace Sereno.Core.History
{
    /// <summary>Pila LIFO implementada a mano: lo último apilado es lo primero en desapilarse.</summary>
    public sealed class Pila<T>
    {
        private readonly List<T> _elementos = new();

        public int Cantidad => _elementos.Count;

        public void Apilar(T elemento) => _elementos.Add(elemento);

        public bool Desapilar(out T elemento)
        {
            if (_elementos.Count == 0)
            {
                elemento = default!;
                return false;
            }

            int ultimo = _elementos.Count - 1;
            elemento = _elementos[ultimo];
            _elementos.RemoveAt(ultimo);
            return true;
        }

        /// <summary>Elementos del más reciente al más antiguo.</summary>
        public IReadOnlyList<T> DelMasReciente()
        {
            var copia = new List<T>(_elementos.Count);
            for (int i = _elementos.Count - 1; i >= 0; i--)
                copia.Add(_elementos[i]);
            return copia;
        }
    }
}
