using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using CosTamarindo.Core;
using CosTamarindo.UI;

namespace CosTamarindo.EditorTools
{
    /// <summary>
    /// Construye la escena de entrada.
    ///
    /// Flujo actual: se juega de forma anónima. Si el puntaje entra al top 5,
    /// el nombre + PIN se reclaman al final de la partida (no aquí).
    /// </summary>
    public static class CosTamarindoLoginSceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string ScenePath = ScenesFolder + "/Login.unity";

        // Paleta centralizada en Theme (FE-04).
        private static Color Sand => Theme.Sand;
        private static Color Wood => Theme.Wood;
        private static Color Ink => Theme.Ink;
        private static Color Sea => Theme.Sea;

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

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root, false);
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = Sea;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(root, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(820f, 560f);
            card.GetComponent<Image>().color = Sand;

            var cardContent = card.transform;

            var title = CreateText(cardContent, "Title", "CosTamarindo", 96f, TextAlignmentOptions.Center, displayFont);
            title.rectTransform.anchoredPosition = new Vector2(0f, 170f);
            title.rectTransform.sizeDelta = new Vector2(700f, 110f);

            var subtitle = CreateText(cardContent, "Subtitle", "Entra y ayuda a la Palenquera", 40f, TextAlignmentOptions.Center, bodyFont);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, 100f);
            subtitle.rectTransform.sizeDelta = new Vector2(700f, 50f);

            var status = CreateText(cardContent, "StatusText",
                "Juegas de forma anónima. Si entras al top 5, puedes reclamar tu nombre.",
                34f, TextAlignmentOptions.Center, bodyFont);
            status.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            status.rectTransform.sizeDelta = new Vector2(700f, 120f);
            status.color = Theme.InkSoft;

            var enter = CreateButton(cardContent, "EnterButton", "Jugar", 44f, new Vector2(420f, 96f), new Vector2(0f, -120f), displayFont);

            var hint = CreateText(cardContent, "HintText",
                "Sin registro previo: el nombre + PIN solo se piden para reclamar un puesto en el top 5.",
                26f, TextAlignmentOptions.Center, bodyFont);
            hint.rectTransform.anchoredPosition = new Vector2(0f, -210f);
            hint.rectTransform.sizeDelta = new Vector2(700f, 70f);
            hint.color = Theme.WithAlpha(Theme.Ink, 0.65f);

            var controller = canvasGo.AddComponent<LoginController>();
            SetObject(controller, "statusText", status);
            SetObject(controller, "enterButton", enter);
            // usernameInput/pinInput quedan sin asignar: el flujo ya no los usa.
            SetProperty(controller, "gameSceneName", "Game");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CosTamarindo] Escena de acceso generada en {ScenePath}");
        }

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

        private static void RegisterSceneInBuildSettings()
        {
            // FE-10: el login es la primera escena del build. Sin sesión no se juega.
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
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