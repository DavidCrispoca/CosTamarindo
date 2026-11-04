using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CosTamarindo.Auth;

namespace CosTamarindo.UI
{
    /// <summary>
    /// Controla la pantalla de acceso: usuario + PIN, con estados de validación,
    /// error, «conectando» y bloqueo progresivo.
    ///
    /// Habla con <see cref="AuthStub"/>, que hoy es local. El día que exista el
    /// backend de F5 solo cambia el stub; este controlador no se toca.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoginController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField pinInput;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button enterButton;

        [Header("Escenas")]
        [SerializeField] private string gameSceneName = "Game";

        private bool busy;

        private static readonly Color ErrorColor = new Color(0.72f, 0.16f, 0.18f);
        private static readonly Color InfoColor = new Color(0.35f, 0.28f, 0.20f);
        private static readonly Color OkColor = new Color(0.16f, 0.55f, 0.26f);

        private void Awake()
        {
            if (enterButton != null) enterButton.onClick.AddListener(Submit);
            if (pinInput != null) pinInput.onSubmit.AddListener(_ => Submit());
            if (usernameInput != null) usernameInput.onSubmit.AddListener(_ => Submit());

            SetStatus("Ingresá con tu usuario y un PIN de 4 dígitos.", InfoColor);

            // El PIN se enmascara y no admite más de 4 caracteres.
            if (pinInput != null)
            {
                pinInput.contentType = TMP_InputField.ContentType.Password;
                pinInput.characterLimit = 4;
                pinInput.ForceLabelUpdate();
            }

            if (usernameInput != null)
            {
                usernameInput.characterLimit = 16;
                usernameInput.ActivateInputField();
            }
        }

        private void Update()
        {
            if (enterButton != null && !busy)
                enterButton.interactable = !AuthStub.IsLocked;

            // Cuenta regresiva visible mientras dura el bloqueo (A.8.5).
            if (AuthStub.IsLocked && !busy)
            {
                SetStatus("Bloqueado. Reintentá en " + Mathf.CeilToInt(AuthStub.SecondsUntilUnlock) + " s.", ErrorColor);
            }
        }

        public void Submit()
        {
            if (busy) return;

            string user = usernameInput != null ? usernameInput.text : string.Empty;
            string pin = pinInput != null ? pinInput.text : string.Empty;

            StartCoroutine(SignInRoutine(user, pin));
        }

        private IEnumerator SignInRoutine(string user, string pin)
        {
            busy = true;
            if (enterButton != null) enterButton.interactable = false;
            SetStatus("Conectando…", InfoColor);

            AuthResult result = default;
            bool done = false;

            yield return AuthStub.SignIn(user, pin, r =>
            {
                result = r;
                done = true;
            });

            if (!done)
            {
                busy = false;
                if (enterButton != null) enterButton.interactable = true;
                yield break;
            }

            if (result.Ok)
            {
                SetStatus(result.Message, OkColor);
                yield return new WaitForSecondsRealtime(0.35f);

                if (!string.IsNullOrEmpty(gameSceneName) && Application.CanStreamedLevelBeLoaded(gameSceneName))
                    SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
                else
                {
                    SetStatus("No se encontró la escena '" + gameSceneName + "' en Build Settings.", ErrorColor);
                    busy = false;
                    if (enterButton != null) enterButton.interactable = true;
                }

                yield break;
            }

            SetStatus(result.Message, ErrorColor);

            if (pinInput != null)
            {
                pinInput.text = string.Empty;
                pinInput.ActivateInputField();
            }

            busy = false;
            if (enterButton != null) enterButton.interactable = !AuthStub.IsLocked;
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = color;
        }
    }
}
