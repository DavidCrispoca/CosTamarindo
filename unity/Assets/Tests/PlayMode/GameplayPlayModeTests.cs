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

        [UnityTest]
        public IEnumerator PlayerCatchLine_SitsAtBasketHeight()
        {
            yield return LoadGameScene();

            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player, "La escena debe tener al jugador.");

            var visual = player.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(visual, "El personaje debe tener un SpriteRenderer en el hijo Visual.");
            Assert.IsNotNull(visual.sprite, "El sprite de la Palenquera debe estar asignado.");

            float feet = player.transform.position.y;
            Assert.Greater(player.CatchHeight, feet + 1.5f,
                "La línea de captura debe estar a la altura de la canasta, no de los pies.");
            Assert.LessOrEqual(player.CatchHeight, visual.bounds.max.y + 0.01f,
                "La línea de captura no puede quedar por encima del sprite.");
        }

        [UnityTest]
        public IEnumerator Background_CoversCameraView()
        {
            yield return LoadGameScene();

            var background = GameObject.Find("Background");
            Assert.IsNotNull(background, "La escena debe tener el fondo de playa.");

            var renderer = background.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.IsNotNull(renderer.sprite, "El fondo debe tener su sprite asignado.");

            var cam = Camera.main;
            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;

            Assert.GreaterOrEqual(renderer.bounds.size.y + 0.01f, height, "El fondo debe cubrir el alto.");
            Assert.GreaterOrEqual(renderer.bounds.size.x + 0.01f, width, "El fondo debe cubrir el ancho.");
        }
    }
}
