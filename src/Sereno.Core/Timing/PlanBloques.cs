using System;

namespace Sereno.Core.Timing
{
    /// <summary>
    /// Duraciones vigentes de bloques y pausas. Cambiar la duración con un bloque en curso no
    /// lo modifica: la nueva duración aplica a partir del siguiente bloque.
    /// </summary>
    public sealed class PlanBloques
    {
        public PlanBloques(int minutosBloque, int minutosPausa)
        {
            MinutosBloque = ValidarBloque(minutosBloque);
            MinutosPausa = ValidarPausa(minutosPausa);
        }

        public int MinutosBloque { get; private set; }
        public int MinutosPausa { get; private set; }
        public bool BloqueEnCurso { get; private set; }
        public int MinutosBloqueEnCurso { get; private set; }

        public void CambiarBloque(int minutos) => MinutosBloque = ValidarBloque(minutos);

        public void CambiarPausa(int minutos) => MinutosPausa = ValidarPausa(minutos);

        public void IniciarBloque()
        {
            BloqueEnCurso = true;
            MinutosBloqueEnCurso = MinutosBloque;
        }

        public void TerminarBloque() => BloqueEnCurso = false;

        private static int ValidarBloque(int minutos) =>
            Duraciones.EsBloqueValido(minutos)
                ? minutos
                : throw new ArgumentOutOfRangeException(nameof(minutos), "El bloque debe durar entre 5 y 90 minutos.");

        private static int ValidarPausa(int minutos) =>
            Duraciones.EsPausaValida(minutos)
                ? minutos
                : throw new ArgumentOutOfRangeException(nameof(minutos), "La pausa debe durar entre 1 y 30 minutos.");
    }
}
