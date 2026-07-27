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

        [Header("Docking")]
        [Tooltip("If true, automatically docks and snaps the boat to the berth on game start.")]
        [SerializeField] private bool autoDockOnStart = true;
        [Tooltip("Optional reference to the starting dock. If unassigned, automatically finds the nearest dock in the scene.")]
        [SerializeField] private Dock startingDock;

        [Header("Land Protection")]
        [Tooltip("Safety radius for checking land collision ahead.")]
        [SerializeField] private float landCollisionRadius = 0.8f;
        [Tooltip("Distance ahead to check for land obstacles.")]
        [SerializeField] private float landCheckDistance = 0.5f;

        private Rigidbody _rb;
        private bool _piloting;
        private float _currentSpeed;
        private float _baseY;
        private float _currentYaw;

        public Vector3 ForwardDirection
        {
            get
            {
                Quaternion yawRot = Quaternion.Euler(0f, _currentYaw, 0f);
                Vector3 dir = yawRot * forwardAxis.normalized;
                dir.y = 0f;
                return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            }
        }

        public Engine Engine
        {
            get => engine;
            set => engine = value;
        }

        public float EffectiveMaxSpeed => maxSpeed * (engine != null ? engine.speedMultiplier : 1f);
        public float EffectiveTurnSpeed => turnSpeed * (engine != null ? engine.maneuverabilityMultiplier : 1f);

        public Dock CurrentDockZone { get; set; }
        public bool IsDocked { get; private set; }

        private bool IsLandCollider(Collider col)
        {
            if (col == null || col.isTrigger) return false;
            if (col.transform == transform || col.transform.IsChildOf(transform)) return false;
            if (col.CompareTag("Player")) return false;
            if (col.name.Contains("Dock") || col.GetComponentInParent<Dock>() != null) return false;

            return true;
        }

        private bool WouldHitLand(Vector3 currentPos, Vector3 moveDelta)
        {
            if (IsDocked || moveDelta.sqrMagnitude < 0.000001f) return false;

            Vector3 origin = currentPos + Vector3.up * 0.5f;
            Vector3 dir = moveDelta.normalized;
            float dist = moveDelta.magnitude + landCheckDistance;

            RaycastHit[] hits = Physics.SphereCastAll(origin, landCollisionRadius, dir, dist);
            foreach (var h in hits)
            {
                if (IsLandCollider(h.collider))
                {
                    return true;
                }
            }
            return false;
        }

        public void DockTo(Dock dock)
        {
            if (dock == null) return;
            IsDocked = true;
            _currentSpeed = 0f;
            CurrentDockZone = dock;
            Transform targetBerth = dock.Berth;
            if (targetBerth != null)
            {
                float oceanOffset = OceanController.Instance != null ? OceanController.Instance.CurrentYOffset : 0f;
                _baseY = targetBerth.position.y - oceanOffset;
                _currentYaw = targetBerth.eulerAngles.y;

                Vector3 targetPos = targetBerth.position;
                targetPos.y = _baseY + oceanOffset;

                float roll = OceanController.Instance != null ? OceanController.Instance.CurrentRoll : 0f;
                float pitch = OceanController.Instance != null ? OceanController.Instance.CurrentPitch : 0f;
                Quaternion targetRot = Quaternion.Euler(pitch, _currentYaw, roll);

                transform.SetPositionAndRotation(targetPos, targetRot);
                if (_rb != null)
                {
                    _rb.position = targetPos;
                    _rb.rotation = targetRot;
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
            _currentYaw = transform.eulerAngles.y;
            EnsureOceanController();
            float oceanOffset = OceanController.Instance != null ? OceanController.Instance.CurrentYOffset : 0f;
            _baseY = transform.position.y - oceanOffset;
        }

        private void EnsureOceanController()
        {
            if (OceanController.Instance == null)
            {
                var oceanObj = GameObject.Find("Ocean");
                if (oceanObj != null && oceanObj.GetComponent<OceanController>() == null)
                {
                    oceanObj.AddComponent<OceanController>();
                }
            }
        }

        private void Start()
        {
            if (autoDockOnStart)
            {
                Dock targetDock = startingDock;
                if (targetDock == null)
                {
                    var docks = Object.FindObjectsByType<Dock>(FindObjectsSortMode.None);
                    float closestDist = float.MaxValue;
                    foreach (var d in docks)
                    {
                        if (d == null) continue;
                        Transform b = d.Berth;
                        float dist = Vector3.Distance(transform.position, b != null ? b.position : d.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            targetDock = d;
                        }
                    }
                }

                if (targetDock != null)
                {
                    DockTo(targetDock);
                }
            }

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

            // Steering yaw
            if (_piloting && Mathf.Abs(input.x) > 0.01f)
            {
                float turn = input.x * EffectiveTurnSpeed * Time.fixedDeltaTime;
                _currentYaw += turn;
            }

            // Apply steering yaw + ocean wave rocking pitch/roll
            float roll = OceanController.Instance != null ? OceanController.Instance.CurrentRoll : 0f;
            float pitch = OceanController.Instance != null ? OceanController.Instance.CurrentPitch : 0f;
            Quaternion targetRotation = Quaternion.Euler(pitch, _currentYaw, roll);
            _rb.MoveRotation(targetRotation);

            // Accelerate / decelerate speed based on input
            float targetSpeed = input.y * EffectiveMaxSpeed;
            float rate = Mathf.Abs(input.y) > 0.01f ? acceleration : deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

            Vector3 nextPos = _rb.position;

            if (Mathf.Abs(_currentSpeed) > 0.0001f)
            {
                Vector3 moveDelta = ForwardDirection * _currentSpeed * Time.fixedDeltaTime;

                // Prevent the boat from driving on land
                if (!WouldHitLand(_rb.position, moveDelta))
                {
                    nextPos += moveDelta;
                }
                else
                {
                    _currentSpeed = 0f;
                }
            }

            // Match ocean bobbing Y position
            float oceanOffset = OceanController.Instance != null ? OceanController.Instance.CurrentYOffset : 0f;
            nextPos.y = _baseY + oceanOffset;

            _rb.MovePosition(nextPos);
        }
    }
}