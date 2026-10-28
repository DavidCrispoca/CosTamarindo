using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CosTamarindo.Core;
using CosTamarindo.Gameplay;

namespace CosTamarindo.Tests
{
    /// <summary>
    /// Pruebas de humo del bucle jugable sobre la escena real (<c>Game.unity</c>).
    /// Cubren las tres reglas que no pueden fallar: capturar suma, el peligro resta
    /// una vida y quedarse sin vidas termina la partida.
    /// </summary>
    public class GameplayPlayModeTests
    {
        private const string SceneName = "Game";

        private IEnumerator LoadGameScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null; // Awake/Start de la escena
            yield return null;

            Assert.IsNotNull(GameManager.Instance, "La escena debe tener un GameManager activo.");
        }

        [UnityTest]
        public IEnumerator CatchingGoodItem_IncreasesScore()
        {
            yield return LoadGameScene();

            var gm = GameManager.Instance;
            gm.StartMatch();
            int before = gm.Score;

            ItemCatalog.TryGet("item_cocada", out var cocada);
            gm.RegisterCatch(cocada);

            Assert.AreEqual(before + 15, gm.Score, "La cocada debe sumar 15 puntos.");
        }

        [UnityTest]
        public IEnumerator CatchingDangerItem_RemovesOneLife()
        {
            yield return LoadGameScene();

            var gm = GameManager.Instance;
            gm.StartMatch();
            int livesBefore = gm.Lives;

            ItemCatalog.TryGet("item_erizo", out var erizo);
            gm.RegisterCatch(erizo);

            Assert.AreEqual(livesBefore - 1, gm.Lives, "El erizo debe quitar una vida.");
            Assert.AreEqual(GameState.Playing, gm.State, "Con vidas restantes la partida continúa.");
        }

        [UnityTest]
        public IEnumerator LosingAllLives_EndsMatch()
        {
            yield return LoadGameScene();

            var gm = GameManager.Instance;
            gm.StartMatch();
            ItemCatalog.TryGet("item_medusa", out var medusa);

            while (gm.Lives > 0) gm.RegisterCatch(medusa);

            Assert.AreEqual(GameState.GameOver, gm.State, "Al llegar a cero vidas la partida termina.");
        }

        [UnityTest]
        public IEnumerator Spawner_ProducesItemsWhilePlaying()
        {
            yield return LoadGameScene();

            var gm = GameManager.Instance;
            gm.StartMatch();

            yield return new WaitForSeconds(1.6f);

            var items = Object.FindObjectsByType<FallingItem>(FindObjectsSortMode.None);
            Assert.Greater(items.Length, 0, "El spawner debe generar ítems durante la partida.");
        }
    }
}
