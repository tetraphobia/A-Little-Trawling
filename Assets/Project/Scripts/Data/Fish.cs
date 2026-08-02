using UnityEngine;

namespace LittleTrawling.Data
{
    public enum FishRarity
    {
        Common,
        Uncommon,
        Rare
    }

    /// <summary>
    /// Editable definition of a catchable fish species.
    /// </summary>
    [CreateAssetMenu(fileName = "Fish", menuName = "Fishing/Fish")]
    public class Fish: ScriptableObject
    {
        [Header("Name")]
        public string displayName = "New Fish";

        [Header("Description")]
        public string description = "New Fish description";

        [Header("Sprite")]
        [Tooltip("2D sprite for the fish.")]
        public Sprite sprite;

        [Header("Rarity")]
        public FishRarity rarity = FishRarity.Common;

        [Header("Size range (meters)")]
        public float minSize = 0.2f;
        public float maxSize = 0.6f;

        [Header("Weight range (kilos)")]
        public float minWeight = 0.12f;
        public float maxWeight = 3.2f;

        [Header("Value")]
        [Tooltip("Base value before the size multiplier is applied.")]
        public int baseValue = 10;

        [Header("Tier")]
        [Tooltip("Lowest rod tier index that can hook this species. 0 = catchable with the starter rod.")]
        public int minRodTier = 0;

        /// <summary>Rolls a random size within this species' range.</summary>
        public float RollSize() => Random.Range(minSize, maxSize);
        /// <summary>Rolls a random weight within this species' range.</summary>
        public float RollWeight() => Random.Range(minWeight, maxWeight);
    }
}