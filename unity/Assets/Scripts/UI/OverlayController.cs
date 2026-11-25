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

        private static Color ErrorColor => Theme.Error;
        private static Color InfoColor => Theme.InkSoft;
        private static Color OkColor => Theme.Palm;

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
            if (finalBestText != null) finalBestText.text = string.Empty;
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
                SetOfflineScoreStatus();
                yield break;
            }

            ApiResult<SessionStartResponse> sessionRes = null;
            yield return api.StartSession(r => sessionRes = r);
            if (IsOffline(sessionRes))
            {
                SetOfflineScoreStatus();
                yield break;
            }
            if (!sessionRes.Ok || sessionRes.Data == null)
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
            if (IsOffline(scoreRes))
            {
                SetOfflineScoreStatus();
                yield break;
            }
            if (!scoreRes.Ok || scoreRes.Data == null)
            {
                SetSubmitStatus(MessageForError(scoreRes, "No se pudo enviar el puntaje."), ErrorColor);
                yield break;
            }

            scoreSubmitted = true;
            if (scoreRes.Data.in_top5)
            {
                SetSubmitStatus("¡Entraste al top 5! Reclama tu nombre.", OkColor);
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
                SetClaimStatus("Ingresa nombre y PIN (4 a 6 dígitos).", ErrorColor);
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
                SetClaimStatus("Sin conexión: intenta reclamar más tarde.", InfoColor);
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

            if (IsOffline(res))
            {
                SetClaimStatus("Sin conexión: intenta reclamar más tarde.", InfoColor);
                SetSubmitStatus("Sin conexión: el nombre no se pudo registrar.", InfoColor);
            }
            else if (res != null && res.Ok && res.Data != null)
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
            var captured = new List<KeyValuePair<ItemDef, int>>();
            foreach (var def in ItemCatalog.All)
            {
                counts.TryGetValue(def.Id, out int n);
                if (n > 0) captured.Add(new KeyValuePair<ItemDef, int>(def, n));
            }
            if (captured.Count == 0) return "Sin capturas";

            // Como máximo tres líneas: el desglose vive en una franja de 120 px
            // y no debe invadir el estado de envío ni el botón de reintentar.
            captured.Sort((a, b) => b.Value.CompareTo(a.Value));
            var sb = new StringBuilder(160);
            const int maxLines = 3;
            int shown = Math.Min(maxLines, captured.Count);
            for (int i = 0; i < shown; i++)
                sb.Append(captured[i].Key.DisplayName).Append("  x").Append(captured[i].Value).Append('\n');
            if (captured.Count > shown)
                sb.Append("… y ").Append(captured.Count - shown).Append(" tipos más");
            return sb.ToString().TrimEnd();
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
            var existing = FindFirstObjectByType<ApiClient>();
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

        /// <summary>
        /// Sin backend no hay nada que reclamar: es un estado normal del juego
        /// offline, no un error. Sin conexión el puntaje no se persiste: solo se
        /// registra en el leaderboard de Supabase cuando hay backend.
        /// </summary>
        private static bool IsOffline<T>(ApiResult<T> res) where T : class =>
            res == null || res.ErrorCode == "network_error" || res.HttpStatus <= 0;

        private void SetOfflineScoreStatus()
        {
            SetSubmitStatus("Sin conexión: no se pudo enviar el puntaje.", InfoColor);
            ShowClaimUI(false);
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
                        return "Nombre bloqueado. Reintenta en " + secs + " s.";
                    return "Nombre bloqueado temporalmente. Reintenta más tarde.";
                case "weak_pin": return "Ese PIN es muy predecible. Elige otro (4 a 6 dígitos).";
                case "username_taken": return "Ese nombre ya está en uso.";
                case "invalid_request": return "Nombre o PIN con formato inválido.";
                default:
                    return string.IsNullOrEmpty(res.ErrorMessage) ? "No se pudo reclamar el nombre." : res.ErrorMessage;
            }
        }
    }
}