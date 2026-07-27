using System;
using UnityEngine;

namespace LittleTrawling.Core
{
    /// <summary>
    /// Tracks player Gold currency for buying upgrades and selling catches.
    /// </summary>
    public class PlayerWallet : MonoBehaviour
    {
        public static PlayerWallet Instance { get; private set; }

        [Header("Starting Balance")]
        [SerializeField] private int startingGold = 1500;

        public int CurrentGold { get; private set; }

        /// <summary>
        /// Raised whenever the player's gold balance changes. Passed argument is the new total.
        /// </summary>
        public event Action<int> GoldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CurrentGold = startingGold;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            CurrentGold += amount;
            GoldChanged?.Invoke(CurrentGold);
        }

        public bool CanAfford(int amount)
        {
            return CurrentGold >= amount;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (!CanAfford(amount)) return false;

            CurrentGold -= amount;
            GoldChanged?.Invoke(CurrentGold);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsurePlayerWallet()
        {
            if (UnityEngine.Object.FindAnyObjectByType<PlayerWallet>() == null)
            {
                var walletObj = new GameObject("PlayerWallet");
                walletObj.AddComponent<PlayerWallet>();
            }
        }
    }
}
