using System;
using System.IO;
using System.Runtime.InteropServices;
using Sereno.Core.Modelos;
using Sereno.Core.Timing;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Sereno.Platform.Tray
{
    /// <summary>
    /// Ícono de Sereno en la bandeja del sistema. Una vez iniciada la sesión, la aplicación
    /// vive acá: sin ventanas abiertas, para avisar sin interrumpir. El ícono muestra con un punto
    /// de color si hay un bloque en curso, y el tooltip indica el tiempo restante.
    /// </summary>
    public sealed class BandejaService : IDisposable
    {
        private const int TamanoIcono = 32;
        private static readonly Drawing.Color ColorEnCurso = Drawing.Color.FromArgb(46, 125, 50);
        private static readonly Drawing.Color ColorDetenido = Drawing.Color.FromArgb(110, 110, 110);

        private readonly Forms.NotifyIcon _icono;
        private readonly Forms.ToolStripMenuItem _itemPerfil;
        private readonly Forms.ToolStripMenuItem _itemCerrarSesion;
        private readonly Drawing.Icon _iconoBase;
        private Drawing.Icon? _iconoGenerado;
        private Perfil? _perfil;
        private bool? _enCursoMostrado;
        private string _textoMostrado = string.Empty;

        public event EventHandler<PerfilEventArgs>? CierreDeSesionSolicitado;
        public event EventHandler? SalidaSolicitada;
        public event EventHandler? ConfiguracionSolicitada;
        public event EventHandler? PausaSolicitada;
        public event EventHandler? ResumenSolicitado;

        /// <param name="iconoStream">
        /// Contenido del ícono a mostrar en la bandeja, o null para usar el ícono por defecto
        /// del sistema. Sereno.Platform no conoce recursos empaquetados de WPF: quien lo llama
        /// (Sereno.Desktop) es responsable de abrir el stream.
        /// </param>
        public BandejaService(Stream? iconoStream)
        {
            _iconoBase = iconoStream is null ? Drawing.SystemIcons.Application : new Drawing.Icon(iconoStream);
            _icono = new Forms.NotifyIcon
            {
                Icon = _iconoBase,
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
            var itemPausa = new Forms.ToolStripMenuItem("Proponer una pausa…", null,
                (_, _) => PausaSolicitada?.Invoke(this, EventArgs.Empty));
            var itemResumen = new Forms.ToolStripMenuItem("Resumen de la sesión…", null,
                (_, _) => ResumenSolicitado?.Invoke(this, EventArgs.Empty));

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add(_itemPerfil);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(itemPausa);
            menu.Items.Add(itemResumen);
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
            _enCursoMostrado = null;
            _icono.Visible = true;
            ActualizarEstado(BlockState.Detenido, 0);
        }

        /// <summary>Refleja el estado del temporizador en el ícono y en el tooltip.</summary>
        public void ActualizarEstado(BlockState estado, int minutosRestantes)
        {
            if (!_icono.Visible) return;

            bool enCurso = estado == BlockState.EnCurso;
            if (_enCursoMostrado != enCurso)
            {
                AplicarIcono(enCurso);
                _enCursoMostrado = enCurso;
            }

            string nombre = _perfil is null ? string.Empty : _perfil.EsCompartido ? "sin perfil" : _perfil.Nombre;
            string detalle = enCurso ? $"bloque · quedan {minutosRestantes} min" : "sin bloque en curso";
            string texto = $"Sereno · {nombre} · {detalle}";
            texto = texto.Length > 63 ? texto[..63] : texto;   // límite de Windows
            if (texto != _textoMostrado)
            {
                _icono.Text = texto;
                _textoMostrado = texto;
            }
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
            _iconoGenerado?.Dispose();
        }

        private void AplicarIcono(bool enCurso)
        {

            using var mapa = new Drawing.Bitmap(TamanoIcono, TamanoIcono);
            using (Drawing.Graphics g = Drawing.Graphics.FromImage(mapa))
            {
                g.DrawIcon(_iconoBase, new Drawing.Rectangle(0, 0, TamanoIcono, TamanoIcono));
                using var borde = new Drawing.SolidBrush(Drawing.Color.White);
                using var punto = new Drawing.SolidBrush(enCurso ? ColorEnCurso : ColorDetenido);
                g.FillEllipse(borde, 16, 16, 15, 15);
                g.FillEllipse(punto, 19, 19, 9, 9);
            }

            // Icon.Clone copia el icono; el handle original se libera enseguida.
            IntPtr handle = mapa.GetHicon();
            Drawing.Icon nuevo;
            try
            {
                nuevo = (Drawing.Icon)Drawing.Icon.FromHandle(handle).Clone();
            }
            finally
            {
                DestroyIcon(handle);
            }

            Drawing.Icon? anterior = _iconoGenerado;
            _icono.Icon = nuevo;
            _iconoGenerado = nuevo;
            anterior?.Dispose();
        }

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
