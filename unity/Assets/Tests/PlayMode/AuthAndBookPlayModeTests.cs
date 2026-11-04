using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CosTamarindo.Auth;
using CosTamarindo.Core;
using CosTamarindo.Gameplay;
using CosTamarindo.UI;

namespace CosTamarindo.Tests
{
    /// <summary>
    /// Pruebas de la autenticación local y del Libro.
    ///
    /// Las reglas de <see cref="AuthStub"/> no son un detalle del stub: son el
    /// diseño de A.8.5 (validación de entrada y bloqueo progresivo). Cuando F5
    /// cambie la implementación por Supabase, estos tests deben seguir pasando.
    /// </summary>
    public class AuthAndBookPlayModeTests
    {
        private const string SceneName = "Game";

        [SetUp]
        public void SetUp() => AuthStub.ResetForTests();

        [TearDown]
        public void TearDown() => AuthStub.ResetForTests();

        // ── A.8.5: validación de entrada ─────────────────────────────────

        [UnityTest]
        public IEnumerator RejectsUsernameTooShort()
        {
            AuthResult result = default;
            yield return AuthStub.SignIn("ab", "1234", r => result = r);

            Assert.AreEqual(AuthStatus.InvalidInput, result.Status);
            Assert.IsFalse(result.Ok, "Un usuario de 2 caracteres no debe autenticarse.");
        }

        [UnityTest]
        public IEnumerator RejectsUsernameWithInvalidCharacters()
        {
            AuthResult result = default;
            yield return AuthStub.SignIn("ana perez!", "1234", r => result = r);

            Assert.AreEqual(AuthStatus.InvalidInput, result.Status,
                "Espacios y símbolos deben rechazarse: el usuario viaja en un correo sintético.");
        }

        [UnityTest]
        public IEnumerator RejectsPinThatIsNotFourDigits()
        {
            AuthResult result = default;
            yield return AuthStub.SignIn("ana", "123", r => result = r);

            Assert.AreEqual(AuthStatus.InvalidInput, result.Status);

            result = default;
            yield return AuthStub.SignIn("ana", "12a4", r => result = r);

            Assert.AreEqual(AuthStatus.InvalidInput, result.Status, "El PIN debe ser numérico.");
        }

        // ── Flujo correcto ───────────────────────────────────────────────

        [UnityTest]
        public IEnumerator FirstLoginCreatesAccountAndReturnsTokenInMemory()
        {
            AuthResult result = default;
            yield return AuthStub.SignIn("ana_01", "1234", r => result = r);

            Assert.AreEqual(AuthStatus.Success, result.Status);
            Assert.IsTrue(AuthStub.IsAuthenticated);
            Assert.AreEqual("ana_01", AuthStub.CurrentUser);
            Assert.IsFalse(string.IsNullOrEmpty(AuthStub.SessionToken), "El token debe vivir en memoria.");

            // No debe tocarse PlayerPrefs: el token y el usuario son volátiles (A.8.9).
            Assert.IsFalse(PlayerPrefs.HasKey("ct.token"));
            Assert.IsFalse(PlayerPrefs.HasKey("ct.user"));
        }

        [UnityTest]
        public IEnumerator WrongPinForExistingAccountFails()
        {
            AuthResult first = default;
            yield return AuthStub.SignIn("beto", "1111", r => first = r);
            Assert.AreEqual(AuthStatus.Success, first.Status);

            AuthResult second = default;
            yield return AuthStub.SignIn("beto", "2222", r => second = r);

            Assert.AreEqual(AuthStatus.WrongCredentials, second.Status);
            Assert.IsFalse(second.Ok);
        }

        // ── A.8.5: bloqueo progresivo ────────────────────────────────────

        [UnityTest]
        public IEnumerator FiveFailures_LockTheAccount()
        {
            // Crea la cuenta con PIN 1111.
            AuthResult created = default;
            yield return AuthStub.SignIn("carla", "1111", r => created = r);
            Assert.AreEqual(AuthStatus.Success, created.Status);

            // Cinco intentos fallidos consecutivos.
            for (int attempt = 1; attempt <= AuthStub.MaxAttempts; attempt++)
            {
                AuthResult failed = default;
                yield return AuthStub.SignIn("carla", "9999", r => failed = r);

                Assert.IsFalse(failed.Ok, $"El intento {attempt} no debe pasar.");
            }

            Assert.IsTrue(AuthStub.IsLocked, "Tras 5 fallos la cuenta debe quedar bloqueada.");
            Assert.Greater(AuthStub.SecondsUntilUnlock, 0f, "Debe haber una cuenta atrás activa.");

            // Con la cuenta bloqueada ni siquiera el PIN correcto entra.
            AuthResult duringLock = default;
            yield return AuthStub.SignIn("carla", "1111", r => duringLock = r);
            Assert.AreEqual(AuthStatus.Locked, duringLock.Status,
                "El bloqueo debe aplicarse antes de validar el PIN.");
        }

        [UnityTest]
        public IEnumerator SuccessfulLoginResetsFailureCounter()
        {
            AuthResult created = default;
            yield return AuthStub.SignIn("dani", "1234", r => created = r);
            Assert.AreEqual(AuthStatus.Success, created.Status);

            AuthResult fail = default;
            yield return AuthStub.SignIn("dani", "0000", r => fail = r);
            Assert.AreEqual(AuthStatus.WrongCredentials, fail.Status);
            Assert.AreEqual(1, AuthStub.FailedAttempts);

            AuthResult ok = default;
            yield return AuthStub.SignIn("dani", "1234", r => ok = r);
            Assert.AreEqual(AuthStatus.Success, ok.Status);
            Assert.AreEqual(0, AuthStub.FailedAttempts, "Un acierto debe limpiar el contador.");
        }

        // ── Libro de la colección (F2) ───────────────────────────────────

        [UnityTest]
        public IEnumerator Book_OpensAndCloses()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            yield return null;

            var book = Object.FindFirstObjectByType<BookView>();
            Assert.IsNotNull(book, "La escena Game debe tener un BookView.");

            Assert.IsFalse(book.IsOpen, "El Libro debe arrancar cerrado.");
            book.Open();
            Assert.IsTrue(book.IsOpen);
            book.Close();
            Assert.IsFalse(book.IsOpen);
        }

        [UnityTest]
        public IEnumerator CatchingCollectible_UnlocksItInTheBook()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            yield return null;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);

            ItemCatalog.TryGet("item_tamarindo", out var tamarindo);
            CollectionBook.ResetAll(new[] { tamarindo.Id });
            Assert.IsFalse(CollectionBook.IsUnlocked(tamarindo.Id), "Debe empezar bloqueado.");

            gm.StartMatch();
            gm.RegisterCatch(tamarindo);

            Assert.IsTrue(CollectionBook.IsUnlocked(tamarindo.Id),
                "Capturar un coleccionable debe desbloquearlo en el Libro.");

            // La basura no es coleccionable: capturarla no debe desbloquear nada.
            ItemCatalog.TryGet("item_basura", out var basura);
            CollectionBook.ResetAll(new[] { basura.Id });
            gm.RegisterCatch(basura);
            Assert.IsFalse(CollectionBook.IsUnlocked(basura.Id), "La basura no entra al Libro.");
        }
    }
}
