using System;
using UnityEngine;

namespace CosTamarindo.Audio
{
    /// <summary>
    /// Volumen y silencio de música y efectos en un solo lugar.
    ///
    /// Antes la música se fijaba en el Inspector y los efectos se reproducían
    /// siempre a volumen completo con <c>PlayOneShot</c>, así que en la pausa
    /// sonaban las dos capas al 100 %. Aquí cada canal tiene su propio valor,
    /// se puede silenciar por separado y la preferencia sobrevive a la partida.
    ///
    /// PlayerPrefs solo guarda preferencias de sonido (nunca puntajes): en WebGL
    /// vive en IndexedDB y el jugador puede editarlo sin afectar la competencia.
    /// </summary>
    public static class AudioSettingsService
    {
        private const string MusicVolumeKey = "ct.audio.music_volume";
        private const string SfxVolumeKey = "ct.audio.sfx_volume";
        private const string MusicMutedKey = "ct.audio.music_muted";
        private const string SfxMutedKey = "ct.audio.sfx_muted";

        // Valores iniciales distintos a propósito: no todo a la vez.
        private const float DefaultMusicVolume = 0.45f;
        private const float DefaultSfxVolume = 0.65f;

        /// <summary>Avisa a la música y a la UI cuando cambia algo.</summary>
        public static event Action Changed;

        public static float MusicVolume { get; private set; }
        public static float SfxVolume { get; private set; }
        public static bool MusicMuted { get; private set; }
        public static bool SfxMuted { get; private set; }

        /// <summary>Volumen real que debe tener el AudioSource de la música.</summary>
        public static float MusicOutput => MusicMuted ? 0f : MusicVolume;

        /// <summary>Volumen real con el que se reproducen los efectos.</summary>
        public static float SfxOutput => SfxMuted ? 0f : SfxVolume;

        static AudioSettingsService()
        {
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
            MusicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
            SfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
        }

        public static void SetMusicVolume(float value)
        {
            float next = Mathf.Clamp01(value);
            if (Mathf.Approximately(MusicVolume, next)) return;

            MusicVolume = next;
            PlayerPrefs.SetFloat(MusicVolumeKey, next);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetSfxVolume(float value)
        {
            float next = Mathf.Clamp01(value);
            if (Mathf.Approximately(SfxVolume, next)) return;

            SfxVolume = next;
            PlayerPrefs.SetFloat(SfxVolumeKey, next);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetMusicMuted(bool value)
        {
            if (MusicMuted == value) return;

            MusicMuted = value;
            PlayerPrefs.SetInt(MusicMutedKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetSfxMuted(bool value)
        {
            if (SfxMuted == value) return;

            SfxMuted = value;
            PlayerPrefs.SetInt(SfxMutedKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void ToggleMusicMute() => SetMusicMuted(!MusicMuted);

        public static void ToggleSfxMute() => SetSfxMuted(!SfxMuted);

        /// <summary>
        /// Reproduce un efecto con el volumen del canal de efectos. Si está
        /// silenciado no crea ninguna voz: evita que siga sonando "de fondo".
        /// </summary>
        public static void PlaySfx(AudioSource source, AudioClip clip)
        {
            if (source == null || clip == null) return;

            float volume = SfxOutput;
            if (volume <= 0f) return;

            source.PlayOneShot(clip, volume);
        }
    }
}
