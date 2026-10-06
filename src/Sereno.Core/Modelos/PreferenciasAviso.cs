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
