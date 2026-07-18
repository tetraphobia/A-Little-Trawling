using System;
using UnityEngine;

namespace LittleTrawling.Core
{
    /// <summary>
    /// Exposes an API to subscribe to game state changes and query the current game state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameState startingState = GameState.Walking;

        public GameState CurrentState { get; private set; }

        /// <summary>Raised whenever the state changes. Argument is the new state.</summary>
        public event Action<GameState> StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CurrentState = startingState;
        }

        private void Start()
        {
            // Broadcast once on boot so listeners can sync to the starting state.
            StateChanged?.Invoke(CurrentState);
        }

        public void SetState(GameState newState)
        {
            if (newState == CurrentState) return;
            CurrentState = newState;
            StateChanged?.Invoke(newState);
        }

        public bool IsState(GameState state) => CurrentState == state;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}