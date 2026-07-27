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

        [Header("Movement Properties")]
        [Tooltip("Max speed (m/s).")]
        public float maxSpeed = 8f;

        [Tooltip("Acceleration rate (m/s²).")]
        public float acceleration = 4f;

        [Tooltip("Deceleration rate when releasing throttle (m/s²).")]
        public float deceleration = 3f;

        [Tooltip("Degrees per second turn speed.")]
        public float turnSpeed = 55f;
    }
}