using System.Linq;
using System.Reflection;
using Sereno.Core.Acceso;
using Xunit;

namespace Sereno.Tests.Arquitectura
{
    /// <summary>
    /// Protege la regla central de la arquitectura: Sereno.Core no depende de WPF ni de Windows.
    /// </summary>
    public class ReferenciasDeSerenoCoreTests
    {
        private static readonly string[] EnsambladosProhibidos =
        {
            "PresentationFramework",
            "WindowsBase",
            "System.Windows.Forms",
        };

        [Fact]
        public void SerenoCore_NoReferenciaEnsambladosDeWpfNiDeWindowsForms()
        {
            Assembly serenoCore = typeof(Hasher).Assembly;

            string[] referencias = serenoCore.GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .ToArray();

            foreach (string prohibido in EnsambladosProhibidos)
                Assert.DoesNotContain(prohibido, referencias);
        }
    }
}
