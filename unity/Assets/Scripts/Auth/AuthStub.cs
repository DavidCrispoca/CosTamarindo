using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace CosTamarindo.Auth
{
    public enum AuthStatus
    {
        Success,
        InvalidInput,
        WrongCredentials,
        Locked,
        ServerError
    }

    public readonly struct AuthResult
    {
        public readonly AuthStatus Status;
        public readonly string Message;

        public AuthResult(AuthStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public bool Ok => Status == AuthStatus.Success;
    }

    /// <summary>
    /// Autenticación del juego.
    ///
    /// Primero intenta el backend real (<c>/api/auth/login</c> y, si el usuario
    /// no existe, <c>/api/auth/register</c>). Si el backend no está desplegado
    /// (error de red), cae al modo local de desarrollo para que la pantalla de
    /// acceso siga siendo usable. Un PIN incorrecto del backend **no** cae al
    /// modo local: sería un bypass de autenticación.
    ///
    /// Reglas que ya se aplican aquí, porque son de diseño y no de red:
    ///   · usuario  ^[A-Za-z0-9_]{3,16}$
    ///   · PIN de 4 dígitos exactos
    ///   · 5 fallos consecutivos → 60 s de bloqueo (A.8.5) en modo local
    ///   · el token vive solo en memoria: nunca en PlayerPrefs (A.8.9)
    /// </summary>
    public static class AuthStub
    {
        public const int MaxAttempts = 5;
        public const float LockoutSeconds = 60f;
        public const string EmailDomain = "players.costamarindo.local";

        // En WebGL el juego se sirve desde https://juego.local, así que esta
        // ruta es same-origin y pasa por Nginx. En el editor hace falta el
        // registro hosts que ya está documentado en infra/README.md.
        private const string ApiBaseUrl = "https://juego.local/api";

        private static readonly Regex UsernamePattern = new Regex(@"^[A-Za-z0-9_]{3,16}$", RegexOptions.Compiled);
        private static readonly Regex PinPattern = new Regex(@"^\d{4}$", RegexOptions.Compiled);

        /// <summary>Cuentas creadas durante esta sesión de ejecución (solo memoria).</summary>
        private static readonly Dictionary<string, string> Registry = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static int failedAttempts;
        private static float lockedUntilRealtime;

        /// <summary>Usuario autenticado. Se pierde al cerrar el juego, a propósito.</summary>
        public static string CurrentUser { get; private set; }

        /// <summary>JWT del backend o marcador local. Vive solo en memoria.</summary>
        public static string SessionToken { get; private set; }

        public static bool IsAuthenticated => !string.IsNullOrEmpty(SessionToken);

        /// <summary>True solo si el token tiene forma de JWT (tres partes).</summary>
        public static bool IsBackendToken
        {
            get
            {
                if (string.IsNullOrEmpty(SessionToken)) return false;
                return SessionToken.Split('.').Length == 3;
            }
        }

        public static int FailedAttempts => failedAttempts;

        public static bool IsLocked => Time.realtimeSinceStartup < lockedUntilRealtime;

        public static float SecondsUntilUnlock =>
            IsLocked ? Mathf.Max(0f, lockedUntilRealtime - Time.realtimeSinceStartup) : 0f;

        public static string ToEmail(string username) => username + "@" + EmailDomain;

        public static void SignOut()
        {
            CurrentUser = null;
            SessionToken = null;
            failedAttempts = 0;
            lockedUntilRealtime = 0f;
        }

        /// <summary>
        /// Solo para pruebas: borra las cuentas en memoria y el estado de bloqueo.
        /// No se usa en el juego; existe para que cada test arranque de cero.
        /// </summary>
        public static void ResetForTests()
        {
            Registry.Clear();
            SignOut();
        }

        /// <summary>
        /// Intenta iniciar sesión. La latencia se simula con <c>yield</c> para que
        /// la UI tenga los mismos estados intermedios que tendrá con la red real.
        /// </summary>
        public static IEnumerator SignIn(string username, string pin, Action<AuthResult> onDone)
        {
            username = (username ?? string.Empty).Trim();
            pin = (pin ?? string.Empty).Trim();

            if (IsLocked)
            {
                onDone?.Invoke(new AuthResult(
                    AuthStatus.Locked,
                    "Demasiados intentos. Esperá " + Mathf.CeilToInt(SecondsUntilUnlock) + " s."));
                yield break;
            }

            if (!UsernamePattern.IsMatch(username))
            {
                onDone?.Invoke(new AuthResult(
                    AuthStatus.InvalidInput,
                    "El usuario debe tener de 3 a 16 caracteres: letras, números o guion bajo."));
                yield break;
            }

            if (!PinPattern.IsMatch(pin))
            {
                onDone?.Invoke(new AuthResult(AuthStatus.InvalidInput, "El PIN debe tener exactamente 4 dígitos."));
                yield break;
            }

            // 1) Backend real. Si no está desplegado, se usa el modo local.
            BackendAttempt backend = default;
            yield return BackendSignIn(username, pin, result => backend = result);

            if (backend.Status == BackendStatus.Success)
            {
                failedAttempts = 0;
                lockedUntilRealtime = 0f;
                CurrentUser = username;
                SessionToken = backend.Token;
                onDone?.Invoke(new AuthResult(AuthStatus.Success, "Hola, " + username + "."));
                yield break;
            }

            if (backend.Status == BackendStatus.InvalidCredentials)
            {
                onDone?.Invoke(new AuthResult(AuthStatus.WrongCredentials, "PIN incorrecto. Verificá tus datos."));
                yield break;
            }

            if (backend.Status == BackendStatus.Locked)
            {
                lockedUntilRealtime = Time.realtimeSinceStartup + LockoutSeconds;
                onDone?.Invoke(new AuthResult(AuthStatus.Locked, "Cuenta bloqueada. Esperá un momento."));
                yield break;
            }

            if (backend.Status == BackendStatus.InvalidInput)
            {
                onDone?.Invoke(new AuthResult(AuthStatus.InvalidInput, backend.Message));
                yield break;
            }

            if (backend.Status == BackendStatus.ServerError)
            {
                onDone?.Invoke(new AuthResult(AuthStatus.ServerError, "El servidor no está disponible. Intentá más tarde."));
                yield break;
            }

            // 2) Modo local de desarrollo: solo si el backend no respondió por red.
            yield return new WaitForSecondsRealtime(0.45f);

            if (Registry.TryGetValue(username, out string registeredPin))
            {
                if (registeredPin != pin)
                {
                    failedAttempts++;

                    if (failedAttempts >= MaxAttempts)
                    {
                        lockedUntilRealtime = Time.realtimeSinceStartup + LockoutSeconds;
                        onDone?.Invoke(new AuthResult(
                            AuthStatus.Locked,
                            "Cuenta bloqueada por " + Mathf.RoundToInt(LockoutSeconds) + " s."));
                        yield break;
                    }

                    int remaining = MaxAttempts - failedAttempts;
                    onDone?.Invoke(new AuthResult(
                        AuthStatus.WrongCredentials,
                        "PIN incorrecto. Te quedan " + remaining + " intento(s)."));
                    yield break;
                }
            }
            else
            {
                // Primer acceso con ese usuario: la cuenta se crea con ese PIN.
                Registry[username] = pin;
            }

            failedAttempts = 0;
            lockedUntilRealtime = 0f;
            CurrentUser = username;
            SessionToken = Guid.NewGuid().ToString("N");

            onDone?.Invoke(new AuthResult(AuthStatus.Success, "Hola, " + username + "."));
        }

        private enum BackendStatus
        {
            Unavailable,
            Success,
            InvalidCredentials,
            Locked,
            InvalidInput,
            ServerError
        }

        private readonly struct BackendAttempt
        {
            public readonly BackendStatus Status;
            public readonly string Token;
            public readonly string Message;

            public BackendAttempt(BackendStatus status, string token = null, string message = null)
            {
                Status = status;
                Token = token;
                Message = message;
            }
        }

        [Serializable]
        private sealed class Credentials
        {
            public string username;
            public string pin;
        }

        [Serializable]
        private sealed class TokenPair
        {
            public string access_token;
            public string refresh_token;
            public string token_type;
            public int expires_in;
            public string user_id;
            public string username;
        }

        [Serializable]
        private sealed class ErrorBody
        {
            public string code;
            public string message;
        }

        private static IEnumerator BackendSignIn(string username, string pin, Action<BackendAttempt> onDone)
        {
            var login = BuildAuthRequest("login", username, pin);
            yield return login.SendWebRequest();

            int loginCode = (int)login.responseCode;
            string loginBody = login.downloadHandler != null ? login.downloadHandler.text : string.Empty;
            var loginResult = login.result;
            login.Dispose();

            if (loginResult == UnityWebRequest.Result.ConnectionError || loginCode == 0)
            {
                onDone?.Invoke(new BackendAttempt(BackendStatus.Unavailable));
                yield break;
            }

            // 404/502/503/504: el backend no está desplegado o el proxy no lo
            // encuentra. Se permite el modo local de desarrollo.
            if (loginCode == 404 || loginCode == 502 || loginCode == 503 || loginCode == 504)
            {
                onDone?.Invoke(new BackendAttempt(BackendStatus.Unavailable));
                yield break;
            }

            if (loginCode == 200)
            {
                if (TryReadToken(loginBody, out string token))
                {
                    onDone?.Invoke(new BackendAttempt(BackendStatus.Success, token));
                }
                else
                {
                    onDone?.Invoke(new BackendAttempt(BackendStatus.ServerError));
                }
                yield break;
            }

            if (loginCode == 423 || loginCode == 429)
            {
                onDone?.Invoke(new BackendAttempt(BackendStatus.Locked));
                yield break;
            }

            if (loginCode >= 500)
            {
                onDone?.Invoke(new BackendAttempt(BackendStatus.ServerError));
                yield break;
            }

            if (loginCode != 401)
            {
                onDone?.Invoke(new BackendAttempt(BackendStatus.ServerError));
                yield break;
            }

            // 401: puede ser un usuario nuevo. Se intenta registrar una vez.
            var register = BuildAuthRequest("register", username, pin);
            yield return register.SendWebRequest();

            int registerCode = (int)register.responseCode;
            string registerBody = register.downloadHandler != null ? register.downloadHandler.text : string.Empty;
            var registerResult = register.result;
            register.Dispose();

            if (registerResult == UnityWebRequest.Result.ConnectionError || registerCode == 0)
            {
                onDone?.Invoke(new BackendAttempt(BackendStatus.Unavailable));
                yield break;
            }

            if (registerCode == 404 || registerCode == 502 || registerCode == 503 || registerCode == 504)
            {
                onDone?.Invoke(new BackendAttempt(BackendStatus.Unavailable));
                yield break;
            }

            if (registerCode == 201)
            {
                if (TryReadToken(registerBody, out string token))
                {
                    onDone?.Invoke(new BackendAttempt(BackendStatus.Success, token));
                }
                else
                {
                    onDone?.Invoke(new BackendAttempt(BackendStatus.ServerError));
                }
                yield break;
            }

            if (registerCode == 409)
            {
                onDone?.Invoke(new BackendAttempt(BackendStatus.InvalidCredentials));
                yield break;
            }

            if (registerCode == 422)
            {
                string message = ReadError(registerBody);
                onDone?.Invoke(new BackendAttempt(
                    BackendStatus.InvalidInput,
                    message: string.IsNullOrEmpty(message) ? "Revisá el usuario y el PIN." : message));
                yield break;
            }

            if (registerCode == 423 || registerCode == 429)
            {
                onDone?.Invoke(new BackendAttempt(BackendStatus.Locked));
                yield break;
            }

            onDone?.Invoke(new BackendAttempt(BackendStatus.ServerError));
        }

        private static UnityWebRequest BuildAuthRequest(string action, string username, string pin)
        {
            string json = JsonUtility.ToJson(new Credentials { username = username, pin = pin });
            var request = new UnityWebRequest(ApiBaseUrl + "/auth/" + action, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 8
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private static bool TryReadToken(string body, out string token)
        {
            token = null;
            if (string.IsNullOrEmpty(body)) return false;

            try
            {
                var pair = JsonUtility.FromJson<TokenPair>(body);
                if (pair == null || string.IsNullOrEmpty(pair.access_token)) return false;
                token = pair.access_token;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static string ReadError(string body)
        {
            if (string.IsNullOrEmpty(body)) return null;
            try
            {
                var error = JsonUtility.FromJson<ErrorBody>(body);
                return error != null ? error.message : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
