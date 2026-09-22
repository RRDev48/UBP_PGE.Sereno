using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Sereno.Desktop.Servicios;

namespace Sereno.Desktop.Controles
{
    /// <summary>
    /// Campo de contraseña con botón para mostrarla, texto de ayuda y mensaje de error.
    /// El error se muestra con ícono, texto y borde, y se anuncia al lector de pantalla.
    /// </summary>
    public partial class CampoContrasena : UserControl
    {
        private bool _sincronizando;
        private bool _conError;
        private string _etiqueta = string.Empty;

        public event EventHandler? ContrasenaCambiada;

        public CampoContrasena()
        {
            InitializeComponent();
            IsKeyboardFocusWithinChanged += (_, _) => ActualizarMarco();
        }

        public string Etiqueta
        {
            get => _etiqueta;
            set
            {
                _etiqueta = value;
                EtiquetaTexto.Text = value;
                AutomationProperties.SetName(CajaOculta, value);
                AutomationProperties.SetName(CajaVisible, value);
                ActualizarNombreBoton();
            }
        }

        public string Ayuda
        {
            get => AyudaTexto.Text;
            set
            {
                AyudaTexto.Text = value;
                AyudaTexto.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
                ActualizarTextoDeAyuda();
            }
        }

        public string Contrasena => Mostrar.IsChecked == true ? CajaVisible.Text : CajaOculta.Password;

        public void MostrarError(string mensaje)
        {
            ErrorTexto.Text = mensaje;
            ErrorPanel.Visibility = Visibility.Visible;
            _conError = true;
            ActualizarMarco();
            ActualizarTextoDeAyuda();
            Accesibilidad.Anunciar(ErrorTexto);
        }

        public void LimpiarError()
        {
            if (!_conError) return;
            _conError = false;
            ErrorPanel.Visibility = Visibility.Collapsed;
            ErrorTexto.Text = string.Empty;
            ActualizarMarco();
            ActualizarTextoDeAyuda();
        }

        public void Limpiar()
        {
            _sincronizando = true;
            CajaOculta.Password = string.Empty;
            CajaVisible.Text = string.Empty;
            _sincronizando = false;
            LimpiarError();
        }

        public void EnfocarCampo()
        {
            IInputElement destino = Mostrar.IsChecked == true ? CajaVisible : CajaOculta;
            destino.Focus();
            Keyboard.Focus(destino);
        }

        public void SeleccionarTodo()
        {
            EnfocarCampo();
            if (Mostrar.IsChecked == true) CajaVisible.SelectAll();
            else CajaOculta.SelectAll();
        }

        private void CajaOculta_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_sincronizando) ContrasenaCambiada?.Invoke(this, EventArgs.Empty);
        }

        private void CajaVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_sincronizando) ContrasenaCambiada?.Invoke(this, EventArgs.Empty);
        }

        private void Mostrar_Checked(object sender, RoutedEventArgs e)
        {
            _sincronizando = true;
            CajaVisible.Text = CajaOculta.Password;
            _sincronizando = false;
            CajaOculta.Visibility = Visibility.Collapsed;
            CajaVisible.Visibility = Visibility.Visible;
            CajaVisible.CaretIndex = CajaVisible.Text.Length;
            ActualizarNombreBoton();
        }

        private void Mostrar_Unchecked(object sender, RoutedEventArgs e)
        {
            _sincronizando = true;
            CajaOculta.Password = CajaVisible.Text;
            CajaVisible.Text = string.Empty;   // no dejar la contraseña en el TextBox oculto
            _sincronizando = false;
            CajaVisible.Visibility = Visibility.Collapsed;
            CajaOculta.Visibility = Visibility.Visible;
            ActualizarNombreBoton();
        }

        private void ActualizarNombreBoton()
        {
            string accion = Mostrar.IsChecked == true ? "Ocultar " : "Mostrar ";
            AutomationProperties.SetName(Mostrar, accion + _etiqueta.ToLowerInvariant());
        }

        private void ActualizarTextoDeAyuda()
        {
            string texto = _conError ? ErrorTexto.Text : AyudaTexto.Text;
            AutomationProperties.SetHelpText(CajaOculta, texto);
            AutomationProperties.SetHelpText(CajaVisible, texto);
        }

        private void ActualizarMarco()
        {
            string clave = _conError ? "Error" : IsKeyboardFocusWithin ? "Foco" : "BordeCampo";
            Marco.SetResourceReference(Border.BorderBrushProperty, clave);
            Marco.BorderThickness = new Thickness(clave == "BordeCampo" ? 1.5 : 2);
        }
    }
}
