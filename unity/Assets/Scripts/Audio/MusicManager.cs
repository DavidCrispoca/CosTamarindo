using System.Collections.Generic;
using UnityEngine;

namespace CosTamarindo
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Configuración de Pistas")]
        [Tooltip("Arrastra aquí todas tus pistas de música")]
        public List<AudioClip> playlist = new List<AudioClip>();

        [Tooltip("Si está activo, las canciones se intercalan al azar sin repetir la anterior")]
        public bool modoAleatorio = true;

        [Range(0f, 1f)]
        public float volumen = 0.5f;

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
            audioSource.volume = volumen;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (playlist.Count > 0)
            {
                ReproducirSiguientePista();
            }
        }

        // Update is called once per frame
        void Update()
        {
            audioSource.volume = volumen;

            if (playlist.Count > 0 && !audioSource.isPlaying && Application.isFocused)
            {
                ReproducirSiguientePista();
            }
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
            audioSource.Play();
        }
    }
}