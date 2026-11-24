using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using CosTamarindo.Core;
using CosTamarindo.Gameplay;
using CosTamarindo.UI;

namespace CosTamarindo.EditorTools
{
    /// <summary>
    /// Construye la escena jugable completa (cámara, jugador, spawner y HUD) por código.
    /// Se ejecuta una sola vez desde el menú <c>CosTamarindo</c>; después la escena es un
    /// archivo <c>.unity</c> normal que se puede editar a mano.
    /// </summary>
    public static class CosTamarindoSceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string ScenePath = ScenesFolder + "/Game.unity";
        private const string CuratedItemsFolder = "Assets/Art/Items/curated";

        // Paleta y estilos centralizados en Theme (FE-04). No hay colores sueltos.
        private static Color Sand => Theme.Sand;
        private static Color Wood => Theme.Wood;
        private static Color Ink => Theme.Ink;
        private static Color Sea => Theme.Sea;

        [MenuItem("CosTamarindo/Construir escena de juego", priority = 1)]
        public static void BuildGameScene()
        {
            EnsureFolder("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildBackground();
            BuildCamera();
            var manager = BuildGameManager();
            var player = BuildPlayer();
            BuildSpawner(player);
            BuildInterface(manager, player);
            BuildNpcDialogue();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CosTamarindo] Escena generada en {ScenePath}");
        }

        // ── Escena ───────────────────────────────────────────────────────

        private static void BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Color de respaldo: solo se ve si el fondo no cubre (aspecto extremo).
            cam.backgroundColor = Sea;

