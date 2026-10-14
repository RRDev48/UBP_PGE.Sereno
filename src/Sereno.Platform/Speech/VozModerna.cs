using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;

namespace Sereno.Platform.Speech
{
    public sealed record VozInfo(string Id, string Nombre);

    /// <summary>
    /// Voces modernas en español vía Windows.Media.SpeechSynthesis (WinRT), más naturales que la
    /// voz clásica de LectorVoz. HablarAsync devuelve false ante cualquier falla de síntesis o
    /// reproducción: quien la usa cae entonces a LectorVoz, sin romper el aviso.
    /// </summary>
    public sealed class VozModerna : IDisposable
    {
        private CancellationTokenSource? _cancelacion;
        private SoundPlayer? _reproductor;

        /// <summary>Voces en español disponibles. Lista vacía si WinRT no está disponible.</summary>
        public static IReadOnlyList<VozInfo> ListarVoces()
        {
            try
            {
                return SpeechSynthesizer.AllVoices
                    .Where(v => v.Language.StartsWith("es", StringComparison.OrdinalIgnoreCase))
                    .Select(v => new VozInfo(v.Id, v.DisplayName))
                    .ToList();
            }
            catch (Exception)
            {
                return Array.Empty<VozInfo>();
            }
        }

        /// <summary>Sintetiza y reproduce. Devuelve false si falló algo (voz no encontrada, sin audio, etc.).</summary>
        public async Task<bool> HablarAsync(string vozId, string texto)
        {
            Callar();
            _cancelacion = new CancellationTokenSource();
            CancellationToken token = _cancelacion.Token;

            try
            {
                var sintetizador = new SpeechSynthesizer();
                VoiceInformation? voz = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Id == vozId);
                if (voz is not null) sintetizador.Voice = voz;

                using SpeechSynthesisStream audio = await sintetizador.SynthesizeTextToStreamAsync(texto).AsTask(token);

                using (Stream flujo = audio.AsStreamForRead())
                {
                    _reproductor = new SoundPlayer(flujo);
                    _reproductor.Load();
                }

                if (token.IsCancellationRequested) return true;
                _reproductor.Play();
                return true;
            }
            catch (OperationCanceledException)
            {
                return true; // se pidió cortar a propósito; no es una falla
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Callar()
        {
            _cancelacion?.Cancel();
            _reproductor?.Stop();
        }

        public void Dispose()
        {
            Callar();
            _reproductor?.Dispose();
            _cancelacion?.Dispose();
        }
    }
}
