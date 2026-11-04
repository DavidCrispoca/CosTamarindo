using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CosTamarindo.Core;
using CosTamarindo.Audio;

namespace CosTamarindo.UI
{
    /// <summary>
    /// Retroalimentación al atrapar un ítem: texto flotante (+15 / -10 / -1),
    /// destello del marcador, aviso «¡Nuevo!» y un efecto de sonido.
    ///
    /// Se engancha al evento <see cref="GameManager.ItemCaught"/>, que ya expone
    /// si fue el primer desbloqueo. No conoce reglas: solo presenta.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CaptureFeedback : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private RectTransform floatingRoot;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Transform player;
        [SerializeField] private AudioSource audioSource;

        [Header("Presentación")]
        [SerializeField] private float riseDistance = 120f;
        [SerializeField] private float lifetime = 0.9f;
        [SerializeField] private float fontSize = 54f;

        private static readonly Color GoodColor = new Color(0.18f, 0.62f, 0.28f);
        private static readonly Color BadColor = new Color(0.82f, 0.22f, 0.24f);
        private static readonly Color NewColor = new Color(0.95f, 0.62f, 0.12f);

        private bool subscribed;

        private void Start()
        {
            TrySubscribe();
        }

        private void Update()
        {
            // Igual que el spawner: el orden de Awake/OnEnable no está garantizado,
            // así que la suscripción se reintenta hasta que GameManager exista.
            TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (subscribed || GameManager.Instance == null) return;

            GameManager.Instance.ItemCaught += HandleItemCaught;
            subscribed = true;
        }

        private void OnDestroy()
        {
            if (subscribed && GameManager.Instance != null)
                GameManager.Instance.ItemCaught -= HandleItemCaught;
        }

        private void HandleItemCaught(ItemDef def, bool firstUnlock)
        {
            if (def == null) return;

            string label;
            Color color;

            if (def.CostsLife)
            {
                label = "-1 \u2665";
                color = BadColor;
                Play(ProceduralSfx.Danger);
            }
            else if (def.Kind == ItemKind.Trash)
            {
                label = def.Points.ToString();
                color = BadColor;
                Play(ProceduralSfx.Danger);
            }
            else
            {
                label = "+" + def.Points;
                color = GoodColor;
                Play(firstUnlock ? ProceduralSfx.Unlock : ProceduralSfx.Catch);
            }

            SpawnLabel(label, color);
            FlashScore();

            if (firstUnlock)
                StartCoroutine(SpawnNewBadge());
        }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        private void SpawnLabel(string label, Color color)
        {
            if (floatingRoot == null) return;

            var go = new GameObject("FloatingLabel", typeof(RectTransform));
            go.transform.SetParent(floatingRoot, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            if (font != null) text.font = font;

            var rt = text.rectTransform;
            rt.sizeDelta = new Vector2(320f, 80f);

            rt.anchoredPosition = WorldToCanvas(player != null ? player.position : Vector3.zero)
                                  + new Vector2(Random.Range(-40f, 40f), 120f);

            StartCoroutine(AnimateLabel(rt, text));
        }

        private IEnumerator AnimateLabel(RectTransform rt, TMP_Text text)
        {
            Vector2 start = rt.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < lifetime && rt != null && text != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);

                rt.anchoredPosition = start + new Vector2(0f, riseDistance * t);

                // Aparece de golpe y se desvanece en el último tercio.
                var c = text.color;
                c.a = t < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.65f) / 0.35f);
                text.color = c;
                text.fontSize = fontSize * Mathf.Lerp(1.15f, 1f, Mathf.Min(1f, t * 4f));

                yield return null;
            }

            if (rt != null) Destroy(rt.gameObject);
        }

        private IEnumerator SpawnNewBadge()
        {
            if (floatingRoot == null) yield break;

            var go = new GameObject("NewBadge", typeof(RectTransform));
            go.transform.SetParent(floatingRoot, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = "¡Nuevo!";
            text.fontSize = 42f;
            text.fontStyle = FontStyles.Bold;
            text.color = NewColor;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            if (font != null) text.font = font;

            var rt = text.rectTransform;
            rt.sizeDelta = new Vector2(320f, 60f);
            rt.anchoredPosition = new Vector2(0f, -260f);

            float elapsed = 0f;
            const float hold = 1.1f;

            // Rebote de entrada (escala) y desvanecido final.
            while (elapsed < hold && rt != null)
            {
                elapsed += Time.unscaledDeltaTime;

                float pop = Mathf.Min(1f, elapsed / 0.22f);
                float scale = Mathf.Lerp(0.6f, 1.08f, pop) - Mathf.Sin(pop * Mathf.PI) * 0.08f;
                rt.localScale = Vector3.one * scale;

                var c = text.color;
                c.a = elapsed > hold - 0.3f ? Mathf.Lerp(1f, 0f, (elapsed - (hold - 0.3f)) / 0.3f) : 1f;
                text.color = c;

                yield return null;
            }

            if (rt != null) Destroy(rt.gameObject);
        }

        private void FlashScore()
        {
            if (scoreText != null) StartCoroutine(AnimateFlash(scoreText.rectTransform));
        }

        private IEnumerator AnimateFlash(RectTransform rt)
        {
            float elapsed = 0f;
            const float duration = 0.18f;

            while (elapsed < duration && rt != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Pico de escala a mitad de camino y vuelta a 1.
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.16f;
                rt.localScale = Vector3.one * scale;
                yield return null;
            }

            if (rt != null) rt.localScale = Vector3.one;
        }

        /// <summary>
        /// Convierte una posición de mundo a coordenadas del canvas de UI.
        /// El canvas es Screen Space - Overlay, así que la cámara del cálculo es null.
        /// </summary>
        private Vector2 WorldToCanvas(Vector3 world)
        {
            var cam = Camera.main;
            if (cam == null || floatingRoot == null) return Vector2.zero;

            Vector2 screen = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(floatingRoot, screen, null, out Vector2 local);
            return local;
        }
    }
}
