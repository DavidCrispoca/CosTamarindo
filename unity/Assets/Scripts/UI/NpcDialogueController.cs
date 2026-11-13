using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CosTamarindo.Auth;
using CosTamarindo.Core;
using CosTamarindo.Gameplay;

namespace CosTamarindo.UI
{
    /// <summary>
    /// Comentarios de la Palenquera durante la partida.
    ///
    /// Se engancha a <see cref="GameManager.ItemCaught"/> y, de vez en cuando,
    /// pide una frase a <c>POST /api/dialog</c>. El evento se envía con el
    /// nombre y el tipo de ítem, sin texto del jugador, para que la IA comente
    /// la captura. Si no hay backend, usa frases locales; el juego nunca se
    /// detiene por la IA.
    ///
    /// La UI se construye en runtime para no depender de prefabs ni de edits
    /// manuales en la escena Game.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcDialogueController : MonoBehaviour
    {
        private const string DialogUrl = "https://juego.local/api/dialog";

        [Header("Ritmo")]
        [SerializeField] private float commentChance = 0.35f;
        [SerializeField] private float cooldownSeconds = 9f;
        [SerializeField] private float visibleSeconds = 5.5f;
        [SerializeField] private float requestTimeoutSeconds = 12f;

        private static readonly Dictionary<string, string> ItemLines =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Tamarindo", "¡Qué buenos tamarindos! Como pa' un juguito." },
                { "Cocada", "Esa cocada sí endulza la partida, mi amor." },
                { "Pescado", "Buen pescado, pero ojo con las espinas." },
                { "Frutas", "Frutas frescas, buena cosecha la de hoy." },
                { "Limonada de coco", "Limonada de coco, refresco seguro para seguir jugando." },
                { "Erizo", "¡Ay! Ese erizo no era para comer." },
                { "Medusa", "Esa medusa te dejó sin vida. ¡A esquivar la próxima!" },
                { "Basura", "Eso no se come, eso contamina. Mejor recoge fruta." },
                { "Bomba", "¡No puede ser! Esa bomba te dañó la cosecha de hoy." }
            };

