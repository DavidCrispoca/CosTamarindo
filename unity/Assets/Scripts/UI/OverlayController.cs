using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CosTamarindo.Core;
using CosTamarindo.Gameplay;

namespace CosTamarindo.UI
{
    /// <summary>
    /// Orquesta las capas superpuestas del juego: pausa, fin de partida y botones.
    /// Los botones se enlazan por nombre para que la escena pueda rearmarse sin
    /// volver a cablear eventos en el inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OverlayController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text finalBestText;
        [SerializeField] private TMP_Text breakdownText;

        private readonly StringBuilder sb = new StringBuilder(256);

        private void Awake()
        {
            WireButton("PauseButton", TogglePause);
            WireButton("ResumeButton", Resume);
            WireButton("RestartButton", Restart);
            WireButton("RetryButton", Restart);
            WireButton("MenuButton", Restart);
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.StateChanged += HandleStateChanged;
            gm.MatchEnded += HandleMatchEnded;

            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            gm.StartMatch();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.StateChanged -= HandleStateChanged;
            gm.MatchEnded -= HandleMatchEnded;
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
                TogglePause();
        }

        private void HandleStateChanged(GameState state)
        {
            if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
        }

        private void HandleMatchEnded(int score)
        {
            var gm = GameManager.Instance;
            if (finalScoreText != null) finalScoreText.text = score.ToString();
            if (finalBestText != null) finalBestText.text = "Récord " + (gm != null ? gm.BestScore : score);
            if (breakdownText != null && gm != null) breakdownText.text = BuildBreakdown(gm.CaughtCounts);
        }

        private static string BuildBreakdown(IReadOnlyDictionary<string, int> counts)
        {
            var sb = new StringBuilder(256);
            foreach (var def in ItemCatalog.All)
            {
                counts.TryGetValue(def.Id, out int n);
                if (n == 0) continue;
                sb.Append(def.DisplayName).Append("  x").Append(n).Append('\n');
            }
            return sb.Length == 0 ? "Sin capturas" : sb.ToString().TrimEnd();
        }

        private void WireButton(string objectName, UnityEngine.Events.UnityAction action)
        {
            var found = transform.root.GetComponentsInChildren<Button>(true);
            foreach (var button in found)
            {
                if (button.name != objectName) continue;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(action);
            }
        }

        private void TogglePause()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.State == GameState.Playing) gm.Pause();
            else if (gm.State == GameState.Paused) gm.Resume();
        }

        private void Resume() => GameManager.Instance?.Resume();

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
