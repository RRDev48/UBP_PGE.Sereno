using System;
using System.IO;

namespace Sereno.Servicios
{
    /// <summary>Ubicaciones en disco. Todo queda dentro de %APPDATA%\Sereno.</summary>
    public static class Rutas
    {
        public static string Base { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sereno");
    }
}
