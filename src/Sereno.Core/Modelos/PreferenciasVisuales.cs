namespace Sereno.Core.Modelos
{
    /// <summary>Preferencias de lectura y visualización de la instalación.</summary>
    public sealed class PreferenciasVisuales
    {
        public const int EscalaPorDefecto = 100;

        /// <summary>Usa la fuente Atkinson Hyperlegible en lugar de la del sistema.</summary>
        public bool FuenteLectura { get; set; }

        /// <summary>Colores de máximo contraste, independientes del tema de Windows.</summary>
        public bool AltoContraste { get; set; }

        /// <summary>Interlineado de 1,5 veces el tamaño de la letra.</summary>
        public bool EspaciadoAmplio { get; set; }

        /// <summary>Porcentaje del tamaño de la interfaz: 100, 125 o 150.</summary>
        public int Escala { get; set; } = EscalaPorDefecto;

        public static bool EsEscalaValida(int escala) => escala is 100 or 125 or 150;
    }
}
