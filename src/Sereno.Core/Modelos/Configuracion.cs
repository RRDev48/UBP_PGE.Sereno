namespace Sereno.Core.Modelos
{
    /// <summary>Preferencias generales de la instalación. Se guarda en config.json.</summary>
    public sealed class Configuracion
    {
        /// <summary>Id del último perfil que inició sesión (sin contar el compartido).</summary>
        public string? UltimoPerfilId { get; set; }
    }
}
