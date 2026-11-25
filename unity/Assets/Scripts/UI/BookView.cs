using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CosTamarindo.Core;
using CosTamarindo.Gameplay;

namespace CosTamarindo.UI
{
    /// <summary>
    /// Pantalla «Libro»: la colección de ítems descubiertos.
    ///
    /// El modelo de datos ya existía (<see cref="CollectionBook"/> persiste en
    /// PlayerPrefs y <see cref="GameManager"/> desbloquea en cada captura); esto
    /// aporta solo la capa visual que faltaba.
    ///
    /// La grilla se construye en runtime a propósito: la escena se genera por
    /// código y así el Libro no depende de que alguien cablee 7 celdas a mano.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BookView : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Sprites de ítems; el nombre debe coincidir con el Id del catálogo.")]
        [SerializeField] private Sprite[] itemSprites;
        [SerializeField] private TMP_FontAsset displayFont;
        [SerializeField] private TMP_FontAsset bodyFont;
        [Tooltip("Fondo de madera de la tarjeta (fondo.png).")]
        [SerializeField] private Sprite backgroundSprite;
        [Tooltip("Ícono de cerrar (iconocerrar.png).")]
        [SerializeField] private Sprite closeSprite;

        [Header("Disposición")]
        [SerializeField] private int columns = 4;
        [SerializeField] private Vector2 cellSize = new Vector2(190f, 190f);
        [SerializeField] private float spacing = 22f;

        private static Color PanelColor => Theme.WithAlpha(Theme.Sand, 0.98f);
        private static Color CardColor => Theme.SandDark;
        private static Color InkColor => Theme.Ink;

        private GameObject panel;
        private RectTransform gridRoot;
        private TMP_Text counterText;
        private bool pausedByMe;

        private readonly Dictionary<string, Sprite> spritesById = new Dictionary<string, Sprite>();

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            spritesById.Clear();
            if (itemSprites != null)
            {
                foreach (var s in itemSprites)
                    if (s != null) spritesById[s.name] = s;
            }

            BuildInterface();

