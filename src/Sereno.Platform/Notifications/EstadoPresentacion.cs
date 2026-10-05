using System.Runtime.InteropServices;

namespace Sereno.Platform.Notifications
{
    /// <summary>
    /// Indica si Windows está mostrando algo que no conviene interrumpir: una app a pantalla
    /// completa o el modo presentación. Usa SHQueryUserNotificationState de la shell.
    /// </summary>
    public static class EstadoPresentacion
    {
        private const int QunsBusy = 2;
        private const int QunsRunningD3dFullScreen = 3;
        private const int QunsPresentationMode = 4;

        public static bool PantallaOcupada() =>
            SHQueryUserNotificationState(out int estado) == 0 && EsOcupado(estado);

        public static bool EsOcupado(int estado) =>
            estado is QunsBusy or QunsRunningD3dFullScreen or QunsPresentationMode;

        [DllImport("shell32.dll")]
        private static extern int SHQueryUserNotificationState(out int estado);
    }
}
