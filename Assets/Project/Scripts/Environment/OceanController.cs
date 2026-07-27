using UnityEngine;

namespace LittleTrawling.Environment
{
    /// <summary>
    /// Controls the ocean tide and wave motion, causing the ocean GameObject to bob up and down.
    /// Exposes properties for other components (like BoatController) to match the ocean's movement.
    /// </summary>
    public class OceanController : MonoBehaviour
    {
        public static OceanController Instance { get; private set; }

        [Header("Tide / Wave Parameters")]
        [Tooltip("Primary amplitude (vertical displacement in meters) of the ocean bobbing.")]
        [SerializeField] private float primaryAmplitude = 0.35f;

        [Tooltip("Primary frequency (speed) of the ocean bobbing.")]
        [SerializeField] private float primaryFrequency = 1.0f;

        [Tooltip("Secondary amplitude for organic wave variation.")]
        [SerializeField] private float secondaryAmplitude = 0.10f;

        [Tooltip("Secondary frequency for organic wave variation.")]
        [SerializeField] private float secondaryFrequency = 1.6f;

        [Header("Boat Rocking Parameters")]
        [Tooltip("Maximum roll tilt angle (degrees) applied to boats.")]
        [SerializeField] private float rollAmplitude = 2.0f;

        [Tooltip("Maximum pitch tilt angle (degrees) applied to boats.")]
        [SerializeField] private float pitchAmplitude = 1.2f;

        private Vector3 _basePosition;
        private Transform _targetTransform;

        /// <summary>
        /// Gets the current vertical offset (y-displacement) caused by the tide/waves.
        /// </summary>
        public float CurrentYOffset { get; private set; }

        /// <summary>
        /// Gets the current roll tilt angle for rocking boats.
        /// </summary>
        public float CurrentRoll { get; private set; }

        /// <summary>
        /// Gets the current pitch tilt angle for rocking boats.
        /// </summary>
        public float CurrentPitch { get; private set; }

        /// <summary>
        /// Gets the current world Y position of the ocean surface.
        /// </summary>
        public float CurrentWaterHeight => _basePosition.y + CurrentYOffset;

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
            // If this component was instantiated on a manager instead of the Ocean object,
            // locate and target the Ocean GameObject in the scene.
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

        private void Update()
        {
            float time = Time.time;

            // Compute tide/wave vertical displacement
            CurrentYOffset = Mathf.Sin(time * primaryFrequency) * primaryAmplitude
                           + Mathf.Sin(time * secondaryFrequency) * secondaryAmplitude;

            // Compute boat rocking angles
            CurrentRoll = Mathf.Sin(time * (primaryFrequency * 0.8f)) * rollAmplitude;
            CurrentPitch = Mathf.Cos(time * (primaryFrequency * 0.6f)) * pitchAmplitude;

            // Update the Ocean's position to bob up and down
            if (_targetTransform != null)
            {
                _targetTransform.position = new Vector3(_basePosition.x, _basePosition.y + CurrentYOffset, _basePosition.z);
            }
        }
    }
}
