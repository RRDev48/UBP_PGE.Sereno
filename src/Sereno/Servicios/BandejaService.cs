using System;
using System.Windows;
using Sereno.Modelos;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Sereno.Servicios
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

        public BandejaService()
        {
            var recurso = Application.GetResourceStream(new Uri("pack://application:,,,/Recursos/sereno.ico"));
            _icono = new Forms.NotifyIcon
            {
                Icon = recurso is null ? Drawing.SystemIcons.Application : new Drawing.Icon(recurso.Stream),
                Text = "Sereno",
                Visible = false,
            };

            _itemPerfil = new Forms.ToolStripMenuItem { Enabled = false };
            _itemCerrarSesion = new Forms.ToolStripMenuItem("Cerrar sesión", null, (_, _) =>
            {
                if (_perfil is not null)
                    CierreDeSesionSolicitado?.Invoke(this, new PerfilEventArgs(_perfil));
            });
            var itemSalir = new Forms.ToolStripMenuItem("Salir de Sereno", null,
                (_, _) => SalidaSolicitada?.Invoke(this, EventArgs.Empty));

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add(_itemPerfil);
            menu.Items.Add(new Forms.ToolStripSeparator());
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
