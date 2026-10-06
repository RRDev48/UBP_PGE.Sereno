using Sereno.Core.History;
using Xunit;

namespace Sereno.Tests.History
{
    public class PilaTests
    {
        [Fact]
        public void Desapilar_DevuelveLoUltimoApilado()
        {
            var pila = new Pila<int>();
            pila.Apilar(1);
            pila.Apilar(2);

            Assert.True(pila.Desapilar(out int primero));
            Assert.Equal(2, primero);
            Assert.Equal(1, pila.Cantidad);
        }

        [Fact]
        public void Desapilar_PilaVacia_DevuelveFalse()
        {
            Assert.False(new Pila<int>().Desapilar(out _));
        }

        [Fact]
        public void DelMasReciente_DevuelveElOrdenDeLaPila()
        {
            var pila = new Pila<string>();
            pila.Apilar("a");
            pila.Apilar("b");

            Assert.Equal(new[] { "b", "a" }, pila.DelMasReciente());
        }
    }
}
