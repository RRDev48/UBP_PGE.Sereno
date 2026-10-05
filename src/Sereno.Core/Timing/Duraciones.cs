namespace Sereno.Core.Timing
{
    /// <summary>Rangos y valores por defecto para la duración de bloques de foco y pausas.</summary>
    public static class Duraciones
    {
        public const int BloqueMinimo = 5;
        public const int BloqueMaximo = 90;
        public const int PausaMinima = 1;
        public const int PausaMaxima = 30;

        public const int BloquePorDefecto = 25;
        public const int PausaPorDefecto = 5;

        public static bool EsBloqueValido(int minutos) => minutos >= BloqueMinimo && minutos <= BloqueMaximo;

        public static bool EsPausaValida(int minutos) => minutos >= PausaMinima && minutos <= PausaMaxima;
    }
}
