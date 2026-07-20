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
        [Tooltip("Forward/backward push.")]
        [SerializeField] private float thrust = 900f;
        [Tooltip("Max speed (m/s).")]
        [SerializeField] private float maxSpeed = 8f;
        [Tooltip("Degrees per second turn speed.")]
        [SerializeField] private float turnSpeed = 55f;
        [Tooltip("Boat only turns above this speed.")]
        [SerializeField] private float minSpeedToTurn = 0.4f;
        [Tooltip("Local axis indicating the forward facing direction of the boat model.")]
        [SerializeField] private Vector3 forwardAxis = Vector3.right;
        [Tooltip("How strongly lateral drift is reduced while moving.")]
        [SerializeField] private float lateralDamping = 3f;

        private Rigidbody _rb;
        private bool _piloting;

        public Vector3 ForwardDirection => transform.TransformDirection(forwardAxis.normalized);

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            // Very simple physics for the prototype.
            _rb.useGravity = false;
            _rb.constraints = RigidbodyConstraints.FreezePositionY
                            | RigidbodyConstraints.FreezeRotationX
                            | RigidbodyConstraints.FreezeRotationZ;

            // Damping settings for coasting when you release the throttle.
            _rb.linearDamping = 1.2f;
            _rb.angularDamping = 4f;
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
            if (!_piloting || InputReader.Instance == null) return;

            Vector2 input = InputReader.Instance.MoveInput;

            Vector3 vel = _rb.linearVelocity;
            Vector3 flat = new Vector3(vel.x, 0f, vel.z);

            Vector3 facing = ForwardDirection;

            // Throttle
            if (Mathf.Abs(input.y) > 0.01f && flat.magnitude < maxSpeed)
                _rb.AddForce(facing * (input.y * thrust), ForceMode.Force);

            // Steering
            if (flat.magnitude > minSpeedToTurn && Mathf.Abs(input.x) > 0.01f)
            {
                float turn = input.x * turnSpeed * Time.fixedDeltaTime;
                _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, turn, 0f));
            }

            // Damp lateral (sideways) velocity relative to boat facing direction so it turns naturally
            if (flat.sqrMagnitude > 0.01f && lateralDamping > 0f)
            {
                Vector3 right = Vector3.Cross(Vector3.up, facing);
                Vector3 lateralVel = Vector3.Project(flat, right);
                _rb.AddForce(-lateralVel * (lateralDamping * Time.fixedDeltaTime), ForceMode.VelocityChange);
            }
        }
    }
}