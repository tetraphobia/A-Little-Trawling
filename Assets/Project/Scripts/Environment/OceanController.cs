using UnityEngine;

namespace LittleTrawling.Environment
{
    /// <summary>
    /// Manages ocean reference position and water level without wave height/bobbing motion.
    /// </summary>
    public class OceanController : MonoBehaviour
    {
        public static OceanController Instance { get; private set; }

        private Vector3 _basePosition;
        private Transform _targetTransform;

        /// <summary>
        /// Gets the current vertical offset (y-displacement) — fixed to 0 (no wave motion).
        /// </summary>
        public float CurrentYOffset => 0f;

        /// <summary>
        /// Gets the current roll tilt angle for boats — fixed to 0.
        /// </summary>
        public float CurrentRoll => 0f;

        /// <summary>
        /// Gets the current pitch tilt angle for boats — fixed to 0.
        /// </summary>
        public float CurrentPitch => 0f;

        /// <summary>
        /// Gets the current world Y position of the ocean surface.
        /// </summary>
        public float CurrentWaterHeight => _basePosition.y;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _targetTransform = transform;
            _basePosition = _targetTransform.position;
        }

        private void Start()
        {
            if (gameObject.name != "Ocean")
            {
                var oceanObj = GameObject.Find("Ocean");
                if (oceanObj != null)
                {
                    _targetTransform = oceanObj.transform;
                    _basePosition = _targetTransform.position;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
