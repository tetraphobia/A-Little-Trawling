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

        private void HandleFishCaught(Fish species, float sizeCm, float weightKg, int sellPrice, LunkerStatus lunkerStatus)
        {
            AddFish(species, sizeCm, weightKg, sellPrice, lunkerStatus);
        }

        public void AddFish(Fish species, float sizeCm, float weightKg, int sellPrice, LunkerStatus lunkerStatus = LunkerStatus.Normal)
        {
            var caught = new CaughtFish(species, sizeCm, weightKg, sellPrice, lunkerStatus);
            items.Add(caught);
            OnInventoryChanged?.Invoke();

            if (species != null)
            {
                bool isFirstTime = !_discoveredFishNames.Contains(species.displayName);
                _discoveredFishNames.Add(species.displayName);

                if (isFirstTime || lunkerStatus != LunkerStatus.Normal)
                {
                    string nameText = species.displayName;
                    if (lunkerStatus == LunkerStatus.MegaLunker)
                        nameText = $"<color=#FFD700>MEGA LUNKER! {species.displayName}</color>";
                    else if (lunkerStatus == LunkerStatus.Lunker)
                        nameText = $"<color=#EE5D5D>LUNKER! {species.displayName}</color>";

                    string speakerTitle = lunkerStatus switch
                    {
                        LunkerStatus.MegaLunker => "MEGA LUNKER!",
                        LunkerStatus.Lunker => "LUNKER CATCH!",
                        _ => "First Catch!"
                    };

                    string descText = lunkerStatus switch
                    {
                        LunkerStatus.MegaLunker => "HOLY COW! You hooked a legendary MEGA LUNKER! It's colossal and worth 6x value!",
                        LunkerStatus.Lunker => "WOW! You landed a giant LUNKER! It's 3x size and value!",
                        _ => !string.IsNullOrEmpty(species.description) ? species.description : "A fine new addition to your fish inventory!"
                    };

                    string[] dialogueLines = new string[]
                    {
                        $"Caught a <b>{nameText}</b>!",
                        descText
                    };

                    if (FishCatchCelebrationSequence.Instance != null)
                    {
                        FishCatchCelebrationSequence.Instance.PlayCelebration(species, lunkerStatus, speakerTitle, dialogueLines);
                    }
                    else if (DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.ShowDialogue(speakerTitle, dialogueLines);
                    }
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
