using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Environment;

namespace LittleTrawling.Vehicles
{
    /// <summary>
    /// Responsible for piloting the boat while in the Piloting GameState.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoatController : MonoBehaviour
    {
        [Header("Engine")]
        [Tooltip("The equipped engine.")]
        [SerializeField] private Engine engine;

        [Header("Movement")]
        [Tooltip("Max speed (m/s).")]
        [SerializeField] private float maxSpeed = 8f;
        [Tooltip("Acceleration rate (m/s²).")]
        [SerializeField] private float acceleration = 4f;
        [Tooltip("Deceleration rate when releasing throttle (m/s²).")]
        [SerializeField] private float deceleration = 3f;
        [Tooltip("Degrees per second turn speed.")]
        [SerializeField] private float turnSpeed = 55f;
        [Tooltip("Local axis indicating the forward facing direction of the boat model.")]
        [SerializeField] private Vector3 forwardAxis = Vector3.right;

        private Rigidbody _rb;
        private bool _piloting;
        private float _currentSpeed;

        public Vector3 ForwardDirection => transform.TransformDirection(forwardAxis.normalized);

        public Engine Engine
        {
            get => engine;
            set => engine = value;
        }

        public float EffectiveMaxSpeed => maxSpeed * (engine != null ? engine.speedMultiplier : 1f);
        public float EffectiveTurnSpeed => turnSpeed * (engine != null ? engine.maneuverabilityMultiplier : 1f);

        public Dock CurrentDockZone { get; set; }
        public bool IsDocked { get; private set; }

        public void DockTo(Dock dock)
        {
            if (dock == null) return;
            IsDocked = true;
            _currentSpeed = 0f;
            Transform targetBerth = dock.Berth;
            if (targetBerth != null)
            {
                transform.SetPositionAndRotation(targetBerth.position, targetBerth.rotation);
                if (_rb != null)
                {
                    _rb.position = targetBerth.position;
                    _rb.rotation = targetBerth.rotation;
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }
                Physics.SyncTransforms();
            }
        }

        public void Undock()
        {
            IsDocked = false;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                // Register this callback to receive events when the game state changes.
                gm.StateChanged += OnStateChanged;
                OnStateChanged(gm.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                // Unregister the callback.
                GameManager.Instance.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state) => _piloting = state == GameState.Piloting;

        private void FixedUpdate()
        {
            Vector2 input = _piloting && InputReader.Instance != null ? InputReader.Instance.MoveInput : Vector2.zero;

            // Steering
            if (_piloting && Mathf.Abs(input.x) > 0.01f)
            {
                float turn = input.x * EffectiveTurnSpeed * Time.fixedDeltaTime;
                _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, turn, 0f));
            }

            // Accelerate gradually
            float targetSpeed = input.y * EffectiveMaxSpeed;
            float rate = Mathf.Abs(input.y) > 0.01f ? acceleration : deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

            if (Mathf.Abs(_currentSpeed) > 0.0001f)
                _rb.MovePosition(_rb.position + ForwardDirection * _currentSpeed * Time.fixedDeltaTime);
        }
    }
}