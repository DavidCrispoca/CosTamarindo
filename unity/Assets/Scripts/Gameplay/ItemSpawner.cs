using System.Collections.Generic;
using UnityEngine;
using CosTamarindo.Core;

namespace CosTamarindo.Gameplay
{
    /// <summary>
    /// Genera los ítems, aplica la curva de dificultad y decide si un ítem
    /// fue capturado o se perdió. Es el único componente que mueve ítems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemSpawner : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private PlayerController player;
        [Tooltip("Sprites de ítems. El nombre del sprite debe coincidir con el Id del catálogo.")]
        [SerializeField] private Sprite[] itemSprites;
        [SerializeField] private Transform itemsRoot;

        [Header("Curva de dificultad")]
        [Tooltip("Intervalo entre apariciones: inicio → final de la partida.")]
        [SerializeField] private Vector2 spawnInterval = new Vector2(0.95f, 0.38f);
        [Tooltip("Velocidad de caída: inicio → final de la partida.")]
        [SerializeField] private Vector2 fallSpeed = new Vector2(3.6f, 7.5f);
        [SerializeField] private float matchDurationSeconds = 120f;

        [Header("Zona de captura")]
        [Tooltip("Margen vertical de la ventana de captura, en unidades de mundo.")]
        [SerializeField] private float catchWindow = 1.4f;

        [Header("Presentación")]
        [Tooltip("Lado mayor del ítem en unidades de mundo. Normaliza assets de distinto tamaño.")]
        [SerializeField] private float itemWorldSize = 1.05f;

        private readonly List<FallingItem> active = new List<FallingItem>();
        private readonly Dictionary<string, Sprite> spritesById = new Dictionary<string, Sprite>();

        private Camera cam;
        private System.Random rng;
        private float nextSpawnAt;
        private float elapsed;
        private bool running;
        private bool subscribed;

        private float HalfWidth => cam != null ? cam.orthographicSize * cam.aspect : 8f;
        private float SpawnY => cam != null ? cam.orthographicSize + 1.5f : 6.5f;
        private float DespawnY => cam != null ? -cam.orthographicSize - 1.5f : -6.5f;

        private void Awake()
        {
            cam = Camera.main;
            rng = new System.Random();

            spritesById.Clear();
            if (itemSprites != null)
            {
                foreach (var s in itemSprites)
                    if (s != null) spritesById[s.name] = s;
            }

            if (itemsRoot == null)
            {
                var root = new GameObject("Items");
                root.transform.SetParent(transform, false);
                itemsRoot = root.transform;
            }
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
        }

        /// <summary>
        /// Se suscribe al GameManager cuando ya existe. No basta con OnEnable:
        /// el orden de Awake/OnEnable entre objetos no está garantizado y el
        /// spawner puede activarse antes de que GameManager.Instance exista.
        /// </summary>
        private void TrySubscribe()
        {
            if (subscribed || GameManager.Instance == null) return;

            GameManager.Instance.StateChanged += HandleStateChanged;
            GameManager.Instance.MatchEnded += HandleMatchEnded;
            subscribed = true;

            // Si la partida ya estaba en curso, sincroniza el estado actual.
            HandleStateChanged(GameManager.Instance.State);
        }

        private void OnDisable()
        {
            if (subscribed && GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= HandleStateChanged;
                GameManager.Instance.MatchEnded -= HandleMatchEnded;
            }

            subscribed = false;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                if (!running) ResetRun();
                running = true;
            }
            else
            {
                running = false;
            }
        }

        private void HandleMatchEnded(int _) => ClearItems();

        private void ResetRun()
        {
            ClearItems();
            elapsed = 0f;
            nextSpawnAt = 0.6f;
        }

        private void Update()
        {
            TrySubscribe();

            if (!running || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            float dt = Time.deltaTime;
            elapsed += dt;

            if (elapsed >= nextSpawnAt) SpawnOne();

            TickItems(dt);
        }

        private float Difficulty01 => Mathf.Clamp01(elapsed / Mathf.Max(1f, matchDurationSeconds));

        private void SpawnOne()
        {
            float t = Difficulty01;
            nextSpawnAt = elapsed + Mathf.Lerp(spawnInterval.x, spawnInterval.y, t);

            var def = ItemCatalog.Roll(rng);
            var go = new GameObject(def.Id);
            go.transform.SetParent(itemsRoot, false);

            float x = Random.Range(-HalfWidth + 0.9f, HalfWidth - 0.9f);
            go.transform.position = new Vector3(x, SpawnY, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ResolveSprite(def);
            sr.sortingOrder = 10;

            NormalizeScale(go.transform, sr.sprite);

            var item = go.AddComponent<FallingItem>();
            float speed = Mathf.Lerp(fallSpeed.x, fallSpeed.y, t) * def.FallSpeedMultiplier;
            item.Initialize(def, speed, Random.Range(-40f, 40f));

            active.Add(item);
        }

        private Sprite ResolveSprite(ItemDef def)
        {
            if (spritesById.TryGetValue(def.Id, out var sprite) && sprite != null) return sprite;
            return PlaceholderSprites.For(def.Kind);
        }

        /// <summary>
        /// Ajusta la escala para que todo ítem ocupe el mismo espacio visible,
        /// sin importar el tamaño en píxeles con el que se exportó de Figma.
        /// </summary>
        private void NormalizeScale(Transform target, Sprite sprite)
        {
            if (sprite == null) return;

            var size = sprite.bounds.size;
            float largest = Mathf.Max(size.x, size.y);
            if (largest <= 0.001f) return;

            target.localScale = Vector3.one * (itemWorldSize / largest);
        }

        private void TickItems(float dt)
        {
            if (player == null) return;

            float catchY = player.CatchHeight;
            float halfCatch = player.CatchHalfWidth + 0.25f;
            float bottom = DespawnY;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var item = active[i];
                if (item == null) { active.RemoveAt(i); continue; }

                float before = item.transform.position.y;
                bool expired = item.Advance(dt, bottom);

                // ¿Cruzó la línea de captura en este fotograma?
                bool crossed = before >= catchY && item.transform.position.y <= catchY;
                bool insideWindow = item.transform.position.y > catchY - catchWindow;

                if ((crossed || (item.transform.position.y <= catchY && insideWindow)) && player != null)
                {
                    float dx = Mathf.Abs(item.transform.position.x - player.transform.position.x);
                    if (dx <= halfCatch)
                    {
                        GameManager.Instance?.RegisterCatch(item.Definition);
                        Destroy(item.gameObject);
                        active.RemoveAt(i);
                        continue;
                    }
                }

                if (expired)
                {
                    Destroy(item.gameObject);
                    active.RemoveAt(i);
                }
            }
        }

        private void ClearItems()
        {
            foreach (var item in active)
                if (item != null) Destroy(item.gameObject);
            active.Clear();
        }
    }
}
