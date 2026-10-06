using System.Windows;
using Sereno.Core.History;

namespace Sereno.Desktop.Vistas
{
    /// <summary>Resumen de la sesión actual. Muestra registros, nunca calificaciones.</summary>
    public partial class ResumenWindow : Window
    {
        private readonly SessionHistory _historial;

        public ResumenWindow(SessionHistory historial)
        {
            InitializeComponent();
            _historial = historial;
            Actualizar();
        }

        private void Actualizar()
        {
            BloquesTexto.Text = _historial.Contar(TipoEntrada.BloqueCompletado).ToString();
            PausasTexto.Text = _historial.Contar(TipoEntrada.PausaTomada).ToString();
            PospuestosTexto.Text = _historial.Contar(TipoEntrada.AvisoPospuesto).ToString();

            Registro.Items.Clear();
            foreach (EntradaSesion entrada in _historial.Entradas)
                Registro.Items.Add(Describir(entrada));

            bool hayRegistros = _historial.Entradas.Count > 0;
            VacioTexto.Visibility = hayRegistros ? Visibility.Collapsed : Visibility.Visible;
            Deshacer.IsEnabled = hayRegistros;
        }

        private void Deshacer_Click(object sender, RoutedEventArgs e)
        {
            _historial.DeshacerUltima();
            Actualizar();
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();

        private static string Describir(EntradaSesion entrada) => entrada.Tipo switch
        {
            TipoEntrada.BloqueCompletado => $"Bloque de {entrada.Minutos} min completado",
            TipoEntrada.PausaTomada => $"Pausa de {entrada.Minutos} min tomada",
            TipoEntrada.AvisoPospuesto => $"Aviso pospuesto (bloque de {entrada.Minutos} min)",
            TipoEntrada.AvisoDescartado => $"Aviso descartado (bloque de {entrada.Minutos} min)",
            _ => string.Empty,
        };
    }
}
