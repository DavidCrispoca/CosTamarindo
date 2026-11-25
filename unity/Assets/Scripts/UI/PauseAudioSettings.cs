using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CosTamarindo.Audio;
using CosTamarindo.Core;

namespace CosTamarindo.UI
{
    /// <summary>
    /// Controles de sonido dentro del menú de pausa: un deslizador y un botón
    /// de silencio para la música y otro para los efectos.
    ///
    /// Lo instala <see cref="OverlayController"/> al despertar, con la tarjeta
    /// del panel de pausa que ya viene serializada. Así no hay que tocar la
    /// escena a mano y funciona igual en el editor y en el build WebGL.
    ///
    /// No usa <c>RuntimeInitializeOnLoadMethod</c>: ese callback solo corre para
    /// la primera escena y el juego entra primero por Login.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseAudioSettings : MonoBehaviour
    {
        private const string RootName = "PauseAudioSettings";

        private Slider musicSlider;
        private Slider sfxSlider;
        private TMP_Text musicMuteLabel;
        private TMP_Text sfxMuteLabel;

        /// <summary>Crea los controles dentro de la tarjeta del panel de pausa.</summary>
        public static void Install(Transform pausePanel)
        {
            if (pausePanel == null) return;

            Transform card = pausePanel.Find("Card");
            if (card == null || card.Find(RootName) != null) return;

            var root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(card, false);
            root.AddComponent<PauseAudioSettings>().Build(card);
        }

        private void OnEnable()
        {
            AudioSettingsService.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            AudioSettingsService.Changed -= Refresh;
        }

        private void Start()
        {
            Refresh();
        }

        private void Build(Transform card)
        {
            var rt = GetComponent<RectTransform>();
            Place(rt, new Vector2(0f, -255f), new Vector2(620f, 180f));

            var font = card.GetComponentInChildren<TMP_Text>(true) != null
                ? card.GetComponentInChildren<TMP_Text>(true).font
                : TMP_Settings.defaultFontAsset;

            CreateRow(rt, "Music", "Música", 38f, font, out musicSlider, out var musicButton, out musicMuteLabel);
            CreateRow(rt, "Effects", "Efectos", -22f, font, out sfxSlider, out var sfxButton, out sfxMuteLabel);

            var hint = CreateText(rt, "Hint", "Ajusta o silencia cada sonido. Se guarda para la próxima partida.",
                18f, font, Theme.InkSoft);
            Place(hint.rectTransform, new Vector2(0f, -72f), new Vector2(600f, 28f));

            musicSlider.onValueChanged.AddListener(AudioSettingsService.SetMusicVolume);
            sfxSlider.onValueChanged.AddListener(AudioSettingsService.SetSfxVolume);
            musicButton.onClick.AddListener(AudioSettingsService.ToggleMusicMute);
            sfxButton.onClick.AddListener(AudioSettingsService.ToggleSfxMute);

            Refresh();
        }

        private void Refresh()
        {
            if (musicSlider == null || sfxSlider == null || musicMuteLabel == null || sfxMuteLabel == null) return;

            musicSlider.SetValueWithoutNotify(AudioSettingsService.MusicVolume);
            sfxSlider.SetValueWithoutNotify(AudioSettingsService.SfxVolume);
            musicMuteLabel.text = AudioSettingsService.MusicMuted ? "Activar" : "Silenciar";
            sfxMuteLabel.text = AudioSettingsService.SfxMuted ? "Activar" : "Silenciar";
        }

        private static void CreateRow(RectTransform parent, string name, string label, float y, TMP_FontAsset font,
            out Slider slider, out Button muteButton, out TMP_Text muteLabel)
        {
            var title = CreateText(parent, name + "Label", label, 26f, font, Theme.Ink);
            Place(title.rectTransform, new Vector2(-245f, y), new Vector2(130f, 40f));

            slider = CreateSlider(parent, name + "Slider", y);
            muteButton = CreateMuteButton(parent, name + "MuteButton", new Vector2(230f, y), font, out muteLabel);
        }

        private static Slider CreateSlider(Transform parent, string name, float y)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Place(rt, new Vector2(10f, y), new Vector2(280f, 26f));

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(go.transform, false);
            var backgroundRt = background.GetComponent<RectTransform>();
            backgroundRt.anchorMin = new Vector2(0f, 0.35f);
            backgroundRt.anchorMax = new Vector2(1f, 0.65f);
            backgroundRt.offsetMin = Vector2.zero;
            backgroundRt.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = Theme.WoodDark;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0f);
            fillAreaRt.anchorMax = new Vector2(1f, 1f);
            fillAreaRt.offsetMin = new Vector2(0f, 0f);
            fillAreaRt.offsetMax = new Vector2(-13f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = Theme.Palm;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(13f, 0f);
            handleAreaRt.offsetMax = new Vector2(-13f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.GetComponent<RectTransform>();
            Place(handleRt, Vector2.zero, new Vector2(26f, 34f));
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = Theme.Sand;

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static Button CreateMuteButton(Transform parent, string name, Vector2 position, TMP_FontAsset font,
            out TMP_Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            Place(rt, position, new Vector2(140f, 46f));

            var image = go.GetComponent<Image>();
            image.color = Theme.Wood;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            label = CreateText(go.transform, "Label", "Silenciar", 24f, font, Theme.Sand);
            label.alignment = TextAlignmentOptions.Center;
            var labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size,
            TMP_FontAsset font, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = color;
            text.raycastTarget = false;
            if (font != null) text.font = font;
            return text;
        }

        private static void Place(RectTransform rt, Vector2 position, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }
    }
}
