using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Sereno.Core.Breaks;
using Sereno.Core.History;
using Sereno.Desktop.Servicios;

namespace Sereno.Desktop.Vistas
{
    /// <summary>
    /// Propone una micropausa acorde a la duración de pausa configurada. Las rutinas se muestran
    /// como pasos ordenados; se pueden abandonar en cualquier momento sin mensajes de falta.
    /// </summary>
    public partial class PausaWindow : Window
    {
        private readonly SessionHistory _historial;
        private readonly DispatcherTimer _automatico = new();
        private BreakSuggestion? _seleccionada;
        private int _paso;

        public PausaWindow(int minutosPausa, SessionHistory historial)
        {
            InitializeComponent();
            _historial = historial;
            BajadaTexto.Text = $"Propuestas que entran en tus {minutosPausa} minutos de pausa.";
            Lista.ItemsSource = Presentar(BreakCatalog.Para(minutosPausa));
            _automatico.Tick += (_, _) => AvanzarPaso();
        }


        private void Lista_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Lista.SelectedItem is not Opcion opcion) return;

            _seleccionada = opcion.Sugerencia;
            _paso = 0;
            NombreDetalle.Text = $"{_seleccionada.Icono} {_seleccionada.Nombre}";
            DescripcionDetalle.Text = _seleccionada.Descripcion;
            Detalle.Visibility = Visibility.Visible;
            Rutina.Visibility = _seleccionada.EsRutina ? Visibility.Visible : Visibility.Collapsed;
            TomarPausa.Visibility = _seleccionada.EsRutina ? Visibility.Collapsed : Visibility.Visible;
            if (_seleccionada.EsRutina) MostrarPasos();
        }

        private void Avance_Changed(object sender, RoutedEventArgs e)
        {
            _automatico.Stop();
            if (_seleccionada is null || !_seleccionada.EsRutina || AvanceManual.IsChecked == true) return;
            IniciarAutomatico();
        }

        private void SiguientePaso_Click(object sender, RoutedEventArgs e) => AvanzarPaso();

        private void SalirRutina_Click(object sender, RoutedEventArgs e)
        {
            _automatico.Stop();
            _seleccionada = null;
            _paso = 0;
            Detalle.Visibility = Visibility.Collapsed;
            Lista.SelectedItem = null;
        }

        private void TomarPausa_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada is null) return;
            int minutos = Math.Max(1, (int)Math.Ceiling(_seleccionada.Segundos / 60.0));
            _historial.Registrar(TipoEntrada.PausaTomada, minutos);
            Close();
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();

        private void Ventana_Cerrada(object? sender, EventArgs e) => _automatico.Stop();

        private void AvanzarPaso()
        {
            if (_seleccionada is null || !_seleccionada.EsRutina) return;

            _paso++;
            if (_paso >= _seleccionada.Pasos.Count)
            {
                _automatico.Stop();
                _paso = _seleccionada.Pasos.Count - 1;
                SiguientePaso.IsEnabled = false;
                NombreDetalle.Text = $"{_seleccionada.Icono} {_seleccionada.Nombre}: rutina terminada";
            }
            MostrarPasos();
            if (AvanceAutomatico.IsChecked == true && _paso < _seleccionada.Pasos.Count - 1)
                IniciarAutomatico();
        }

        private void IniciarAutomatico()
        {
            if (_seleccionada is null) return;
            _automatico.Interval = TimeSpan.FromSeconds(_seleccionada.Pasos[_paso].Segundos);
            _automatico.Start();
        }

        private void MostrarPasos()
        {
            if (_seleccionada is null) return;

            PasosPanel.Children.Clear();
            for (int i = 0; i < _seleccionada.Pasos.Count; i++)
            {
                bool actual = i == _paso;
                var texto = new TextBlock
                {
                    Text = $"{i + 1} · {_seleccionada.Pasos[i].Texto}",
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = actual ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = (System.Windows.Media.Brush)FindResource("Texto"),
                };
                var borde = new Border
                {
                    Padding = new Thickness(12, 10, 12, 10),
                    MinHeight = 44,
                    Margin = new Thickness(0, 0, 0, 8),
                    CornerRadius = new CornerRadius(10),
                    BorderThickness = new Thickness(actual ? 2 : 1.5),
                    BorderBrush = (System.Windows.Media.Brush)FindResource(actual ? "Foco" : "BordeCampo"),
                    Background = (System.Windows.Media.Brush)FindResource(actual ? "AvisoFondo" : "Fondo"),
                    Child = texto,
                };
                PasosPanel.Children.Add(borde);
            }
        }

        private static IReadOnlyList<Opcion> Presentar(IReadOnlyList<BreakSuggestion> sugerencias)
        {
            var opciones = new List<Opcion>(sugerencias.Count);
            foreach (BreakSuggestion s in sugerencias)
                opciones.Add(new Opcion(s));
            return opciones;
        }

        internal sealed record Opcion(BreakSuggestion Sugerencia)
        {
            public string Icono => Sugerencia.Icono;
            public string Nombre => Sugerencia.Nombre;
            public string Etiqueta => Sugerencia.EsRutina
                ? $"{Sugerencia.Pasos.Count} pasos · {Sugerencia.Segundos} segundos por paso"
                : $"{Sugerencia.Segundos} segundos";
        }
    }
}
