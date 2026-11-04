using UnityEngine;

namespace CosTamarindo.Audio
{
    /// <summary>
    /// Genera los efectos de sonido en memoria en lugar de importar archivos.
    ///
    /// Por qué: los SFX de Figma no están exportados (F1 sigue bloqueada) y el
    /// proyecto no debe quedar sin audio por eso. Un barrido de frecuencia con
    /// envolvente de ataque/decaimiento da un sonido limpio y sin clics, pesa
    /// cero bytes en el build y no añade dependencias.
    ///
    /// Cuando F1 entregue los SFX reales, se reemplaza esta clase: los llamadores
    /// solo usan <see cref="Catch"/>, <see cref="Danger"/> y <see cref="Unlock"/>.
    /// </summary>
    public static class ProceduralSfx
    {
        private const int SampleRate = 44100;

        private static AudioClip catchClip;
        private static AudioClip dangerClip;
        private static AudioClip unlockClip;

        /// <summary>Blip ascendente al atrapar un ítem bueno.</summary>
        public static AudioClip Catch =>
            catchClip != null ? catchClip : catchClip = Build("sfx_catch", 0.11f, 780f, 1240f, 0.30f);

        /// <summary>Zumbido descendente al chocar con un peligro o la basura.</summary>
        public static AudioClip Danger =>
            dangerClip != null ? dangerClip : dangerClip = Build("sfx_danger", 0.20f, 300f, 120f, 0.32f);

        /// <summary>Arpegio corto al descubrir un ítem nuevo (primera vez).</summary>
        public static AudioClip Unlock =>
            unlockClip != null ? unlockClip : unlockClip = Build("sfx_unlock", 0.34f, 660f, 1760f, 0.26f);

        /// <summary>
        /// Barrido lineal de frecuencia con envolvente ADSR simplificada
        /// (ataque corto, caída exponencial) para que no haya chasquidos.
        /// </summary>
        private static AudioClip Build(string name, float duration, float startHz, float endHz, float amplitude)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];

            const float attackFraction = 0.08f;
            int attackSamples = Mathf.Max(1, Mathf.RoundToInt(samples * attackFraction));

            double phase = 0.0;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;

                // Frecuencia interpolada linealmente entre los dos extremos.
                float hz = Mathf.Lerp(startHz, endHz, t);
                phase += 2.0 * Mathf.PI * hz / SampleRate;

                // Ataque lineal y caída exponencial: sin discontinuidad en los bordes.
                float envelope = i < attackSamples
                    ? (float)i / attackSamples
                    : Mathf.Exp(-4f * (i - attackSamples) / Mathf.Max(1, samples - attackSamples));

                // Un armónico suave le quita el timbre de "seno puro" de test.
                float wave = Mathf.Sin((float)phase) + 0.25f * Mathf.Sin((float)phase * 2f);

                data[i] = wave * envelope * amplitude;
            }

            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
