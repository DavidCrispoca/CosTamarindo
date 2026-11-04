using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using CosTamarindo.UI;

namespace CosTamarindo.EditorTools
{
    /// <summary>
    /// Construye la escena de acceso (usuario + PIN).
    ///
    /// Es una escena real y editable, igual que <c>Game</c>: el generador solo
    /// sirve para rearmarla desde cero de forma reproducible. La lógica vive en
    /// <see cref="LoginController"/> y <c>AuthStub</c>.
    /// </summary>
    public static class CosTamarindoLoginSceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string ScenePath = ScenesFolder + "/Login.unity";

        private static readonly Color Sand = new Color(0.96f, 0.87f, 0.71f);
        private static readonly Color Wood = new Color(0.45f, 0.29f, 0.18f);
        private static readonly Color Ink = new Color(0.24f, 0.16f, 0.10f);
        private static readonly Color Sea = new Color(0.42f, 0.78f, 0.90f);

        [MenuItem("CosTamarindo/Construir escena de inicio de sesión", priority = 2)]
        public static void BuildLoginScene()
        {
            EnsureFolder(ScenesFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();

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

            // ── Fondo ──
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root, false);
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = Sea;

            // ── Tarjeta ──
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(root, false);

            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(820f, 760f);
            card.GetComponent<Image>().color = Sand;

            var cardContent = card.transform;

            var title = CreateText(cardContent, "Title", "CosTamarindo", 96f, TextAlignmentOptions.Center, displayFont);
            title.rectTransform.anchoredPosition = new Vector2(0f, 268f);
            title.rectTransform.sizeDelta = new Vector2(700f, 110f);

            var subtitle = CreateText(cardContent, "Subtitle", "Entrá y ayudá a la Palenquera", 40f, TextAlignmentOptions.Center, bodyFont);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, 196f);
            subtitle.rectTransform.sizeDelta = new Vector2(700f, 50f);

            // ── Usuario ──
            var userLabel = CreateText(cardContent, "UserLabel", "Usuario", 34f, TextAlignmentOptions.Left, bodyFont);
            userLabel.rectTransform.anchoredPosition = new Vector2(-210f, 120f);
            userLabel.rectTransform.sizeDelta = new Vector2(360f, 44f);

            var username = CreateInput(cardContent, "UsernameInput", "tu_usuario", 40f,
                new Vector2(600f, 84f), new Vector2(0f, 62f), bodyFont);

            // ── PIN ──
            var pinLabel = CreateText(cardContent, "PinLabel", "PIN (4 dígitos)", 34f, TextAlignmentOptions.Left, bodyFont);
            pinLabel.rectTransform.anchoredPosition = new Vector2(-210f, -10f);
            pinLabel.rectTransform.sizeDelta = new Vector2(360f, 44f);

            var pin = CreateInput(cardContent, "PinInput", "••••", 44f,
                new Vector2(600f, 84f), new Vector2(0f, -68f), bodyFont);

            // ── Estado ──
            var status = CreateText(cardContent, "StatusText", "", 32f, TextAlignmentOptions.Center, bodyFont);
            status.rectTransform.anchoredPosition = new Vector2(0f, -152f);
            status.rectTransform.sizeDelta = new Vector2(700f, 72f);
            status.color = new Color(0.35f, 0.28f, 0.20f);

            // ── Botón ──
            var enter = CreateButton(cardContent, "EnterButton", "Entrar", 44f, new Vector2(420f, 96f), new Vector2(0f, -248f), displayFont);

            // ── Nota de alcance ──
            var hint = CreateText(cardContent, "HintText",
                "Modo desarrollo: la cuenta se crea en el primer acceso con ese usuario y PIN.",
                26f, TextAlignmentOptions.Center, bodyFont);
            hint.rectTransform.anchoredPosition = new Vector2(0f, -336f);
            hint.rectTransform.sizeDelta = new Vector2(700f, 60f);
            hint.color = new Color(Ink.r, Ink.g, Ink.b, 0.65f);

            // ── Controlador ──
            var controller = canvasGo.AddComponent<LoginController>();
            SetObject(controller, "usernameInput", username);
            SetObject(controller, "pinInput", pin);
            SetObject(controller, "statusText", status);
            SetObject(controller, "enterButton", enter);
            SetProperty(controller, "gameSceneName", "Game");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CosTamarindo] Escena de acceso generada en {ScenePath}");
        }

        // ── Construcción ─────────────────────────────────────────────────

        private static void BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Sea;
            go.AddComponent<AudioListener>();
        }

        /// <summary>
        /// Campo de texto TMP con la estructura estándar: raíz con Image,
        /// «Text Area» con RectMask2D, y dentro el texto editable y el placeholder.
        /// Sin el viewport y el textComponent el campo no acepta entrada.
        /// </summary>
        private static TMP_InputField CreateInput(
            Transform parent, string name, string placeholderText,
            float fontSize, Vector2 size, Vector2 position, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(1f, 0.98f, 0.92f);

            var input = go.AddComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.lineType = TMP_InputField.LineType.SingleLine;

            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(go.transform, false);

            var areaRt = area.GetComponent<RectTransform>();
            areaRt.anchorMin = Vector2.zero;
            areaRt.anchorMax = Vector2.one;
            areaRt.offsetMin = new Vector2(24f, 8f);
            areaRt.offsetMax = new Vector2(-24f, -8f);

            var text = CreateText(area.transform, "Text", "", fontSize, TextAlignmentOptions.Left, font);
            text.color = Ink;
            Stretch(text.rectTransform);

            var placeholder = CreateText(area.transform, "Placeholder", placeholderText, fontSize, TextAlignmentOptions.Left, font);
            placeholder.color = new Color(Ink.r, Ink.g, Ink.b, 0.40f);
            Stretch(placeholder.rectTransform);

            input.textViewport = areaRt;
            input.textComponent = text;
            input.placeholder = placeholder;

            return input;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, string name, string content, float size,
            TextAlignmentOptions alignment, TMP_FontAsset font)
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

            return text;
        }

        private static Button CreateButton(
            Transform parent, string name, string label, float fontSize,
            Vector2 size, Vector2 position, TMP_FontAsset font)
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
            Stretch(text.rectTransform);

            return button;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ── Utilidades ───────────────────────────────────────────────────

        private static void RegisterSceneInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool present = scenes.Exists(s => s.path == ScenePath);
            if (present) return;

            // Se añade después de Game: el juego sigue arrancando directo en Game
            // hasta que F4 haga obligatorio el acceso.
            int insertAt = scenes.FindIndex(s => s.path == ScenesFolder + "/Game.unity");
            scenes.Insert(insertAt >= 0 ? insertAt + 1 : 0, new EditorBuildSettingsScene(ScenePath, true));
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

        private static void SetProperty(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[CosTamarindo] Campo no encontrado: {field}"); return; }
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
