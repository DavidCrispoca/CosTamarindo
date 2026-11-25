using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CosTamarindo.Core;
using CosTamarindo.Net;

namespace CosTamarindo.UI
{
    /// <summary>
    /// Pantalla «Top 5»: el leaderboard global del juego.
    ///
    /// Llama a <c>GET /api/leaderboard</c> (público, sin login) y muestra las
    /// cinco mejores puntuaciones reclamadas con su nombre. Si el jugador
    /// reclamó un nombre y está en la tabla, su fila se resalta.
    ///
    /// La UI se construye en runtime igual que <see cref="BookView"/>, usando
    /// la paleta y las fuentes de <see cref="Theme"/> para no romper la estética.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LeaderboardView : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private TMP_FontAsset displayFont;
        [SerializeField] private TMP_FontAsset bodyFont;
        [Tooltip("Fondo de madera de la tarjeta (fondo.png).")]
        [SerializeField] private Sprite backgroundSprite;
        [Tooltip("Título dibujado 'Top 5' (top5.png).")]
        [SerializeField] private Sprite titleSprite;
        [Tooltip("Ícono de cerrar (iconocerrar.png).")]
        [SerializeField] private Sprite closeSprite;

        [Header("Disposición")]
        [SerializeField] private Vector2 cardSize = new Vector2(700f, 720f);
        [SerializeField] private float rowHeight = 84f;
        [SerializeField] private float rowSpacing = 12f;

        private static Color PanelColor => Theme.WithAlpha(Theme.Sand, 0.98f);
        private static Color InkColor => Theme.Ink;
        private static Color RowColor => Theme.WithAlpha(Theme.SandDark, 0.42f);
        private static Color MeColor => Theme.WithAlpha(Theme.Gold, 0.55f);