        private CanvasGroup bubbleGroup;
        private TMP_Text bodyText;
        private Coroutine hideRoutine;
        private GameManager gameManager;
        private float nextAllowedAt;
        private bool requestInFlight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "Game") return;
            if (GameManager.Instance == null) return;
            if (FindFirstObjectByType<NpcDialogueController>() != null) return;

            var go = new GameObject("NpcDialogue");
            go.AddComponent<NpcDialogueController>();
        }

        private void Awake()
        {
            BuildUi();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void Update()
        {
            TrySubscribe();
        }

        private void OnDestroy()
        {
            if (gameManager == null) return;
            gameManager.ItemCaught -= OnItemCaught;
            gameManager.MatchEnded -= OnMatchEnded;
        }

        private void TrySubscribe()
        {
            if (gameManager != null) return;
            gameManager = GameManager.Instance;
            if (gameManager == null) return;

            gameManager.ItemCaught += OnItemCaught;
            gameManager.MatchEnded += OnMatchEnded;
        }

        private void OnItemCaught(ItemDef def, bool firstUnlock)
        {
            if (gameManager == null || gameManager.State != GameState.Playing || def == null) return;
            if (Time.unscaledTime < nextAllowedAt) return;
            if (UnityEngine.Random.value > commentChance) return;

            nextAllowedAt = Time.unscaledTime + cooldownSeconds;

            string context = def.CostsLife || def.Kind == ItemKind.Trash ? "peligro" : "captura";
            string kind = def.Kind == ItemKind.Good
                ? "good"
                : def.Kind == ItemKind.Danger ? "danger" : "trash";

            StartCoroutine(RequestLine(context, def.DisplayName, kind, string.Empty));
        }

        private void OnMatchEnded(int score)
        {
            StartCoroutine(RequestLine("game_over", null, null, string.Empty));
        }

        private IEnumerator RequestLine(string context, string item, string kind, string text)
        {
            if (requestInFlight) yield break;

            string fallback = LocalLine(context, item);
            if (!AuthStub.IsBackendToken)
            {
                ShowLine(fallback);
                yield break;
            }

            requestInFlight = true;
            string json = BuildPayload(text, context, item, kind);
            var request = new UnityWebRequest(DialogUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Mathf.Max(1, Mathf.RoundToInt(requestTimeoutSeconds))
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + AuthStub.SessionToken);

            yield return request.SendWebRequest();

            string reply = null;
            if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
            {
                try
                {
                    var response = JsonUtility.FromJson<DialogResponse>(request.downloadHandler.text);
                    if (response != null && !string.IsNullOrEmpty(response.reply))
                        reply = response.reply;
                }
                catch (ArgumentException)
                {
                    reply = null;
                }
            }

            request.Dispose();
            requestInFlight = false;
            ShowLine(string.IsNullOrEmpty(reply) ? fallback : reply);
        }

        private string LocalLine(string context, string item)
        {
            if (!string.IsNullOrEmpty(item) && ItemLines.TryGetValue(item, out string line))
                return line;

            switch (context)
            {
                case "captura":
                    return "Buena pesca. Cada cosa que atrapas cuenta.";
                case "peligro":
                    return "Eso pica. No gastes las vidas en el mismo erizo.";
                case "game_over":
                    return "Se acabó la pesca por hoy. Revisa tu Libro y vuelve a intentarlo.";
                default:
                    return "La Palenquera está revisando los registros. ¡Sigue jugando!";
            }
        }

        private static string BuildPayload(string text, string context, string item, string kind)
        {
            var parts = new List<string>
            {
                "\"text\":" + Quote(text ?? string.Empty),
                "\"context\":" + Quote(context ?? "defecto")
            };

            if (!string.IsNullOrEmpty(item)) parts.Add("\"item\":" + Quote(item));
            if (!string.IsNullOrEmpty(kind)) parts.Add("\"kind\":" + Quote(kind));

            return "{" + string.Join(",", parts) + "}";
        }

        private static string Quote(string value)
        {
            return "\"" + value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r") + "\"";
        }

        private void ShowLine(string line)
        {
            if (bodyText == null || bubbleGroup == null || string.IsNullOrEmpty(line)) return;

            bodyText.text = line;

            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            bubbleGroup.alpha = 0f;
            bubbleGroup.blocksRaycasts = false;
            bubbleGroup.interactable = false;

            float elapsed = 0f;
            const float fadeIn = 0.18f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.unscaledDeltaTime;
                bubbleGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
                yield return null;
            }
            bubbleGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(visibleSeconds);

            elapsed = 0f;
            const float fadeOut = 0.35f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                bubbleGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
                yield return null;
            }
            bubbleGroup.alpha = 0f;
            hideRoutine = null;
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject(
                "NpcDialogueCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var bubbleGo = new GameObject(
                "Bubble",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));
            bubbleGo.transform.SetParent(canvasGo.transform, false);

            var background = bubbleGo.GetComponent<Image>();
            background.color = new Color(0.07f, 0.12f, 0.16f, 0.93f);
            background.raycastTarget = false;

            var bubbleRect = bubbleGo.GetComponent<RectTransform>();
            bubbleRect.anchorMin = new Vector2(0.5f, 0f);
            bubbleRect.anchorMax = new Vector2(0.5f, 0f);
            bubbleRect.pivot = new Vector2(0.5f, 0f);
            bubbleRect.anchoredPosition = new Vector2(0f, 72f);
            bubbleRect.sizeDelta = new Vector2(900f, 220f);

            bubbleGroup = bubbleGo.GetComponent<CanvasGroup>();
            bubbleGroup.alpha = 0f;
            bubbleGroup.blocksRaycasts = false;
            bubbleGroup.interactable = false;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(bubbleGo.transform, false);
            var nameText = nameGo.GetComponent<TextMeshProUGUI>();
            nameText.text = "La Palenquera";
            nameText.fontSize = 30f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = new Color(1f, 0.78f, 0.25f);
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null) nameText.font = TMP_Settings.defaultFontAsset;

            var nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.offsetMin = new Vector2(28f, -48f);
            nameRect.offsetMax = new Vector2(-28f, -14f);

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(bubbleGo.transform, false);
            bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
            bodyText.fontSize = 38f;
            bodyText.color = Color.white;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.enableWordWrapping = true;
            bodyText.overflowMode = TextOverflowModes.Truncate;
            bodyText.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null) bodyText.font = TMP_Settings.defaultFontAsset;

            var bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(28f, 22f);
            bodyRect.offsetMax = new Vector2(-28f, -54f);

            var shadow = bodyGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        [Serializable]
        private sealed class DialogResponse
        {
            public string reply;
            public bool blocked;
        }
    }
}
