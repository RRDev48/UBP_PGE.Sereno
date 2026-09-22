using System;
using System.Text.Json.Serialization;

namespace Sereno.Core.Modelos
{
    /// <summary>
    /// Perfil local de una persona. Se guarda como perfil.json dentro de su carpeta.
    /// Nunca se guardan la contraseña ni la clave en texto plano dentro de este archivo:
    /// solo su hash PBKDF2 y la sal correspondiente.
    /// </summary>
    public sealed class Perfil
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Perfil compartido que se usa con la opción "Usar Sereno sin perfil".</summary>
        public bool EsCompartido { get; set; }

        public string HashContrasena { get; set; } = string.Empty;
        public string SalContrasena { get; set; } = string.Empty;
        public string HashClave { get; set; } = string.Empty;
        public string SalClave { get; set; } = string.Empty;
        public int Iteraciones { get; set; }

        /// <summary>Si es verdadero, el splash abre Sereno directo en la bandeja.</summary>
        public bool AbrirSinContrasena { get; set; }

        public DateTime CreadoEn { get; set; } = DateTime.Now;

        /// <summary>Carpeta del perfil en disco. Se completa al cargar; no se serializa.</summary>
        [JsonIgnore]
        public string Carpeta { get; set; } = string.Empty;

        [JsonIgnore]
        public bool TieneContrasena => HashContrasena.Length > 0;

        [JsonIgnore]
        public string Inicial => Nombre.Length > 0 ? char.ToUpperInvariant(Nombre[0]).ToString() : "?";
    }
}
