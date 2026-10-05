using Sereno.Core.Timing;

namespace Sereno.Core.Modelos
{
    /// <summary>Preferencias generales de la instalación. Se guarda en config.json.</summary>
    public sealed class Configuracion
    {
        /// <summary>Id del último perfil que inició sesión (sin contar el compartido).</summary>
        public string? UltimoPerfilId { get; set; }

        public int MinutosBloque { get; set; } = Duraciones.BloquePorDefecto;

        public int MinutosPausa { get; set; } = Duraciones.PausaPorDefecto;
    }
}