        private GameObject panel;
        private Transform rowsRoot;
        private TMP_Text statusText;
        private Coroutine loadRoutine;
        private bool pausedByMe;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            BuildInterface();
            WireButton("LeaderboardButton", Toggle); // por si existe en la escena
            EnsureHudButton();                       // y lo crea junto al botón «Libro»
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.kKey.wasPressedThisFrame) Toggle();
            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (panel == null) return;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            PauseGame();
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (loadRoutine != null) { StopCoroutine(loadRoutine); loadRoutine = null; }
            ResumeGame();
        }

        private void PauseGame()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            pausedByMe = true;
            gm.Pause();
        }

        private void ResumeGame()
        {
            if (!pausedByMe) return;
            pausedByMe = false;
            var gm = GameManager.Instance;
            if (gm != null && gm.State == GameState.Paused) gm.Resume();
        }

        private void Refresh()
        {
            if (rowsRoot == null) return;
            for (int i = rowsRoot.childCount - 1; i >= 0; i--)
                Destroy(rowsRoot.GetChild(i).gameObject);

            SetStatus("Cargando ranking…", Theme.InkSoft);
            loadRoutine = StartCoroutine(LoadLeaderboard());
        }

        private IEnumerator LoadLeaderboard()
        {
            var api = FindApiClient();
            if (api == null)
            {
                SetStatus("Sin conexión: no se pudo cargar el ranking.", Theme.InkSoft);
                yield break;
            }

            ApiResult<LeaderboardResponse> res = null;
            yield return api.GetLeaderboard(5, r => res = r);

            if (res == null || !res.Ok || res.Data == null)
            {
                bool offline = res == null || res.HttpStatus <= 0 || res.ErrorCode == "network_error";
                SetStatus(offline
                    ? "Sin conexión: no se pudo cargar el ranking."
                    : "No se pudo cargar el ranking.", offline ? Theme.InkSoft : Theme.Error);
                yield break;
            }

            var entries = res.Data.entries;
            if (entries == null || entries.Length == 0)
            {
                SetStatus("Aún no hay puntajes. ¡Sé el primero!", Theme.InkSoft);
                yield break;
            }

            SetStatus(string.Empty, Theme.InkSoft);
            BuildRows(entries);
        }

        // ── Construcción de la UI ────────────────────────────────────────

        private void BuildInterface()
        {
            panel = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);

            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var dim = panel.GetComponent<Image>();
            dim.color = Theme.Scrim;
            dim.raycastTarget = true;

            // ── Tarjeta central ──
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(panel.transform, false);

            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = cardSize;
            card.GetComponent<Image>().sprite = backgroundSprite;
            card.GetComponent<Image>().color = backgroundSprite != null ? Color.white : PanelColor;

            // ── Título ──
            if (titleSprite != null)
            {
                var titleImg = new GameObject("Title", typeof(RectTransform), typeof(Image));
                titleImg.transform.SetParent(card.transform, false);
                var titleRt = titleImg.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0.5f, 1f);
                titleRt.anchorMax = new Vector2(0.5f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.sizeDelta = new Vector2(360f, 110f);
                titleRt.anchoredPosition = new Vector2(0f, -28f);
                var img = titleImg.GetComponent<Image>();
                img.sprite = titleSprite;
                img.color = Color.white;
                img.raycastTarget = false;
                img.preserveAspect = true;
            }
            else
            {
                var title = CreateText(card.transform, "Title", "Top 5", 64f, displayFont, InkColor);
                var titleRt = title.rectTransform;
                titleRt.anchorMin = new Vector2(0.5f, 1f);
                titleRt.anchorMax = new Vector2(0.5f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.sizeDelta = new Vector2(cardSize.x - 80f, 72f);
                titleRt.anchoredPosition = new Vector2(0f, -28f);
            }

            var subtitle = CreateText(card.transform, "Subtitle", "Mejores puntajes", 32f, bodyFont, Theme.InkSoft);
            var subtitleRt = subtitle.rectTransform;
            subtitleRt.anchorMin = new Vector2(0.5f, 1f);
            subtitleRt.anchorMax = new Vector2(0.5f, 1f);
            subtitleRt.pivot = new Vector2(0.5f, 1f);
            subtitleRt.sizeDelta = new Vector2(cardSize.x - 80f, 44f);
            subtitleRt.anchoredPosition = new Vector2(0f, -100f);

            // ── Contenedor de filas ──
            var rows = new GameObject("Rows", typeof(RectTransform));
            rows.transform.SetParent(card.transform, false);
            rowsRoot = rows.transform;
            var rowsRt = rows.GetComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0.5f, 1f);
            rowsRt.anchorMax = new Vector2(0.5f, 1f);
            rowsRt.pivot = new Vector2(0.5f, 1f);
            rowsRt.sizeDelta = new Vector2(cardSize.x - 80f, 5f * (rowHeight + rowSpacing));
            rowsRt.anchoredPosition = new Vector2(0f, -156f);

            // ── Estado (cargando / vacío / error) ──
            statusText = CreateText(card.transform, "Status", string.Empty, 30f, bodyFont, Theme.InkSoft);
            var statusRt = statusText.rectTransform;
            statusRt.anchorMin = new Vector2(0.5f, 0f);
            statusRt.anchorMax = new Vector2(0.5f, 0f);
            statusRt.pivot = new Vector2(0.5f, 0f);
            statusRt.sizeDelta = new Vector2(cardSize.x - 80f, 90f);
            statusRt.anchoredPosition = new Vector2(0f, 96f);

            // ── Botón cerrar ──
            Button close;
            if (closeSprite != null)
            {
                close = CreateIconButton(card.transform, "CloseLeaderboardButton", closeSprite, new Vector2(72f, 72f));
            }
            else
            {
                close = CreateButton(card.transform, "CloseLeaderboardButton", "Cerrar", 32f, new Vector2(240f, 64f), bodyFont);
            }
            var closeRt = close.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 24f);
            close.onClick.AddListener(Close);

            panel.SetActive(false);
        }

        private void BuildRows(LeaderboardEntry[] entries)
        {
            float width = cardSize.x - 80f;
            float y = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                bool me = e.is_me;

                var row = new GameObject("Row_" + (i + 1), typeof(RectTransform), typeof(Image));
                row.transform.SetParent(rowsRoot, false);

                var rt = row.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(width, rowHeight);
                rt.anchoredPosition = new Vector2(0f, y);

                row.GetComponent<Image>().color = me ? MeColor : RowColor;
                row.GetComponent<Image>().raycastTarget = false;

                // Número de puesto
                var rank = CreateText(row.transform, "Rank", (e.rank_position > 0 ? e.rank_position : i + 1).ToString(),
                    40f, displayFont, InkColor);
                var rankRt = rank.rectTransform;
                rankRt.anchorMin = new Vector2(0f, 0.5f);
                rankRt.anchorMax = new Vector2(0f, 0.5f);
                rankRt.pivot = new Vector2(0f, 0.5f);
                rankRt.sizeDelta = new Vector2(90f, rowHeight);
                rankRt.anchoredPosition = new Vector2(18f, 0f);
                rank.alignment = TextAlignmentOptions.Left;

                // Nombre
                string name = string.IsNullOrEmpty(e.username) ? "—" : e.username;
                var nameText = CreateText(row.transform, "Name", name, 34f, bodyFont, InkColor);
                var nameRt = nameText.rectTransform;
                nameRt.anchorMin = new Vector2(0f, 0.5f);
                nameRt.anchorMax = new Vector2(1f, 0.5f);
                nameRt.pivot = new Vector2(0f, 0.5f);
                nameRt.offsetMin = new Vector2(116f, 0f);
                nameRt.offsetMax = new Vector2(-150f, 0f);
                nameText.alignment = TextAlignmentOptions.Left;
                nameText.enableAutoSizing = true;
                nameText.fontSizeMin = 24f;
                nameText.fontSizeMax = 34f;

                // Puntaje
                var score = CreateText(row.transform, "Score", e.best_score.ToString(), 40f, displayFont, InkColor);
                var scoreRt = score.rectTransform;
                scoreRt.anchorMin = new Vector2(1f, 0.5f);
                scoreRt.anchorMax = new Vector2(1f, 0.5f);
                scoreRt.pivot = new Vector2(1f, 0.5f);
                scoreRt.sizeDelta = new Vector2(140f, rowHeight);
                scoreRt.anchoredPosition = new Vector2(-18f, 0f);
                score.alignment = TextAlignmentOptions.Right;

                y -= rowHeight + rowSpacing;
            }
        }

        // ── Utilidades ───────────────────────────────────────────────────

        private void EnsureHudButton()
        {
            // Crea «Top 5» junto al botón «Libro» del HUD, sin depender de la escena.
            var book = GameObject.Find("BookButton");
            if (book == null) return;
            if (GameObject.Find("LeaderboardButton") != null) return;

            var parent = book.transform.parent;
            var bookRt = book.GetComponent<RectTransform>();
            if (parent == null || bookRt == null) return;

            var go = new GameObject("LeaderboardButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = bookRt.anchorMin;
            rt.anchorMax = bookRt.anchorMax;
            rt.pivot = bookRt.pivot;
            rt.sizeDelta = bookRt.sizeDelta;
            rt.anchoredPosition = bookRt.anchoredPosition + new Vector2(0f, -76f);

            var image = go.GetComponent<Image>();
            image.color = Theme.Wood;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Toggle);

            var label = CreateText(go.transform, "Label", "Top 5", 30f, bodyFont, Theme.Sand);
            var labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
        }

        private ApiClient FindApiClient()
        {
            if (ApiClient.Instance != null) return ApiClient.Instance;
            var existing = FindFirstObjectByType<ApiClient>();
            if (existing != null) return existing;
            var go = new GameObject("ApiClient");
            return go.AddComponent<ApiClient>();
        }

        private void WireButton(string objectName, UnityEngine.Events.UnityAction action)
        {
            var buttons = transform.root.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name != objectName) continue;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(action);
            }
        }

        private void SetStatus(string msg, Color color)
        {
            if (statusText == null) return;
            statusText.text = msg ?? string.Empty;
            statusText.color = color;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, string name, string content, float size,
            TMP_FontAsset font, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            var chosen = font != null ? font : TMP_Settings.defaultFontAsset;
            if (chosen != null) text.font = chosen;

            return text;
        }

        private static Button CreateIconButton(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            return button;
        }

        private static Button CreateButton(
            Transform parent, string name, string label, float fontSize,
            Vector2 size, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = Theme.Wood;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(go.transform, "Label", label, fontSize, font, Theme.Sand);
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return button;
        }
    }
}
