using System;
using System.Security.Cryptography;
using System.Text;

namespace Sereno.Core.Acceso
{
    /// <summary>
    /// Deriva y verifica secretos con PBKDF2-SHA256. Se usa para la contraseña y para la clave
    /// de recuperación, de modo que perfil.json nunca contiene ninguna de las dos en texto plano.
    /// </summary>
    public static class Hasher
    {
        public const int Iteraciones = 210_000;
        private const int LargoSal = 16;
        private const int LargoHash = 32;

        public static (string Hash, string Sal) Crear(string secreto)
        {
            byte[] sal = RandomNumberGenerator.GetBytes(LargoSal);
            byte[] hash = Derivar(secreto, sal, Iteraciones);
            return (Convert.ToBase64String(hash), Convert.ToBase64String(sal));
        }

        public static bool Verificar(string secreto, string hashGuardado, string salGuardada, int iteraciones)
        {
            if (hashGuardado.Length == 0 || salGuardada.Length == 0 || iteraciones <= 0)
                return false;

            try
            {
                byte[] esperado = Convert.FromBase64String(hashGuardado);
                byte[] calculado = Derivar(secreto, Convert.FromBase64String(salGuardada), iteraciones);
                // Comparación en tiempo constante: no revela cuántos bytes coinciden.
                return CryptographicOperations.FixedTimeEquals(esperado, calculado);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static byte[] Derivar(string secreto, byte[] sal, int iteraciones) =>
            Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(secreto), sal, iteraciones,
                HashAlgorithmName.SHA256, LargoHash);
    }
}
