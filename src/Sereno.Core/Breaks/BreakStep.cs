namespace Sereno.Core.Breaks
{
    /// <summary>Un paso de una rutina de pausa, con su duración sugerida en segundos.</summary>
    public sealed record BreakStep(string Texto, int Segundos);
}
