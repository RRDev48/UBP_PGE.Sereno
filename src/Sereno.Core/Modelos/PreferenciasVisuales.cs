namespace Sereno.Core.Modelos
{
    /// <summary>Preferencias de lectura y visualización de la instalación.</summary>
    public sealed class PreferenciasVisuales
    {
        /// <summary>Usa la fuente Atkinson Hyperlegible en lugar de la del sistema.</summary>
        public bool FuenteLectura { get; set; }

        /// <summary>Colores de máximo contraste, independientes del tema de Windows.</summary>
        public bool AltoContraste { get; set; }
    }
}
