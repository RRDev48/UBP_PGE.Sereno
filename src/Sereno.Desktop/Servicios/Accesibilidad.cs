using System.Windows;
using System.Windows.Automation.Peers;

namespace Sereno.Desktop.Servicios
{
    public static class Accesibilidad
    {
        /// <summary>
        /// Pide al lector de pantalla que lea un texto que acaba de cambiar.
        /// El elemento debe tener AutomationProperties.LiveSetting configurado.
        /// </summary>
        public static void Anunciar(UIElement elemento)
        {
            AutomationPeer? peer = UIElementAutomationPeer.FromElement(elemento)
                                   ?? UIElementAutomationPeer.CreatePeerForElement(elemento);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }
}
