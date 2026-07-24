using UnityEngine;

namespace LittleTrawling.Data
{
    public enum EngineTier
    {
        Shoddy,
        Improved,
        Premium
    }

    /// <summary>
    /// Editable definition of an engine.
    /// </summary>
    [CreateAssetMenu(fileName = "Engine", menuName = "Fishing/Engine")]
    public class Engine : ScriptableObject
    {
        [Header("Name")]
        public string displayName = "New Engine";
        
        [Header("Cost")]
        public int cost = 1000;

        [Header("Engine Tier")]
        public EngineTier tier = EngineTier.Shoddy;

        [Header("Speed")]
        [Tooltip("Speed multiplier.")]
        public float speedMultiplier = 1f;

        [Header("Maneuverability")]
        [Tooltip("How well the boat turns.")]
        public float maneuverabilityMultiplier = 1f;
    }
}