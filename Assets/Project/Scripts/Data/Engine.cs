using UnityEngine;

namespace LittleTrawling.Data
{
    public enum EngineTier
    {
        Tier0,
        Tier1,
        Tier2,
        Tier3
    }

    /// <summary>
    /// Editable definition of an engine.
    /// </summary>
    [CreateAssetMenu(fileName = "Engine", menuName = "Fishing/Engine")]
    public class Engine : ScriptableObject
    {
        public string displayName = "New Engine";
        public int cost = 1000;
        public EngineTier tier = EngineTier.Tier0;

        public float maxSpeed = 8f;
        public float acceleration = 3.5f;
        public float deceleration = 0.8f;
        public float turnSpeed = 45f;
        public float angularAcceleration = 80f;
        public float angularDeceleration = 35f;
    }
}