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
