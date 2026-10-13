using System;
using System.Linq;
using System.Speech.Synthesis;

namespace Sereno.Platform.Speech
{
    /// <summary>
    /// Lee avisos en voz alta con una voz en español instalada en Windows. Hablar reemplaza lo que
    /// se estuviera diciendo; Callar lo interrumpe. Sin voz en español, Hablar no hace nada.
    /// </summary>
    public sealed class LectorVoz : IDisposable
    {
        private readonly SpeechSynthesizer? _sintetizador;

        public LectorVoz()
        {
            using var consulta = new SpeechSynthesizer();
            InstalledVoice? voz = consulta.GetInstalledVoices().FirstOrDefault(EsEspanola);
            if (voz is null) return;

            _sintetizador = new SpeechSynthesizer();
            _sintetizador.SelectVoice(voz.VoiceInfo.Name);
        }

        public static bool HayVozEnEspanol()
        {
            using var consulta = new SpeechSynthesizer();
            return consulta.GetInstalledVoices().Any(EsEspanola);
        }

        public void Hablar(string texto)
        {
            if (_sintetizador is null) return;
            _sintetizador.SpeakAsyncCancelAll();
            _sintetizador.SpeakAsync(texto);
        }

        public void Callar() => _sintetizador?.SpeakAsyncCancelAll();

        public void Dispose() => _sintetizador?.Dispose();

        private static bool EsEspanola(InstalledVoice voz) =>
            voz.Enabled && voz.VoiceInfo.Culture.TwoLetterISOLanguageName == "es";
    }
}
