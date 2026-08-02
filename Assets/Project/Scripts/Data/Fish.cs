using UnityEngine;

namespace LittleTrawling.Data
{
    public enum FishTier
    {
        Tier0,
        Tier1,
        Tier2,
        Tier3
    }

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
    public class Fish : ScriptableObject
    {
        public string displayName = "New Fish";
        public string description = "New Fish description";

        public Sprite sprite;
        public AudioClip catchSound;

        public FishRarity rarity = FishRarity.Common;
        public FishTier tier = FishTier.Tier0;

        public float minSize = 0.2f;
        public float maxSize = 0.6f;

        public float minWeight = 0.12f;
        public float maxWeight = 3.2f;

        public int baseValue = 10;

        public float RollSize() => Random.Range(minSize, maxSize);
        public float RollWeight() => Random.Range(minWeight, maxWeight);
    }
}