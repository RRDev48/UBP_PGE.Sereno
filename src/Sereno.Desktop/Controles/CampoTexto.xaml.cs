using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Sereno.Desktop.Servicios;

namespace Sereno.Desktop.Controles
{
    /// <summary>Campo de texto con etiqueta, ayuda y error, con el mismo aspecto que CampoContrasena.</summary>
    public partial class CampoTexto : UserControl
    {
        private bool _conError;

        public event EventHandler? TextoCambiado;

        public CampoTexto()
        {
            InitializeComponent();
            IsKeyboardFocusWithinChanged += (_, _) => ActualizarMarco();
        }

        /// <summary>Acceso al TextBox interno, por ejemplo para dar formato mientras se escribe.</summary>
        public TextBox CajaTexto => Caja;

        public string Etiqueta
        {
            get => EtiquetaTexto.Text;
            set
            {
                EtiquetaTexto.Text = value;
                AutomationProperties.SetName(Caja, value);
            }
        }

        public string Ayuda
        {
            get => AyudaTexto.Text;
            set
            {
                AyudaTexto.Text = value;
                AyudaTexto.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
                AutomationProperties.SetHelpText(Caja, value);
            }
        }

        public string Texto
        {
            get => Caja.Text;
            set => Caja.Text = value;
        }

        public int LongitudMaxima
        {
            get => Caja.MaxLength;
            set => Caja.MaxLength = value;
        }

        private bool _monoespaciada;

        public bool Monoespaciada
        {
            get => _monoespaciada;
            set
            {
                _monoespaciada = value;
                if (value && TryFindResource("FuenteMono") is FontFamily mono)
                {
                    Caja.FontFamily = mono;
                    Caja.FontSize = 17;
                }
            }
        }

        public void MostrarError(string mensaje)
        {
            ErrorTexto.Text = mensaje;
            ErrorPanel.Visibility = Visibility.Visible;
            _conError = true;
            ActualizarMarco();
            AutomationProperties.SetHelpText(Caja, mensaje);
            Accesibilidad.Anunciar(ErrorTexto);
        }

        public void LimpiarError()
        {
            if (!_conError) return;
            _conError = false;
            ErrorPanel.Visibility = Visibility.Collapsed;
            ActualizarMarco();
            AutomationProperties.SetHelpText(Caja, AyudaTexto.Text);
        }

        public void EnfocarCampo()
        {
            Caja.Focus();
            Keyboard.Focus(Caja);
        }

        private void Caja_TextChanged(object sender, TextChangedEventArgs e) =>
            TextoCambiado?.Invoke(this, EventArgs.Empty);

        private void ActualizarMarco()
        {
            string clave = _conError ? "Error" : IsKeyboardFocusWithin ? "Foco" : "BordeCampo";
            Marco.SetResourceReference(Border.BorderBrushProperty, clave);
            Marco.BorderThickness = new Thickness(clave == "BordeCampo" ? 1.5 : 2);
        }
    }
}
