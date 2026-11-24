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

        private static string GetDialogUrl() =>
            Application.platform == RuntimePlatform.WebGLPlayer ? "/api/dialog" : DialogUrl;

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

        private const float BubbleWidth = 780f;
        private const float BubbleHeight = 232f;
        private const float HeadGap = 78f;
        private const float EdgeMargin = 28f;

        private CanvasGroup bubbleGroup;
        private CanvasGroup thoughtGroup;
        private RectTransform canvasRect;
        private RectTransform bubbleRect;
        private RectTransform thoughtLayer;
        private RectTransform gameOverPanel;
        private readonly List<Image> thoughtDots = new List<Image>();
        private TMP_Text bodyText;
        private TMP_Text historyText;
        private Coroutine hideRoutine;
        private Coroutine typingRoutine;
        private string lastLine;
        private readonly Queue<string> history = new Queue<string>(HistorySize);
        private const int HistorySize = 2;
        private GameManager gameManager;
        private PlayerController player;
        private SpriteRenderer playerSprite;
        private Camera worldCamera;
        private float nextAllowedAt;
        private bool requestInFlight;

        // Auto-bootstrap desactivado temporalmente: en WebGL producía un crash de stack
        // al cargar. El diálogo NPC se puede reactivar cuando se estabilice la escena.
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // private static void RegisterSceneHook() { ... }
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        // private static void Bootstrap() { ... }
        // private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { ... }

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

        private void LateUpdate()
        {
            if (playerSprite == null) TryFindPlayer();
            UpdateBubblePosition();
        }

        private void TryFindPlayer()
        {
            if (playerSprite != null) return;

            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            playerSprite = player.GetComponentInChildren<SpriteRenderer>();
        }

        private void UpdateBubblePosition()
        {
            if (bubbleRect == null || canvasRect == null) return;

            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;
            if (canvasWidth <= 1f || canvasHeight <= 1f) return;

            if (playerSprite == null)
            {
                // Fallback mientras aparece el jugador: abajo, sin tapar el HUD.
                bubbleRect.anchoredPosition = new Vector2(0f, -canvasHeight * 0.5f + 120f);
                return;
            }

            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null) return;

            Bounds spriteBounds = playerSprite.bounds;
            Vector3 screenHead = worldCamera.WorldToScreenPoint(
                new Vector3(spriteBounds.center.x, spriteBounds.max.y, spriteBounds.center.z));
            if (screenHead.z < 0f) return;

            Vector2 headLocal;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenHead, null, out headLocal))
                return;

            if (gameManager != null && gameManager.State == GameState.GameOver)
            {
                TryFindGameOverPanel();
                if (gameOverPanel != null && TryPlaceAboveGameOverPanel()) return;
            }

            float maxBubbleWidth = Mathf.Max(320f, canvasWidth - EdgeMargin * 2f);
            if (bubbleRect.sizeDelta.x > maxBubbleWidth)
                bubbleRect.sizeDelta = new Vector2(maxBubbleWidth, bubbleRect.sizeDelta.y);

            float halfWidth = bubbleRect.sizeDelta.x * 0.5f;
            float minX = -canvasWidth * 0.5f + halfWidth + EdgeMargin;
            float maxX = canvasWidth * 0.5f - halfWidth - EdgeMargin;
            float x = minX <= maxX ? Mathf.Clamp(headLocal.x, minX, maxX) : 0f;

            // El pivote del cuadro está abajo: queda justo encima de la cabeza.
            float y = headLocal.y + HeadGap;
            float maxY = canvasHeight * 0.5f - bubbleRect.sizeDelta.y - EdgeMargin;
            y = Mathf.Min(y, maxY);

            bubbleRect.anchoredPosition = new Vector2(x, y);
            UpdateThoughtDots(x, y, headLocal);
        }

        private void TryFindGameOverPanel()
        {
            if (gameOverPanel != null) return;

            var panelGo = GameObject.Find("GameOverPanel");
            if (panelGo != null) gameOverPanel = panelGo.GetComponent<RectTransform>();
        }

        private bool TryPlaceAboveGameOverPanel()
        {
            var corners = new Vector3[4];
            gameOverPanel.GetWorldCorners(corners);

            Canvas panelCanvas = gameOverPanel.GetComponentInParent<Canvas>();
            Camera panelCamera = panelCanvas != null ? panelCanvas.worldCamera : null;
            Vector2 panelTopScreen = RectTransformUtility.WorldToScreenPoint(panelCamera, corners[1]);

            Vector2 panelTopLocal;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, panelTopScreen, null, out panelTopLocal))
                return false;

            float maxBubbleWidth = Mathf.Max(320f, canvasRect.rect.width - EdgeMargin * 2f);
            if (bubbleRect.sizeDelta.x > maxBubbleWidth)
                bubbleRect.sizeDelta = new Vector2(maxBubbleWidth, bubbleRect.sizeDelta.y);

            // En game over el panel ocupa el centro: el pensamiento va arriba,
            // sin tapar el resumen ni el botón de reintentar.
            float y = panelTopLocal.y + 14f;
            float maxY = canvasRect.rect.height * 0.5f - bubbleRect.sizeDelta.y - EdgeMargin;
            y = Mathf.Min(y, maxY);
            bubbleRect.anchoredPosition = new Vector2(0f, y);

            // Los puntos quedarían sobre el texto del panel; en game over sobran.
            if (thoughtGroup != null) thoughtGroup.alpha = 0f;
            return true;
        }

        private void UpdateThoughtDots(float bubbleX, float bubbleBottomY, Vector2 headLocal)
        {
            if (thoughtDots.Count == 0) return;

            float endY = headLocal.y + 10f;
            float startY = bubbleBottomY;
            for (int i = 0; i < thoughtDots.Count; i++)
            {
                float t = (i + 1f) / (thoughtDots.Count + 1f);
                Image dot = thoughtDots[i];
                dot.rectTransform.anchoredPosition = new Vector2(
                    Mathf.Lerp(bubbleX, headLocal.x, t),
                    Mathf.Lerp(startY, endY, t));
            }
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
            // El contrato exige texto o ítem; el game over no tiene ítem.
            StartCoroutine(RequestLineWhenFree("game_over", null, null, "La partida terminó"));
        }

        private IEnumerator RequestLineWhenFree(string context, string item, string kind, string text)
        {
            // Si el jugador atrapó algo justo al terminar, la petición anterior
            // puede seguir en vuelo. El comentario final no se descarta.
            while (requestInFlight) yield return null;
            yield return RequestLine(context, item, kind, text);
        }

        private IEnumerator RequestLine(string context, string item, string kind, string text)
        {
            if (requestInFlight) yield break;

            string fallback = LocalLine(context, item);

            requestInFlight = true;
            BeginTyping();
            string json = BuildPayload(text, context, item, kind);
            var request = new UnityWebRequest(GetDialogUrl(), UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Mathf.Max(1, Mathf.RoundToInt(requestTimeoutSeconds))
            };
            request.SetRequestHeader("Content-Type", "application/json");
            if (AuthStub.IsBackendToken)
            {
                request.SetRequestHeader("Authorization", "Bearer " + AuthStub.SessionToken);
            }

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
            StopTyping();
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

            // FE-09: historial corto. La línea anterior queda visible, atenuada,
            // debajo de la nueva, para que el comentario no aparezca de la nada.
            if (!string.IsNullOrEmpty(lastLine) && lastLine != line)
            {
                history.Enqueue(lastLine);
                while (history.Count > HistorySize) history.Dequeue();
            }
            lastLine = line;
            RefreshHistory();

            bodyText.text = line;

            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(ShowRoutine());
        }

        private void RefreshHistory()
        {
            if (historyText == null) return;
            historyText.text = string.Join("\n", history);
        }

        /// <summary>Indicador de "escribiendo…" mientras la IA responde.</summary>
        private void BeginTyping()
        {
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }
            typingRoutine = StartCoroutine(TypingRoutine());
        }

        private void StopTyping()
        {
            if (typingRoutine == null) return;
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        private IEnumerator TypingRoutine()
        {
            // Un backend local responde en milisegundos; esperar evita un
            // parpadeo de "escribiendo…" en cada captura.
            yield return new WaitForSecondsRealtime(0.35f);

            if (bodyText == null || bubbleGroup == null)
            {
                typingRoutine = null;
                yield break;
            }

            const string label = "escribiendo";
            int dots = 0;
            float elapsed = 0f;
            const float fadeIn = 0.18f;

            while (true)
            {
                bodyText.text = label + new string('.', dots);
                dots = (dots + 1) % 4;

                if (bubbleGroup.alpha < 1f)
                {
                    elapsed += 0.35f;
                    bubbleGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
                    if (thoughtGroup != null) thoughtGroup.alpha = bubbleGroup.alpha;
                }

                yield return new WaitForSecondsRealtime(0.35f);
            }
        }

        private IEnumerator ShowRoutine()
        {
            bubbleGroup.alpha = 0f;
            bubbleGroup.blocksRaycasts = false;
            bubbleGroup.interactable = false;
            if (thoughtGroup != null) thoughtGroup.alpha = 0f;

            float elapsed = 0f;
            const float fadeIn = 0.18f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.unscaledDeltaTime;
                bubbleGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
                if (thoughtGroup != null) thoughtGroup.alpha = bubbleGroup.alpha;
                yield return null;
            }
            bubbleGroup.alpha = 1f;
            if (thoughtGroup != null) thoughtGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(visibleSeconds);

            elapsed = 0f;
            const float fadeOut = 0.35f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                bubbleGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
                if (thoughtGroup != null) thoughtGroup.alpha = bubbleGroup.alpha;
                yield return null;
            }
            bubbleGroup.alpha = 0f;
            if (thoughtGroup != null) thoughtGroup.alpha = 0f;
            hideRoutine = null;
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject(
                "NpcDialogueCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            canvasRect = canvasGo.GetComponent<RectTransform>();

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var thoughtsGo = new GameObject(
                "ThoughtDots",
                typeof(RectTransform),
                typeof(CanvasGroup));
            thoughtsGo.transform.SetParent(canvasGo.transform, false);
            thoughtLayer = thoughtsGo.GetComponent<RectTransform>();
            thoughtLayer.anchorMin = Vector2.zero;
            thoughtLayer.anchorMax = Vector2.one;
            thoughtLayer.offsetMin = Vector2.zero;
            thoughtLayer.offsetMax = Vector2.zero;
            thoughtGroup = thoughtsGo.GetComponent<CanvasGroup>();
            thoughtGroup.alpha = 0f;
            thoughtGroup.blocksRaycasts = false;
            thoughtGroup.interactable = false;

            CreateThoughtDots();

            var bubbleGo = new GameObject(
                "Bubble",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));
            bubbleGo.transform.SetParent(canvasGo.transform, false);

            var background = bubbleGo.GetComponent<Image>();
            background.color = Theme.WithAlpha(Theme.Ink, 0.94f);
            background.raycastTarget = false;

            bubbleRect = bubbleGo.GetComponent<RectTransform>();
            bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
            bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRect.pivot = new Vector2(0.5f, 0f);
            bubbleRect.anchoredPosition = new Vector2(0f, -120f);
            bubbleRect.sizeDelta = new Vector2(BubbleWidth, BubbleHeight);

            var bubbleOutline = bubbleGo.AddComponent<Outline>();
            bubbleOutline.effectColor = Theme.WithAlpha(Theme.Gold, 0.32f);
            bubbleOutline.effectDistance = new Vector2(1.5f, -1.5f);

            bubbleGroup = bubbleGo.GetComponent<CanvasGroup>();
            bubbleGroup.alpha = 0f;
            bubbleGroup.blocksRaycasts = false;
            bubbleGroup.interactable = false;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(bubbleGo.transform, false);
            var nameText = nameGo.GetComponent<TextMeshProUGUI>();
            nameText.text = "La Palenquera";
            nameText.fontSize = 28f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Theme.Gold;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null) nameText.font = TMP_Settings.defaultFontAsset;

            var nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.offsetMin = new Vector2(28f, -44f);
            nameRect.offsetMax = new Vector2(-28f, -12f);

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(bubbleGo.transform, false);
            bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
            bodyText.fontSize = 34f;
            bodyText.color = Theme.White;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.overflowMode = TextOverflowModes.Truncate;
            bodyText.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null) bodyText.font = TMP_Settings.defaultFontAsset;

            var bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(28f, 62f);
            bodyRect.offsetMax = new Vector2(-28f, -50f);

            var shadow = bodyGo.AddComponent<Shadow>();
            shadow.effectColor = Theme.WithAlpha(Theme.Ink, 0.65f);
            shadow.effectDistance = new Vector2(2f, -2f);

            // FE-09: dos líneas previas, pequeñas y atenuadas, al pie del bocadillo.
            var historyGo = new GameObject("History", typeof(RectTransform), typeof(TextMeshProUGUI));
            historyGo.transform.SetParent(bubbleGo.transform, false);
            historyText = historyGo.GetComponent<TextMeshProUGUI>();
            historyText.fontSize = 21f;
            historyText.color = Theme.WithAlpha(Theme.Sand, 0.62f);
            historyText.alignment = TextAlignmentOptions.TopLeft;
            historyText.textWrappingMode = TextWrappingModes.Normal;
            historyText.overflowMode = TextOverflowModes.Ellipsis;
            historyText.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null) historyText.font = TMP_Settings.defaultFontAsset;

            var historyRect = historyText.rectTransform;
            historyRect.anchorMin = Vector2.zero;
            historyRect.anchorMax = new Vector2(1f, 0f);
            historyRect.pivot = new Vector2(0.5f, 0f);
            historyRect.offsetMin = new Vector2(28f, 14f);
            historyRect.offsetMax = new Vector2(-28f, 58f);
        }

        private void CreateThoughtDots()
        {
            Sprite circle = CreateCircleSprite();
            float[] sizes = { 22f, 15f, 9f };
            foreach (float size in sizes)
            {
                var dotGo = new GameObject("ThoughtDot", typeof(RectTransform), typeof(Image));
                dotGo.transform.SetParent(thoughtLayer, false);

                var dotRect = dotGo.GetComponent<RectTransform>();
                dotRect.anchorMin = new Vector2(0.5f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta = new Vector2(size, size);

                var dot = dotGo.GetComponent<Image>();
                dot.sprite = circle;
                dot.color = Theme.WithAlpha(Theme.Ink, 0.96f);
                dot.raycastTarget = false;

                var outline = dotGo.AddComponent<Outline>();
                outline.effectColor = Theme.WithAlpha(Theme.Gold, 0.45f);
                outline.effectDistance = new Vector2(1.1f, -1.1f);

                thoughtDots.Add(dot);
            }
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "NpcThoughtDot",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color(Theme.White.r, Theme.White.g, Theme.White.b, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        [Serializable]
        private sealed class DialogResponse
        {
            public string reply;
            public bool blocked;
        }
    }
}
