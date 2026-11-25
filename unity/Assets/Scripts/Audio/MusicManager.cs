using System.Collections.Generic;
using UnityEngine;
using CosTamarindo.Audio;

namespace CosTamarindo
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Configuración de pistas")]
        [Tooltip("Arrastra aquí todas tus pistas de música")]
        public List<AudioClip> playlist = new List<AudioClip>();

        [Tooltip("Si está activo, las canciones se intercalan al azar sin repetir la anterior")]
        public bool modoAleatorio = true;

        private AudioSource audioSource;
        private int indiceActual = -1;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            ApplyVolume();

            // El volumen ya no se fija aquí: lo controla el jugador desde la pausa.
            AudioSettingsService.Changed += ApplyVolume;
        }

        void Start()
        {
            if (playlist.Count > 0)
            {
                ReproducirSiguientePista();
            }
        }

        void Update()
        {
            if (playlist.Count > 0 && !audioSource.isPlaying && Application.isFocused)
            {
                ReproducirSiguientePista();
            }
        }

        void OnDestroy()
        {
            AudioSettingsService.Changed -= ApplyVolume;
        }

        private void ApplyVolume()
        {
            if (audioSource != null) audioSource.volume = AudioSettingsService.MusicOutput;
        }

        public void ReproducirSiguientePista()
        {
            if (playlist.Count == 0) return;

            if (modoAleatorio && playlist.Count > 1)
            {
                int nuevoIndice;
                do
                {
                    nuevoIndice = Random.Range(0, playlist.Count);
                } while (nuevoIndice == indiceActual);

                indiceActual = nuevoIndice;
            }
            else
            {
                indiceActual = (indiceActual + 1) % playlist.Count;
            }

            audioSource.clip = playlist[indiceActual];
            ApplyVolume();
            audioSource.Play();
        }
    }
}
