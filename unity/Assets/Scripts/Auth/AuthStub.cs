using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

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
    /// Autenticación **local de desarrollo**: no habla con Supabase.
    ///
    /// Existe para que la pantalla de acceso y sus estados (validación, error,
    /// bloqueo progresivo) estén construidos y probados antes de F4. Cuando el
    /// backend de F5 exponga <c>POST /api/auth/login</c>, se reemplaza el cuerpo
    /// de <see cref="SignIn"/> por la llamada real y la UI no cambia.
    ///
    /// Reglas que ya se aplican aquí, porque son de diseño y no de red:
    ///   · usuario  ^[A-Za-z0-9_]{3,16}$
    ///   · PIN de 4 dígitos exactos
    ///   · 5 fallos consecutivos → 60 s de bloqueo (A.8.5)
    ///   · el token vive solo en memoria: nunca en PlayerPrefs (A.8.9)
    /// </summary>
    public static class AuthStub
    {
        public const int MaxAttempts = 5;
        public const float LockoutSeconds = 60f;
        public const string EmailDomain = "players.costamarindo.local";

        private static readonly Regex UsernamePattern = new Regex(@"^[A-Za-z0-9_]{3,16}$", RegexOptions.Compiled);
        private static readonly Regex PinPattern = new Regex(@"^\d{4}$", RegexOptions.Compiled);

        /// <summary>Cuentas creadas durante esta sesión de ejecución (solo memoria).</summary>
        private static readonly Dictionary<string, string> Registry = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static int failedAttempts;
        private static float lockedUntilRealtime;

        /// <summary>Usuario autenticado. Se pierde al cerrar el juego, a propósito.</summary>
        public static string CurrentUser { get; private set; }

        /// <summary>Token en memoria. En F5 será el JWT real; aquí es un marcador.</summary>
        public static string SessionToken { get; private set; }

        public static bool IsAuthenticated => !string.IsNullOrEmpty(SessionToken);

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

            // Latencia simulada: la UI debe mostrar «Conectando…».
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
    }
}
