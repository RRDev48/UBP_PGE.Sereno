using System.Linq;

namespace Sereno.Servicios
{
    public enum NivelFuerza { Vacia = 0, Debil = 1, Aceptable = 2, Fuerte = 3 }

    /// <summary>Validaciones de contraseña compartidas por el registro y la recuperación.</summary>
    public static class ReglasContrasena
    {
        public const int LongitudMinima = 8;

        public static NivelFuerza Evaluar(string contrasena)
        {
            if (contrasena.Length == 0) return NivelFuerza.Vacia;

            int puntos = 0;
            if (contrasena.Length >= LongitudMinima) puntos++;
            if (contrasena.Length >= 12) puntos++;
            if (contrasena.Any(char.IsDigit) && contrasena.Any(char.IsLetter)) puntos++;
            if (contrasena.Any(c => !char.IsLetterOrDigit(c))) puntos++;

            if (contrasena.Length < LongitudMinima || puntos <= 1) return NivelFuerza.Debil;
            return puntos >= 3 ? NivelFuerza.Fuerte : NivelFuerza.Aceptable;
        }

        public static string Describir(NivelFuerza nivel) => nivel switch
        {
            NivelFuerza.Debil => "débil",
            NivelFuerza.Aceptable => "aceptable",
            NivelFuerza.Fuerte => "fuerte",
            _ => "sin completar",
        };

        /// <summary>Devuelve el mensaje de error o null si la contraseña es válida.</summary>
        public static string? ValidarLongitud(string contrasena)
        {
            int faltan = LongitudMinima - contrasena.Length;
            if (faltan <= 0) return null;
            return faltan == 1
                ? "Falta 1 carácter para llegar a 8."
                : $"Faltan {faltan} caracteres para llegar a 8.";
        }

        public static string? ValidarRepeticion(string contrasena, string repetida) =>
            repetida.Length == 0 || contrasena != repetida
                ? "Las dos contraseñas tienen que ser iguales."
                : null;
    }
}
