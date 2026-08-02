using UnityEngine;

namespace LittleTrawling.Data
{
    public enum RodTier
    {
        Tier0,
        Tier1,
        Tier2,
        Tier3
    }

    /// <summary>
    /// Editable definition of a fishing rod.
    /// </summary>
    [CreateAssetMenu(fileName = "Rod", menuName = "Fishing/Rod")]
    public class Rod : ScriptableObject
    {
        public string displayName = "New Rod";
        public int cost = 1000;
        public RodTier tier = RodTier.Tier0;
    }
}