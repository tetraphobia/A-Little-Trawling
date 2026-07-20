using UnityEngine;
using LittleTrawling.Core;

namespace LittleTrawling.Vehicles
{
    /// <summary>
    /// Responsible for piloting the boat while in the Piloting GameState.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoatController : MonoBehaviour
    {
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
                float turn = input.x * turnSpeed * Time.fixedDeltaTime;
                _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, turn, 0f));
            }

            // Accelerate gradually
            float targetSpeed = input.y * maxSpeed;
            float rate = Mathf.Abs(input.y) > 0.01f ? acceleration : deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

            if (Mathf.Abs(_currentSpeed) > 0.0001f)
                _rb.MovePosition(_rb.position + ForwardDirection * _currentSpeed * Time.fixedDeltaTime);
        }
    }
}