using System.Text;
using TMPro;
using UnityEngine;
using CosTamarindo.Core;
using CosTamarindo.Gameplay;

namespace CosTamarindo.UI
{
    /// <summary>
    /// Refleja el estado de la partida en el HUD: puntaje, tiempo, vidas y récord.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text bestText;

        private readonly StringBuilder sb = new StringBuilder(8);

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.ScoreChanged += OnScoreChanged;
            gm.LivesChanged += OnLivesChanged;
            gm.TimeChanged += OnTimeChanged;

            OnScoreChanged(gm.Score);
            OnLivesChanged(gm.Lives);
            OnTimeChanged(gm.TimeLeft);
            RefreshBest(gm.BestScore);
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.ScoreChanged -= OnScoreChanged;
            gm.LivesChanged -= OnLivesChanged;
            gm.TimeChanged -= OnTimeChanged;
        }

        private void OnScoreChanged(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString();
            if (GameManager.Instance != null) RefreshBest(GameManager.Instance.BestScore);
        }

        private void OnLivesChanged(int lives)
        {
            if (livesText == null) return;

            sb.Clear();
            int total = Mathf.Max(lives, 0);
            for (int i = 0; i < total; i++) sb.Append('\u2665');
            if (sb.Length == 0) sb.Append('\u2661');
            livesText.text = sb.ToString();
        }

        private void OnTimeChanged(float secondsLeft)
        {
            if (timerText == null) return;

            int s = Mathf.CeilToInt(secondsLeft);
            timerText.text = $"{s / 60:00}:{s % 60:00}";
        }

        private void RefreshBest(int best)
        {
            if (bestText != null) bestText.text = "Récord " + best;
        }
    }
}
