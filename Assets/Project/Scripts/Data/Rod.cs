using UnityEngine;

namespace LittleTrawling.Data
{
    public enum RodTier
    {
        Beginner,
        Enthusiast,
        Professional
    }

    /// <summary>
    /// Editable definition of a catchable fish species.
    /// </summary>
    [CreateAssetMenu(fileName = "Rod", menuName = "Fishing/Rod")]
    public class Rod: ScriptableObject
    {
        [Header("Name")]
        public string displayName = "New Rod";

        [Header("Cost")]
        public int cost = 1000;

        [Header("Tier")]
        [Tooltip("Determines which tier of fish this rod can catch.")]
        public RodTier tier = RodTier.Beginner;
    }
}