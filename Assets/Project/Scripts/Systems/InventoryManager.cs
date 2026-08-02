using System;
using System.Collections.Generic;
using UnityEngine;
using LittleTrawling.Data;

namespace LittleTrawling.Systems
{
    /// <summary>
    /// Stores and manages all caught fish in player inventory.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [SerializeField] private List<CaughtFish> items = new List<CaughtFish>();
        private readonly HashSet<string> _discoveredFishNames = new HashSet<string>();

        public IReadOnlyList<CaughtFish> Items => items;
        public int TotalCount => items.Count;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (FishingManager.Instance != null)
            {
                FishingManager.Instance.OnFishCaught += HandleFishCaught;
            }
        }

        private void OnDestroy()
        {
            if (FishingManager.Instance != null)
            {
                FishingManager.Instance.OnFishCaught -= HandleFishCaught;
            }
            if (Instance == this) Instance = null;
        }

        private void HandleFishCaught(Fish species, float sizeCm, float weightKg, int sellPrice)
        {
            AddFish(species, sizeCm, weightKg, sellPrice);
        }

        public void AddFish(Fish species, float sizeCm, float weightKg, int sellPrice)
        {
            var caught = new CaughtFish(species, sizeCm, weightKg, sellPrice);
            items.Add(caught);
            OnInventoryChanged?.Invoke();

            if (species != null)
            {
                bool isNewDiscovery = _discoveredFishNames.Add(species.displayName);
                if (isNewDiscovery && DialogueManager.Instance != null)
                {
                    string dialogueText = $"You caught a {species.displayName}! {species.description}";
                    DialogueManager.Instance.ShowDialogue("Notice", new string[] { dialogueText }, null, new Color(0.2f, 0.85f, 0.4f));
                }
            }
        }

        public bool RemoveFish(CaughtFish item)
        {
            bool removed = items.Remove(item);
            if (removed) OnInventoryChanged?.Invoke();
            return removed;
        }

        public void ClearInventory()
        {
            items.Clear();
            OnInventoryChanged?.Invoke();
        }

        public int CalculateTotalValue()
        {
            int total = 0;
            foreach (var item in items)
            {
                if (item != null) total += item.sellPrice;
            }
            return total;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureInventoryManager()
        {
            if (UnityEngine.Object.FindAnyObjectByType<InventoryManager>() == null)
            {
                var obj = new GameObject("InventoryManager");
                obj.AddComponent<InventoryManager>();
            }
        }
    }
}
