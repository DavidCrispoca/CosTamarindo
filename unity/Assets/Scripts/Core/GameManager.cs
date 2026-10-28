using System;
using System.Collections.Generic;
using UnityEngine;
using CosTamarindo.Gameplay;

namespace CosTamarindo.Core
{
    /// <summary>
    /// Estado autoritativo de la partida mientras se juega offline.
    /// En F5 el puntaje definitivo lo valida el backend; este manager solo
    /// lleva la lógica local (vidas, puntaje, tiempo y conteo por ítem).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        private const string BestScoreKey = "ct.best_score";

        public static GameManager Instance { get; private set; }

        [Header("Reglas")]
        [SerializeField] private int startingLives = 3;
        [SerializeField] private float matchDurationSeconds = 120f;

        public GameState State { get; private set; } = GameState.Boot;
        public int Score { get; private set; }
        public int Lives { get; private set; }
        public float TimeLeft { get; private set; }
        public int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);

        /// <summary>Cuántas veces se atrapó cada ítem en la partida actual.</summary>
        public IReadOnlyDictionary<string, int> CaughtCounts => caughtCounts;

        private readonly Dictionary<string, int> caughtCounts = new Dictionary<string, int>();

        public event Action<int> ScoreChanged;
        public event Action<int> LivesChanged;
        public event Action<float> TimeChanged;
        public event Action<GameState> StateChanged;
        public event Action<ItemDef, bool> ItemCaught;   // (def, esPrimerDesbloqueo)
        public event Action<int> MatchEnded;             // puntaje final

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Lives = startingLives;
            TimeLeft = matchDurationSeconds;

            // Mantiene la simulación activa aunque la ventana pierda el foco
            // (en el editor Unity congela el bucle si esto está en false).
            Application.runInBackground = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            TimeLeft = Mathf.Max(0f, TimeLeft - Time.deltaTime);
            TimeChanged?.Invoke(TimeLeft);

            if (TimeLeft <= 0f) EndMatch();
        }

        public void StartMatch()
        {
            Score = 0;
            Lives = startingLives;
            TimeLeft = matchDurationSeconds;
            caughtCounts.Clear();

            ScoreChanged?.Invoke(Score);
            LivesChanged?.Invoke(Lives);
            TimeChanged?.Invoke(TimeLeft);

            SetState(GameState.Playing);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        /// <summary>Registra la captura de un ítem y aplica sus consecuencias.</summary>
        public void RegisterCatch(ItemDef def)
        {
            if (State != GameState.Playing || def == null) return;

            Score = Mathf.Max(0, Score + def.Points);
            caughtCounts.TryGetValue(def.Id, out int count);
            caughtCounts[def.Id] = count + 1;

            bool firstUnlock = def.Kind != ItemKind.Trash && CollectionBook.Unlock(def.Id);

            ScoreChanged?.Invoke(Score);
            ItemCaught?.Invoke(def, firstUnlock);

            if (def.CostsLife)
            {
                Lives = Mathf.Max(0, Lives - 1);
                LivesChanged?.Invoke(Lives);
                if (Lives == 0)
                {
                    EndMatch();
                    return;
                }
            }

            if (Score > BestScore) PlayerPrefs.SetInt(BestScoreKey, Score);
        }

        public void EndMatch()
        {
            if (State == GameState.GameOver) return;

            Time.timeScale = 1f;
            if (Score > BestScore) PlayerPrefs.SetInt(BestScoreKey, Score);
            PlayerPrefs.Save();

            SetState(GameState.GameOver);
            MatchEnded?.Invoke(Score);
        }

        public void RestartMatch()
        {
            Time.timeScale = 1f;
            StartMatch();
        }

        private void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
