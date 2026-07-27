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

        [Tooltip("Acceleration rate when applying throttle (m/s²).")]
        public float acceleration = 3.5f;

        [Tooltip("Gliding deceleration rate when releasing throttle (m/s²). Lower values create longer water gliding.")]
        public float deceleration = 0.8f;

        [Tooltip("Max turn speed (deg/s).")]
        public float turnSpeed = 45f;

        [Tooltip("Angular acceleration when steering into a turn (deg/s²).")]
        public float angularAcceleration = 80f;

        [Tooltip("Angular deceleration/inertia when releasing steering (deg/s²). Lower values create smoother turn gliding.")]
        public float angularDeceleration = 35f;
    }
}