            go.AddComponent<AudioListener>();
        }

        /// <summary>
        /// Fondo de playa exportado de Figma. Va en sortingOrder -100 y
        /// <c>BackgroundFitter</c> lo escala para cubrir la cámara sin deformarlo.
        /// </summary>
        private static void BuildBackground()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FigmaArtImporter.BackgroundPath);

            var go = new GameObject("Background");
            go.transform.position = new Vector3(0f, 0f, 5f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;

            go.AddComponent<BackgroundFitter>();

            if (sprite == null)
                Debug.LogWarning($"[CosTamarindo] falta el fondo en {FigmaArtImporter.BackgroundPath}");
        }

        private static GameManager BuildGameManager()
        {
            var go = new GameObject("GameManager");
            return go.AddComponent<GameManager>();
        }

        /// <summary>
        /// Palenquera con la canasta en la cabeza. El controlador mueve el objeto
        /// raíz y <c>CharacterAnimator</c> anima el hijo <c>Visual</c> (paso,
        /// inclinación y respiración) para que ambas cosas no se pisen.
        /// </summary>
        private static PlayerController BuildPlayer()
        {
            const float targetHeight = 3f;
            const float feetY = -4.6f;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FigmaArtImporter.CharacterPath);

            var go = new GameObject("Player");
            go.transform.position = new Vector3(0f, feetY, 0f);

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(go.transform, false);

            var renderer = visualGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 20;

            // Sin sprite (arte sin importar) se deja escala 1 y PlayerController
            // cae al relleno procedural en Awake.
            if (sprite != null)
            {
                float height = sprite.bounds.size.y;
                if (height > 0.001f)
                    visualGo.transform.localScale = Vector3.one * (targetHeight / height);
            }
            else
            {
                renderer.color = Theme.Coral;
                Debug.LogWarning($"[CosTamarindo] falta el personaje en {FigmaArtImporter.CharacterPath}");
            }

            var controller = go.AddComponent<PlayerController>();
            SetObject(controller, "visual", renderer);

            var animator = visualGo.AddComponent<CharacterAnimator>();
            SetObject(animator, "player", controller);
            SetObject(animator, "spriteRenderer", renderer);

            return controller;
        }

        private static void BuildSpawner(PlayerController player)
        {
            var go = new GameObject("Spawner");
            var spawner = go.AddComponent<ItemSpawner>();

            SetObject(spawner, "player", player);
            SetObjectArray(spawner, "itemSprites", LoadItemSprites());
        }

        private static void BuildNpcDialogue()
        {
            var go = new GameObject("NpcDialogue");
            go.AddComponent<NpcDialogueController>();
        }

        // ── Interfaz ─────────────────────────────────────────────────────

        private static void BuildInterface(GameManager manager, PlayerController player)
        {
            var displayFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/LilitaOne SDF.asset");
            var bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/PatrickHand SDF.asset");

            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            var root = canvasGo.transform;

            // ── HUD ──
            var scoreText = CreateText(root, "ScoreText", "0", 72f, TextAlignmentOptions.TopLeft, displayFont);
            Anchor(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                   new Vector2(48f, -40f), new Vector2(420f, 90f));

            var bestText = CreateText(root, "BestText", "Récord 0", 34f, TextAlignmentOptions.TopLeft, bodyFont);
            Anchor(bestText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                   new Vector2(50f, -118f), new Vector2(420f, 50f));

            var timerText = CreateText(root, "TimerText", "02:00", 64f, TextAlignmentOptions.Top, displayFont);
            Anchor(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                   new Vector2(0f, -40f), new Vector2(320f, 88f));

            var livesText = CreateText(root, "LivesText", "\u2665\u2665\u2665", 64f, TextAlignmentOptions.TopRight);
            Anchor(livesText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                   new Vector2(-150f, -40f), new Vector2(320f, 88f));
            livesText.color = Theme.Error;

            var pauseButton = CreateButton(root, "PauseButton", "II", 46f, new Vector2(96f, 88f));
            Anchor(pauseButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                   new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(92f, 92f));

            // ── Pausa ──
            var pausePanel = CreateOverlayPanel(root, "PausePanel", out var pauseContent);
            CreateText(pauseContent, "Title", "Pausa", 84f, TextAlignmentOptions.Center, displayFont);
            CreateButton(pauseContent, "ResumeButton", "Continuar", 46f, new Vector2(420f, 96f), new Vector2(0f, 20f), bodyFont);
            CreateButton(pauseContent, "RestartButton", "Reiniciar", 46f, new Vector2(420f, 96f), new Vector2(0f, -100f), bodyFont);

            // ── Fin de partida ──
            // La tarjeta mide 760×700 (-350..350). Cada elemento ocupa su propia
            // franja vertical con hueco, para que ningún texto se monte sobre otro
            // cuando el desglose o el estado de la red cambian de longitud.
            var gameOverPanel = CreateOverlayPanel(root, "GameOverPanel", out var overContent);

            var overTitle = CreateText(overContent, "Title", "¡Se acabó la pesca!", 68f, TextAlignmentOptions.Center, displayFont);
            PlaceText(overTitle, new Vector2(0f, 292f), new Vector2(640f, 72f));

            var finalScore = CreateText(overContent, "FinalScore", "0", 104f, TextAlignmentOptions.Center, displayFont);
            PlaceText(finalScore, new Vector2(0f, 198f), new Vector2(640f, 104f));

            var finalBest = CreateText(overContent, "FinalBest", "Récord 0", 36f, TextAlignmentOptions.Center, bodyFont);
            PlaceText(finalBest, new Vector2(0f, 118f), new Vector2(640f, 52f));

            var breakdown = CreateText(overContent, "Breakdown", "", 24f, TextAlignmentOptions.Center, bodyFont);
            PlaceText(breakdown, new Vector2(0f, 30f), new Vector2(640f, 120f));

            var submitStatus = CreateText(overContent, "SubmitStatus", "", 26f, TextAlignmentOptions.Center, bodyFont);
            PlaceText(submitStatus, new Vector2(0f, -70f), new Vector2(640f, 64f));

            // Reclamo top 5 (se activa en runtime si in_top5)
            var claimPanel = new GameObject("ClaimPanel", typeof(RectTransform));
            claimPanel.transform.SetParent(overContent, false);
            var claimRt = claimPanel.GetComponent<RectTransform>();
            claimRt.anchorMin = new Vector2(0.5f, 0.5f);
            claimRt.anchorMax = new Vector2(0.5f, 0.5f);
            claimRt.pivot = new Vector2(0.5f, 0.5f);
            claimRt.anchoredPosition = new Vector2(0f, -190f);
            claimRt.sizeDelta = new Vector2(680f, 170f);

            var claimUser = CreateInputField(claimPanel.transform, "ClaimUsernameInput", "Nombre público (3-16)", 26f,
                new Vector2(300f, 56f), new Vector2(-160f, 44f), bodyFont, false, 16);
            var claimPin = CreateInputField(claimPanel.transform, "ClaimPinInput", "PIN (4-6 dígitos)", 26f,
                new Vector2(300f, 56f), new Vector2(160f, 44f), bodyFont, true, 6);
            var claimBtn = CreateButton(claimPanel.transform, "ClaimButton", "Reclamar", 34f, new Vector2(280f, 52f), new Vector2(0f, -12f), bodyFont);
            var claimStatus = CreateText(claimPanel.transform, "ClaimStatus", "", 22f, TextAlignmentOptions.Center, bodyFont);
            PlaceText(claimStatus, new Vector2(0f, -62f), new Vector2(640f, 36f));
            claimPanel.SetActive(false);

            CreateButton(overContent, "RetryButton", "Reintentar", 42f, new Vector2(360f, 60f), new Vector2(0f, -315f), bodyFont);

            // ── Controladores de UI ──
            var hud = canvasGo.AddComponent<HudController>();
            SetObject(hud, "scoreText", scoreText);
            SetObject(hud, "timerText", timerText);
            SetObject(hud, "livesText", livesText);
            SetObject(hud, "bestText", bestText);

            var overlay = canvasGo.AddComponent<OverlayController>();
            SetObject(overlay, "pausePanel", pausePanel);
            SetObject(overlay, "gameOverPanel", gameOverPanel);
            SetObject(overlay, "finalScoreText", finalScore);
            SetObject(overlay, "finalBestText", finalBest);
            SetObject(overlay, "breakdownText", breakdown);
            SetObject(overlay, "submitStatusText", submitStatus);
            SetObject(overlay, "claimPanel", claimPanel);
            SetObject(overlay, "claimUsernameInput", claimUser);
            SetObject(overlay, "claimPinInput", claimPin);
            SetObject(overlay, "claimButton", claimBtn);
            SetObject(overlay, "claimStatusText", claimStatus);

            // ── Libro de la colección (F2) ──
            var bookButton = CreateButton(root, "BookButton", "Libro", 32f, new Vector2(160f, 64f), default, bodyFont);
            Anchor(bookButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                   new Vector2(0f, 1f), new Vector2(50f, -182f), new Vector2(160f, 64f));

            var book = canvasGo.AddComponent<BookView>();
            SetObjectArray(book, "itemSprites", LoadItemSprites());
            SetObject(book, "displayFont", displayFont);
            SetObject(book, "bodyFont", bodyFont);
            bookButton.onClick.AddListener(book.Toggle);

            // ── Feedback de captura (F2) ──
            var floatingLayer = new GameObject("FloatingLayer", typeof(RectTransform));
            floatingLayer.transform.SetParent(root, false);

            var floatingRt = floatingLayer.GetComponent<RectTransform>();
            floatingRt.anchorMin = Vector2.zero;
            floatingRt.anchorMax = Vector2.one;
            floatingRt.offsetMin = Vector2.zero;
            floatingRt.offsetMax = Vector2.zero;
            floatingRt.pivot = new Vector2(0.5f, 0.5f);
            // Encima del HUD pero debajo de los paneles modales.
            floatingLayer.transform.SetSiblingIndex(1);

            var audio = canvasGo.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;

            var feedback = canvasGo.AddComponent<CaptureFeedback>();
            SetObject(feedback, "floatingRoot", floatingRt);
            SetObject(feedback, "scoreText", scoreText);
            SetObject(feedback, "font", displayFont);
            SetObject(feedback, "player", player != null ? player.transform : null);
            SetObject(feedback, "audioSource", audio);
        }

        private static GameObject CreateOverlayPanel(Transform parent, string name, out Transform content)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = Theme.Scrim;
            image.raycastTarget = true;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(go.transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(760f, 700f);
            card.GetComponent<Image>().color = Sand;

            content = card.transform;
            go.SetActive(false);
            return go;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions alignment, TMP_FontAsset font = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Ink;
            text.raycastTarget = false;

            var chosen = font != null ? font : TMP_Settings.defaultFontAsset;
            if (chosen != null) text.font = chosen;

            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(640f, 90f);
            return text;
        }

        /// <summary>Ubica un texto y evita que se desborde de su franja.</summary>
        private static void PlaceText(TMP_Text text, Vector2 position, Vector2 size)
        {
            var rt = text.rectTransform;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static Button CreateButton(Transform parent, string name, string label, float fontSize, Vector2 size, Vector2 position = default, TMP_FontAsset font = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            var image = go.GetComponent<Image>();
            image.color = Wood;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(go.transform, "Label", label, fontSize, TextAlignmentOptions.Center, font);
            text.color = Sand;
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return button;
        }

        private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, float fontSize,
            Vector2 size, Vector2 position, TMP_FontAsset font = null, bool password = false, int charLimit = 0)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            var bg = go.GetComponent<Image>();
            bg.color = Theme.Field;

            var viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            var vprt = viewport.GetComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.offsetMin = new Vector2(12f, 8f);
            vprt.offsetMax = new Vector2(-12f, -8f);

            var placeholderText = CreateText(viewport.transform, "Placeholder", placeholder, fontSize, TextAlignmentOptions.Left, font);
            placeholderText.color = Theme.Placeholder;
            Stretch(placeholderText.rectTransform);

            var inputText = CreateText(viewport.transform, "Text", "", fontSize, TextAlignmentOptions.Left, font);
            inputText.color = Ink;
            Stretch(inputText.rectTransform);

            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = vprt;
            input.textComponent = inputText;
            input.placeholder = placeholderText;
            input.fontAsset = font != null ? font : TMP_Settings.defaultFontAsset;
            input.pointSize = fontSize;
            if (charLimit > 0) input.characterLimit = charLimit;
            if (password)
            {
                input.contentType = TMP_InputField.ContentType.Pin;
                input.asteriskChar = '•';
            }

            return input;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        // ── Utilidades ───────────────────────────────────────────────────

        private static Sprite[] LoadItemSprites()
        {
            // FE-02/FE-03: solo se cargan los 12 sprites curados, en el orden del
            // catálogo. Un PNG sin definición (o una definición sin PNG) se avisa
            // aquí y no llega al build como referencia huérfana.
            var list = new List<Sprite>();

            foreach (var def in ItemCatalog.All)
            {
                string path = $"{CuratedItemsFolder}/{def.Id}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogError($"[CosTamarindo] falta el sprite curado {path}");
                    continue;
                }
                list.Add(sprite);
            }

            Debug.Log($"[CosTamarindo] {list.Count}/{ItemCatalog.All.Count} sprites de ítems curados cargados.");
            return list.ToArray();
        }

        private static void RegisterSceneInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool present = scenes.Exists(s => s.path == ScenePath);
            if (!present) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetObject(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[CosTamarindo] Campo no encontrado: {field}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[CosTamarindo] Campo no encontrado: {field}"); return; }

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
