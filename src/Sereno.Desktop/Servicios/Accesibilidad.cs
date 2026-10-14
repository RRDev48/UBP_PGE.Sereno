using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Sereno.Desktop.Servicios
{
    public static class Accesibilidad
    {
        /// <summary>
        /// Pide al lector de pantalla que lea un texto que acaba de cambiar o aparecer.
        /// El elemento debe tener AutomationProperties.LiveSetting configurado.
        ///
        /// LiveRegionChanged es lo habitual, pero NVDA y Narrador lo anuncian a partir del foco:
        /// una ventana que no lo toma a propósito (el aviso de fin de bloque) queda muda con ese
        /// evento solo. RaiseNotificationEvent sí se anuncia sin necesidad de foco (US-B6).
        /// </summary>
        public static void Anunciar(UIElement elemento)
        {
            AutomationPeer? peer = UIElementAutomationPeer.FromElement(elemento)
                                   ?? UIElementAutomationPeer.CreatePeerForElement(elemento);
            if (peer is null) return;

            peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);

            string texto = (elemento as TextBlock)?.Text ?? peer.GetName();
            if (string.IsNullOrEmpty(texto)) return;

            peer.RaiseNotificationEvent(
                AutomationNotificationKind.Other,
                AutomationNotificationProcessing.MostRecent,
                texto,
                "Sereno.Anuncio");
        }
    }
}