            // El botón «Libro» del HUD no estaba cableado en la escena: se conecta aquí.
            WireButton("BookButton", Toggle);
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame) Toggle();

            if (IsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (panel == null) return;

            // Se repuebla al abrir: los desbloqueos ocurren durante la partida.
            RefreshCells();
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            PauseGame();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
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

        // ── Construcción de la UI ────────────────────────────────────────

        private void BuildInterface()
        {
            panel = new GameObject("BookPanel", typeof(RectTransform), typeof(Image));
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

            int rows = Mathf.CeilToInt((float)CollectibleCount / columns);
            float gridWidth = columns * cellSize.x + (columns - 1) * spacing;
            float gridHeight = rows * cellSize.y + (rows - 1) * spacing;
            // 330 = título + contador + botón de cierre + márgenes. Con 240 la
            // última fila quedaba debajo del botón «Cerrar».
            cardRt.sizeDelta = new Vector2(gridWidth + 120f, gridHeight + 330f);

            card.GetComponent<Image>().sprite = backgroundSprite;
            card.GetComponent<Image>().color = backgroundSprite != null ? Color.white : PanelColor;

            // ── Título ──
            var title = CreateText(card.transform, "Title", "Mi libro del mar", 64f, displayFont, InkColor);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(gridWidth, 80f);
            titleRt.anchoredPosition = new Vector2(0f, -34f);

            // ── Contador ──
            counterText = CreateText(card.transform, "Counter", "0 / 0", 34f, bodyFont, InkColor);
            var counterRt = counterText.rectTransform;
            counterRt.anchorMin = new Vector2(0.5f, 1f);
            counterRt.anchorMax = new Vector2(0.5f, 1f);
            counterRt.pivot = new Vector2(0.5f, 1f);
            counterRt.sizeDelta = new Vector2(gridWidth, 44f);
            counterRt.anchoredPosition = new Vector2(0f, -112f);

            // ── Grilla ──
            var grid = new GameObject("Grid", typeof(RectTransform));
            grid.transform.SetParent(card.transform, false);
            gridRoot = grid.GetComponent<RectTransform>();
            gridRoot.anchorMin = new Vector2(0.5f, 0.5f);
            gridRoot.anchorMax = new Vector2(0.5f, 0.5f);
            gridRoot.pivot = new Vector2(0.5f, 0.5f);
            gridRoot.sizeDelta = new Vector2(gridWidth, gridHeight);
            gridRoot.anchoredPosition = new Vector2(0f, -30f);

            // ── Botón cerrar ──
            Button close;
            if (closeSprite != null)
            {
                close = CreateIconButton(card.transform, "CloseBookButton", closeSprite, new Vector2(72f, 72f));
            }
            else
            {
                close = CreateButton(card.transform, "CloseBookButton", "Cerrar", 34f, new Vector2(240f, 68f), bodyFont);
            }
            var closeRt = close.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 30f);
            close.onClick.AddListener(Close);

            panel.SetActive(false);
        }

        private int CollectibleCount
        {
            get
            {
                int n = 0;
                foreach (var _ in ItemCatalog.Collectibles) n++;
                return Mathf.Max(1, n);
            }
        }

        private void RefreshCells()
        {
            if (gridRoot == null) return;

            // Limpia celdas previas (se reconstruyen para reflejar desbloqueos).
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
                Destroy(gridRoot.GetChild(i).gameObject);

            int index = 0;
            int unlocked = 0;
            int total = 0;

            float gridWidth = gridRoot.sizeDelta.x;
            float startX = -gridWidth * 0.5f + cellSize.x * 0.5f;

            foreach (var def in ItemCatalog.Collectibles)
            {
                total++;
                bool isUnlocked = CollectionBook.IsUnlocked(def.Id);
                if (isUnlocked) unlocked++;

                int col = index % columns;
                int row = index / columns;

                var cell = BuildCell(def, isUnlocked);
                var rt = cell.GetComponent<RectTransform>();
                rt.SetParent(gridRoot, false);

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = cellSize;
                rt.anchoredPosition = new Vector2(
                    startX + col * (cellSize.x + spacing),
                    gridRoot.sizeDelta.y * 0.5f - cellSize.y * 0.5f - row * (cellSize.y + spacing));

                index++;
            }

            if (counterText != null)
                counterText.text = unlocked + " / " + total + " descubiertos";
        }

        private GameObject BuildCell(ItemDef def, bool isUnlocked)
        {
            var cell = new GameObject("Cell_" + def.Id, typeof(RectTransform), typeof(Image));
            var bg = cell.GetComponent<Image>();
            bg.color = isUnlocked ? CardColor : Theme.WithAlpha(Theme.SandDark, 0.72f);

            // ── Retrato ──
            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(cell.transform, false);

            var portraitRt = portrait.GetComponent<RectTransform>();
            portraitRt.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRt.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRt.pivot = new Vector2(0.5f, 0.5f);
            portraitRt.sizeDelta = new Vector2(cellSize.x * 0.70f, cellSize.y * 0.58f);
            portraitRt.anchoredPosition = new Vector2(0f, cellSize.y * 0.19f);

            var image = portrait.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            if (spritesById.TryGetValue(def.Id, out var sprite) && sprite != null)
            {
                image.sprite = sprite;

                // Bloqueado: silueta oscura del propio sprite, no un cuadro gris.
                image.color = isUnlocked ? Theme.White : Theme.WithAlpha(Theme.Ink, 0.55f);
            }
            else
            {
                // Ítem sin arte todavía (F1 bloqueada por el token de Figma):
                // se usa el mismo relleno procedural del gameplay, no un hueco.
                var fallback = PlaceholderSprites.For(def.Kind);
                if (fallback != null)
                {
                    image.sprite = fallback;
                    image.color = isUnlocked ? Theme.White : Theme.WithAlpha(Theme.Ink, 0.55f);
                }
                else
                {
                    image.color = isUnlocked ? Theme.White : Theme.WithAlpha(Theme.WoodDark, 0.80f);
                }
            }

            // ── Nombre o incógnita ──
            string label = isUnlocked ? def.DisplayName : "???";
            var nameText = CreateText(
                cell.transform, "Name", label, 26f,
                isUnlocked ? bodyFont : displayFont,
                isUnlocked ? InkColor : Theme.InkSoft);

            var nameRt = nameText.rectTransform;
            nameRt.anchorMin = new Vector2(0.5f, 0f);
            nameRt.anchorMax = new Vector2(0.5f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            // Alto suficiente para dos líneas («Limonada de coco» no cabía en una).
            nameRt.sizeDelta = new Vector2(cellSize.x - 12f, 64f);
            nameRt.anchoredPosition = new Vector2(0f, 6f);

            if (isUnlocked && def.Kind == ItemKind.Danger)
            {
                var danger = CreateText(cell.transform, "Danger", "\u26A0", 26f, displayFont, Theme.Error);
                var dangerRt = danger.rectTransform;
                dangerRt.anchorMin = new Vector2(1f, 1f);
                dangerRt.anchorMax = new Vector2(1f, 1f);
                dangerRt.pivot = new Vector2(1f, 1f);
                dangerRt.sizeDelta = new Vector2(40f, 40f);
                dangerRt.anchoredPosition = new Vector2(-8f, -8f);
            }

            return cell;
        }

        // ── Utilidades de UI ─────────────────────────────────────────────

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
