using System;
using System.IO;
using Sereno.Core.Modelos;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Sereno.Platform.Tray
{
    /// <summary>
    /// Ícono de Sereno en la bandeja del sistema. Una vez iniciada la sesión, la aplicación
    /// vive acá: sin ventanas abiertas, para avisar sin interrumpir.
    /// </summary>
    public sealed class BandejaService : IDisposable
    {
        private readonly Forms.NotifyIcon _icono;
        private readonly Forms.ToolStripMenuItem _itemPerfil;
        private readonly Forms.ToolStripMenuItem _itemCerrarSesion;
        private Perfil? _perfil;

        public event EventHandler<PerfilEventArgs>? CierreDeSesionSolicitado;
        public event EventHandler? SalidaSolicitada;
        public event EventHandler? ConfiguracionSolicitada;

        /// <param name="iconoStream">
        /// Contenido del ícono a mostrar en la bandeja, o null para usar el ícono por defecto
        /// del sistema. Sereno.Platform no conoce recursos empaquetados de WPF: quien lo llama
        /// (Sereno.Desktop) es responsable de abrir el stream.
        /// </param>
        public BandejaService(Stream? iconoStream)
        {
            _icono = new Forms.NotifyIcon
            {
                Icon = iconoStream is null ? Drawing.SystemIcons.Application : new Drawing.Icon(iconoStream),
                Text = "Sereno",
                Visible = false,
            };

            _itemPerfil = new Forms.ToolStripMenuItem { Enabled = false };
            _itemCerrarSesion = new Forms.ToolStripMenuItem("Cerrar sesión", null, (_, _) =>
            {
                if (_perfil is not null)
                    CierreDeSesionSolicitado?.Invoke(this, new PerfilEventArgs(_perfil));
            });
            var itemConfiguracion = new Forms.ToolStripMenuItem("Configuración…", null,
                (_, _) => ConfiguracionSolicitada?.Invoke(this, EventArgs.Empty));
            var itemSalir = new Forms.ToolStripMenuItem("Salir de Sereno", null,
                (_, _) => SalidaSolicitada?.Invoke(this, EventArgs.Empty));

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add(_itemPerfil);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(itemConfiguracion);
            menu.Items.Add(_itemCerrarSesion);
            menu.Items.Add(itemSalir);
            _icono.ContextMenuStrip = menu;
        }

        public bool Visible => _icono.Visible;

        public void Mostrar(Perfil perfil)
        {
            _perfil = perfil;
            string nombre = perfil.EsCompartido ? "sin perfil" : perfil.Nombre;
            _itemPerfil.Text = "Perfil: " + nombre;
            _itemCerrarSesion.Text = perfil.EsCompartido ? "Elegir un perfil" : "Cerrar sesión";

            string texto = "Sereno · " + nombre;
            _icono.Text = texto.Length > 63 ? texto[..63] : texto;   // límite de Windows
            _icono.Visible = true;
            _icono.ShowBalloonTip(4000, "Sereno está activo",
                "Queda en la bandeja del sistema. Hacé clic derecho en el ícono para ver las opciones.",
                Forms.ToolTipIcon.None);
        }

        public void MostrarAviso(string titulo, string texto) =>
            _icono.ShowBalloonTip(5000, titulo, texto, Forms.ToolTipIcon.None);

        public void Ocultar()
        {
            _icono.Visible = false;
            _perfil = null;
        }

        public void Dispose()
        {
            _icono.Visible = false;
            _icono.Dispose();
        }
    }
}
