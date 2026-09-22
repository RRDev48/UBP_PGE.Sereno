using System.Security.Cryptography;
using System.Text;

namespace Sereno.Core.Acceso
{
    /// <summary>
    /// Genera y da formato a la clave de recuperación (16 caracteres, en grupos de 4).
    /// El alfabeto omite 0, O, 1 e I para que la clave no se confunda al copiarla a mano.
    /// </summary>
    public static class ClaveRecuperacion
    {
        public const int Longitud = 16;
        private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string Generar()
        {
            var sb = new StringBuilder(Longitud);
            for (int i = 0; i < Longitud; i++)
                sb.Append(Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)]);
            return sb.ToString();
        }

        /// <summary>Pasa a mayúsculas, descarta guiones y símbolos, y corta en 16 caracteres.</summary>
        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;

            var sb = new StringBuilder(Longitud);
            foreach (char c in texto)
            {
                if (sb.Length == Longitud) break;
                if (char.IsAsciiLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        /// <summary>"ABCDEFGH" → "ABCD-EFGH".</summary>
        public static string Formatear(string normalizada)
        {
            var sb = new StringBuilder(normalizada.Length + 3);
            for (int i = 0; i < normalizada.Length; i++)
            {
                if (i > 0 && i % 4 == 0) sb.Append('-');
                sb.Append(normalizada[i]);
            }
            return sb.ToString();
        }
    }
}
