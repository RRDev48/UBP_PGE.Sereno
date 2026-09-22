using Sereno.Core.Acceso;
using Xunit;

namespace Sereno.Tests.Acceso
{
    public class HasherTests
    {
        [Fact]
        public void Verificar_ConElSecretoCorrecto_DevuelveTrue()
        {
            var (hash, sal) = Hasher.Crear("mi-secreto");

            bool valido = Hasher.Verificar("mi-secreto", hash, sal, Hasher.Iteraciones);

            Assert.True(valido);
        }

        [Fact]
        public void Verificar_ConElSecretoIncorrecto_DevuelveFalse()
        {
            var (hash, sal) = Hasher.Crear("mi-secreto");

            bool valido = Hasher.Verificar("otro-secreto", hash, sal, Hasher.Iteraciones);

            Assert.False(valido);
        }

        [Fact]
        public void Crear_DosVecesElMismoSecreto_DaHashesDistintosPorLaSal()
        {
            var (hash1, sal1) = Hasher.Crear("mi-secreto");
            var (hash2, sal2) = Hasher.Crear("mi-secreto");

            Assert.NotEqual(sal1, sal2);
            Assert.NotEqual(hash1, hash2);
        }
    }
}
