using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Sereno.Controles
{
    /// <summary>
    /// Muestra en qué paso está la persona. Cada paso combina número (o tilde), texto y estado,
    /// y tiene un nombre accesible del tipo "Paso 2 de 3: Nueva contraseña, actual".
    /// </summary>
    public sealed class IndicadorPasos : WrapPanel
    {
        public IndicadorPasos()
        {
            Margin = new Thickness(0, 0, 0, 18);
        }

        public void Configurar(IReadOnlyList<string> pasos, int actual)
        {
            Children.Clear();

            for (int i = 0; i < pasos.Count; i++)
            {
                bool esActual = i == actual;
                bool completo = i < actual;

                var numero = new TextBlock
                {
                    Text = completo ? "✓" : (i + 1).ToString(),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                var circulo = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(1.5),
                    Child = numero,
                };

                if (esActual)
                {
                    circulo.SetResourceReference(Border.BackgroundProperty, "FondoPrimario");
                    circulo.SetResourceReference(Border.BorderBrushProperty, "FondoPrimario");
                    numero.SetResourceReference(TextBlock.ForegroundProperty, "TextoPrimario");
                }
                else if (completo)
                {
                    circulo.SetResourceReference(Border.BorderBrushProperty, "Acento");
                    numero.SetResourceReference(TextBlock.ForegroundProperty, "Acento");
                }
                else
                {
                    circulo.SetResourceReference(Border.BorderBrushProperty, "BordeCampo");
                    numero.SetResourceReference(TextBlock.ForegroundProperty, "TextoSecundario");
                }

                var texto = new TextBlock
                {
                    Text = pasos[i],
                    FontSize = 13,
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = esActual ? FontWeights.SemiBold : FontWeights.Normal,
                };
                texto.SetResourceReference(TextBlock.ForegroundProperty, esActual ? "Texto" : "TextoSecundario");

                var paso = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 16, 4),
                };
                paso.Children.Add(circulo);
                paso.Children.Add(texto);

                string estado = completo ? "completo" : esActual ? "actual" : "pendiente";
                AutomationProperties.SetName(paso, $"Paso {i + 1} de {pasos.Count}: {pasos[i]}, {estado}");

                Children.Add(paso);
            }
        }
    }
}
