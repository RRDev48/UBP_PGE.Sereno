namespace Sereno.Core.Modelos
{
    /// <summary>Por qué canal llega el aviso al terminar un bloque.</summary>
    public enum CanalAviso
    {
        SoloVisual,
        SoloHablado,
        VisualYHablado,
        SoloSistema,
    }

    public sealed class PreferenciasAviso
    {
        public CanalAviso Canal { get; set; } = CanalAviso.SoloVisual;

        /// <summary>Fuerza el modo sin movimiento aunque Windows tenga las animaciones activas.</summary>
        public bool BajoEstimulo { get; set; }

        /// <summary>Segundos mínimos que el aviso visual permanece antes de cerrarse solo (10 a 60).</summary>
        public int SegundosVisible { get; set; } = 10;

        public static bool EsSegundosValido(int segundos) => segundos is >= 10 and <= 60;
    }
}

namespace Sereno.Core.Modelos
{
    public static class CanalesAviso
    {
        public static bool UsaVoz(CanalAviso canal) => canal is CanalAviso.SoloHablado or CanalAviso.VisualYHablado;

        public static bool UsaVentana(CanalAviso canal) => canal is CanalAviso.SoloVisual or CanalAviso.VisualYHablado;

        /// <summary>Si no hay voz en español, los canales hablados caen a visual en lugar de fallar.</summary>
        public static CanalAviso Efectivo(CanalAviso canal, bool hayVozEnEspanol) =>
            hayVozEnEspanol || !UsaVoz(canal) ? canal : CanalAviso.SoloVisual;
    }
}
