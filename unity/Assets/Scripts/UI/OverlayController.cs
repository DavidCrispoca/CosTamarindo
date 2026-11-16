using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CosTamarindo.Core;
using CosTamarindo.Gameplay;
using CosTamarindo.Net;

namespace CosTamarindo.UI
{
    /// <summary>
    /// Orquesta las capas superpuestas del juego: pausa, fin de partida y reclamo.
    /// Al terminar la partida envía el puntaje de forma anónima; si entra al top 5
    /// permite reclamarlo con nombre + PIN (4-6 dígitos).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OverlayController : MonoBehaviour
    {
        [Header("Paneles")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Fin de partida")]
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text finalBestText;
        [SerializeField] private TMP_Text breakdownText;
        [SerializeField] private TMP_Text submitStatusText;

        [Header("Reclamo top 5 (opcional, se muestra solo si in_top5)")]
        [SerializeField] private GameObject claimPanel;
        [SerializeField] private TMP_InputField claimUsernameInput;
        [SerializeField] private TMP_InputField claimPinInput;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimStatusText;

        private readonly StringBuilder sb = new StringBuilder(256);
        private Coroutine submitRoutine;
        private SessionStartResponse currentSession;
        private bool scoreSubmitted;
        private bool claimed;
        private bool claimBusy;

        private static readonly Color ErrorColor = new Color(0.72f, 0.16f, 0.18f);
        private static readonly Color InfoColor = new Color(0.35f, 0.28f, 0.20f);
        private static readonly Color OkColor = new Color(0.16f, 0.55f, 0.26f);

        private void Awake()
        {
            WireButton("PauseButton", TogglePause);
            WireButton("ResumeButton", Resume);
            WireButton("RestartButton", Restart);
            WireButton("RetryButton", Restart);
            WireButton("MenuButton", Restart);
            WireButton("ClaimButton", SubmitClaim);
            if (claimButton != null) claimButton.onClick.AddListener(SubmitClaim);

            if (claimPinInput != null)
            {
                claimPinInput.contentType = TMP_InputField.ContentType.Password;
                claimPinInput.characterLimit = 6;
            }
            if (claimUsernameInput != null) claimUsernameInput.characterLimit = 16;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.StateChanged += HandleStateChanged;
            gm.MatchEnded += HandleMatchEnded;

            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (claimPanel != null) claimPanel.SetActive(false);

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

            scoreSubmitted = false;
            claimed = false;
            currentSession = null;
            if (claimPanel != null) claimPanel.SetActive(false);
            SetSubmitStatus("Enviando puntaje…", InfoColor);
            SetClaimStatus(string.Empty, InfoColor);

            if (submitRoutine != null) StopCoroutine(submitRoutine);
            submitRoutine = StartCoroutine(SubmitScoreFlow(score));
        }

        private IEnumerator SubmitScoreFlow(int score)
        {
            var gm = GameManager.Instance;
            var api = EnsureApiClient();
            if (api == null)
            {
                SetSubmitStatus("No hay conexión configurada.", ErrorColor);
                yield break;
            }

            ApiResult<SessionStartResponse> sessionRes = null;
            yield return api.StartSession(r => sessionRes = r);
            if (sessionRes == null || !sessionRes.Ok || sessionRes.Data == null)
            {
                SetSubmitStatus(MessageForError(sessionRes, "No se pudo iniciar la sesión."), ErrorColor);
                yield break;
            }
            currentSession = sessionRes.Data;

            int durationMs = gm != null ? gm.ElapsedMs : 0;
            int catches = gm != null ? gm.TotalCatches : 0;
            if (catches < 0) catches = 0;
            if (durationMs <= 0) durationMs = 1; // evita división/casos borde en firma coherente

            string signature = ScoreSigner.SignSessionHex(
                currentSession.session_key, currentSession.session_id, score, durationMs, catches, currentSession.nonce);

            var req = new ScoreRequest
            {
                session_id = currentSession.session_id,
                score = score,
                duration_ms = durationMs,
                catches = catches,
                nonce = currentSession.nonce,
                signature = signature
            };

            ApiResult<ScoreResponse> scoreRes = null;
            yield return api.SubmitScore(req, r => scoreRes = r);
            if (scoreRes == null || !scoreRes.Ok || scoreRes.Data == null)
            {
                SetSubmitStatus(MessageForError(scoreRes, "No se pudo enviar el puntaje."), ErrorColor);
                yield break;
            }

            scoreSubmitted = true;
            if (scoreRes.Data.in_top5)
            {
                SetSubmitStatus("¡Entraste al top 5! Reclamá tu nombre.", OkColor);
                ShowClaimUI(true);
            }
            else
            {
                SetSubmitStatus("Puntaje registrado. No entró al top 5.", InfoColor);
                ShowClaimUI(false);
            }
        }

        public void SubmitClaim()
        {
            if (!scoreSubmitted || claimed || currentSession == null || claimBusy) return;
            string username = claimUsernameInput != null ? claimUsernameInput.text.Trim() : string.Empty;
            string pin = claimPinInput != null ? claimPinInput.text.Trim() : string.Empty;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(pin))
            {
                SetClaimStatus("Ingresá nombre y PIN (4 a 6 dígitos).", ErrorColor);
                return;
            }

            StartCoroutine(ClaimFlow(username, pin));
        }

        private IEnumerator ClaimFlow(string username, string pin)
        {
            claimBusy = true;
            if (claimButton != null) claimButton.interactable = false;
            SetClaimStatus("Reclamando…", InfoColor);

            var api = EnsureApiClient();
            if (api == null)
            {
                SetClaimStatus("No hay conexión configurada.", ErrorColor);
                claimBusy = false;
                if (claimButton != null) claimButton.interactable = true;
                yield break;
            }

            var req = new ClaimRequest
            {
                session_id = currentSession.session_id,
                username = username,
                pin = pin
            };

            ApiResult<ClaimResponse> res = null;
            yield return api.Claim(req, r => res = r);

            if (res != null && res.Ok && res.Data != null)
            {
                claimed = true;
                string rank = res.Data.rank_position > 0 ? (" · Puesto #" + res.Data.rank_position) : string.Empty;
                SetClaimStatus("¡Listo, " + res.Data.username + "! Récord " + res.Data.best_score + rank, OkColor);
                SetSubmitStatus("Nombre registrado en la tabla.", OkColor);
                if (claimPinInput != null) claimPinInput.text = string.Empty;
            }
            else
            {
                SetClaimStatus(MessageForClaimError(res), ErrorColor);
                if (claimPinInput != null) { claimPinInput.text = string.Empty; claimPinInput.ActivateInputField(); }
            }

            claimBusy = false;
            if (claimButton != null) claimButton.interactable = !claimed;
        }

        private void ShowClaimUI(bool show)
        {
            if (claimPanel != null) claimPanel.SetActive(show);
            if (claimButton != null) claimButton.interactable = show && !claimed;
            if (show && claimUsernameInput != null) claimUsernameInput.ActivateInputField();
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

        private ApiClient EnsureApiClient()
        {
            if (ApiClient.Instance != null) return ApiClient.Instance;
            var existing = FindObjectOfType<ApiClient>();
            if (existing != null) return existing;
            var go = new GameObject("ApiClient");
            return go.AddComponent<ApiClient>();
        }

        private void SetSubmitStatus(string msg, Color color)
        {
            if (submitStatusText == null) return;
            submitStatusText.text = msg ?? string.Empty;
            submitStatusText.color = color;
        }

        private void SetClaimStatus(string msg, Color color)
        {
            if (claimStatusText == null) return;
            claimStatusText.text = msg ?? string.Empty;
            claimStatusText.color = color;
        }

        private static string MessageForError<T>(ApiResult<T> res, string fallback) where T : class
        {
            if (res == null) return fallback;
            if (!string.IsNullOrEmpty(res.ErrorMessage)) return res.ErrorMessage;
            return fallback;
        }

        private static string MessageForClaimError(ApiResult<ClaimResponse> res)
        {
            if (res == null) return "No se pudo reclamar el nombre.";
            switch (res.ErrorCode)
            {
                case "not_top5": return "Ya no está en el top 5. El ranking cambió.";
                case "bad_session": return "Este puntaje ya no se puede reclamar.";
                case "invalid_credentials": return "PIN incorrecto para ese nombre.";
                case "account_locked":
                    if (int.TryParse(res.RetryAfter, out int secs) && secs > 0)
                        return "Nombre bloqueado. Reintentá en " + secs + " s.";
                    return "Nombre bloqueado temporalmente. Reintentá más tarde.";
                case "weak_pin": return "Ese PIN es muy predecible. Elegí otro (4 a 6 dígitos).";
                case "username_taken": return "Ese nombre ya está en uso.";
                case "invalid_request": return "Nombre o PIN con formato inválido.";
                default:
                    return string.IsNullOrEmpty(res.ErrorMessage) ? "No se pudo reclamar el nombre." : res.ErrorMessage;
            }
        }
    }
}