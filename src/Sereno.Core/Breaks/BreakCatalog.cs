using System;
using System.Collections.Generic;
using System.Linq;

namespace Sereno.Core.Breaks
{
    /// <summary>Catálogo de micropausas concretas. Incluye la regla 20-20-20 y una rutina de pasos.</summary>
    public static class BreakCatalog
    {
        public static IReadOnlyList<BreakSuggestion> Todas { get; } = new[]
        {
            new BreakSuggestion("Descanso visual 20-20-20", "👁️", 20,
                "Mirá algo a unos 6 metros de distancia durante 20 segundos.", Array.Empty<BreakStep>()),
            new BreakSuggestion("Estirar el cuello", "🙆", 60,
                "Llevá la oreja hacia cada hombro, 15 segundos por lado.", Array.Empty<BreakStep>()),
            new BreakSuggestion("Ponerse de pie", "🧍", 120,
                "Levantate, estirá los brazos y caminá unos pasos.", Array.Empty<BreakStep>()),
            new BreakSuggestion("Respiración lenta", "🌬️", 120,
                "Inhalá 4 segundos y exhalá 6 segundos, cinco veces.", Array.Empty<BreakStep>()),
            new BreakSuggestion("Tomar agua", "💧", 60,
                "Servite un vaso de agua y tomalo despacio.", Array.Empty<BreakStep>()),
            new BreakSuggestion("Estirar la espalda", "🧘", 30,
                "Rutina guiada en tres pasos.", new[]
                {
                    new BreakStep("Sentate derecho y soltá los hombros.", 10),
                    new BreakStep("Llevá los brazos arriba y estirate.", 10),
                    new BreakStep("Inclinate hacia cada lado.", 10),
                }),
        };

        /// <summary>Micropausas que entran en la duración de pausa configurada.</summary>
        public static IReadOnlyList<BreakSuggestion> Para(int minutosPausa) =>
            Todas.Where(s => s.Segundos <= minutosPausa * 60).ToList();
    }
}
