using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace CosTamarindo.Net
{
    public sealed class ApiResult<T>
    {
        public bool Ok;
        public long HttpStatus;
        public T Data;
        public string ErrorCode;
        public string ErrorMessage;
        public string Raw;
        public string RetryAfter; // header, useful for 423

        public static ApiResult<T> Success(T data, long status, string raw = null) =>
            new ApiResult<T> { Ok = true, HttpStatus = status, Data = data, Raw = raw };

        public static ApiResult<T> Failure(long status, string code, string message, string raw = null, string retryAfter = null) =>
            new ApiResult<T> { Ok = false, HttpStatus = status, ErrorCode = code, ErrorMessage = message, Raw = raw, RetryAfter = retryAfter };
    }

    /// <summary>
    /// Cliente HTTP mínimo para la API del juego (UnityWebRequest, corutinas).
    /// El flujo de juego es anónimo: no se guardan tokens de jugador.
    /// </summary>
    public class ApiClient : MonoBehaviour
    {
        public static ApiClient Instance { get; private set; }

        [Header("API")]
        [SerializeField] private string baseUrl = "https://juego.local/api";
        [SerializeField] private float timeoutSeconds = 10f;

        public string BaseUrl
        {
            get => baseUrl;
            set => baseUrl = (value ?? string.Empty).TrimEnd('/');
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                baseUrl = "/api";
            }
            else
            {
                baseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
            }
        }

        public IEnumerator StartSession(Action<ApiResult<SessionStartResponse>> onDone)
        {
            yield return PostJson("/session/start", "{}", raw =>
            {
                try { return JsonUtility.FromJson<SessionStartResponse>(raw); }
                catch { return null; }
            }, onDone);
        }

        public IEnumerator SubmitScore(ScoreRequest request, Action<ApiResult<ScoreResponse>> onDone)
        {
            string body = JsonUtility.ToJson(request);
            yield return PostJson("/score", body, raw =>
            {
                try { return JsonUtility.FromJson<ScoreResponse>(raw); }
                catch { return null; }
            }, onDone);
        }

        public IEnumerator Claim(ClaimRequest request, Action<ApiResult<ClaimResponse>> onDone)
        {
            string body = JsonUtility.ToJson(request);
            yield return PostJson("/claim", body, raw =>
            {
                try { return JsonUtility.FromJson<ClaimResponse>(raw); }
                catch { return null; }
            }, onDone);
        }

        public IEnumerator GetLeaderboard(int limit, Action<ApiResult<LeaderboardResponse>> onDone)
        {
            string path = "/leaderboard?limit=" + Mathf.Clamp(limit, 1, 20);
            yield return GetJson(path, raw =>
            {
                try { return JsonUtility.FromJson<LeaderboardResponse>(raw); }
                catch { return null; }
            }, onDone);
        }

        private IEnumerator GetJson<T>(string path, Func<string, T> parse, Action<ApiResult<T>> onDone) where T : class
        {
            using (var req = UnityWebRequest.Get(baseUrl + path))
            {
                req.timeout = Mathf.CeilToInt(timeoutSeconds);
                req.SetRequestHeader("Accept", "application/json");
                yield return req.SendWebRequest();
                onDone?.Invoke(BuildResult(req, parse));
            }
        }

        private IEnumerator PostJson<T>(string path, string jsonBody, Func<string, T> parse, Action<ApiResult<T>> onDone) where T : class
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
            using (var req = new UnityWebRequest(baseUrl + path, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = Mathf.CeilToInt(timeoutSeconds);
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Accept", "application/json");
                yield return req.SendWebRequest();
                onDone?.Invoke(BuildResult(req, parse));
            }
        }

        private static ApiResult<T> BuildResult<T>(UnityWebRequest req, Func<string, T> parse) where T : class
        {
            long status = req.responseCode;
            string raw = req.downloadHandler != null ? req.downloadHandler.text : null;
            string retryAfter = req.GetResponseHeader("Retry-After");

            if (req.result == UnityWebRequest.Result.Success && status >= 200 && status < 300)
            {
                T data = parse != null ? parse(raw) : null;
                if (data == null && !string.IsNullOrEmpty(raw))
                    return ApiResult<T>.Failure(status, "parse_error", "Respuesta inválida del servidor.", raw, retryAfter);
                return ApiResult<T>.Success(data, status, raw);
            }

            // Error HTTP o de red: intentar extraer {"code","message"}
            string code = "network_error";
            string message = "No se pudo conectar con el servidor.";
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    var err = JsonUtility.FromJson<ApiError>(raw);
                    if (err != null && !string.IsNullOrEmpty(err.code))
                    {
                        code = err.code;
                        message = string.IsNullOrEmpty(err.message) ? code : err.message;
                    }
                }
                catch { /* ignorar parse de error */ }
            }
            else if (req.result != UnityWebRequest.Result.ConnectionError && status > 0)
            {
                message = "Error del servidor (" + status + ").";
            }

            return ApiResult<T>.Failure(status, code, message, raw, retryAfter);
        }
    }
}