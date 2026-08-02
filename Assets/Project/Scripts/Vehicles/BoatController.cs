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

        [Header("Movement Alignment")]
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

        [Header("Audio SFX")]
        [Tooltip("Sound played when entering the boat.")]
        [SerializeField] private AudioClip enterBoatSound;
        [Tooltip("Sound played when exiting the boat.")]
        [SerializeField] private AudioClip exitBoatSound;
        [Tooltip("Sound played when accelerating the boat.")]
        [SerializeField] private AudioClip accelerateSound;

        private Rigidbody _rb;
        private bool _piloting;
        private float _currentSpeed;
        private float _currentAngularVelocity;
        private float _baseY;
        private float _currentYaw;
        private float _lastAccelSoundTime;
        private AudioSource _audioSource;

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

        public float MaxSpeed => engine != null ? engine.maxSpeed : 8f;
        public float Acceleration => engine != null ? engine.acceleration : 3.5f;
        public float Deceleration => engine != null ? engine.deceleration : 0.8f;
        public float TurnSpeed => engine != null ? engine.turnSpeed : 45f;
        public float AngularAcceleration => engine != null ? engine.angularAcceleration : 80f;
        public float AngularDeceleration => engine != null ? engine.angularDeceleration : 35f;

        public static BoatController Instance { get; private set; }

        public Dock CurrentDockZone { get; set; }
        public bool IsDocked { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _currentYaw = transform.eulerAngles.y;
            EnsureOceanController();
            _baseY = transform.position.y;
        }

        private void Start()
        {
            if (autoDockOnStart)
            {
                Dock targetDock = startingDock ?? FindNearestDock();
                if (targetDock != null)
                {
                    DockTo(targetDock);
                }
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.StateChanged += OnStateChanged;
                OnStateChanged(gm.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(GameState state) => _piloting = state == GameState.Piloting;

        public void DockTo(Dock dock)
        {
            if (dock == null) return;

            IsDocked = true;
            _currentSpeed = 0f;
            _currentAngularVelocity = 0f;
            CurrentDockZone = dock;

            Transform targetBerth = dock.Berth;
            if (targetBerth != null)
            {
                _baseY = targetBerth.position.y;
                _currentYaw = targetBerth.eulerAngles.y;

                Vector3 targetPos = targetBerth.position;
                targetPos.y = _baseY;

                Quaternion targetRot = CalculateWaveRotation();
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

        private Dock FindNearestDock()
        {
            var docks = Object.FindObjectsByType<Dock>(FindObjectsSortMode.None);
            Dock closest = null;
            float closestDist = float.MaxValue;

            foreach (var d in docks)
            {
                if (d == null) continue;
                Transform b = d.Berth;
                float dist = Vector3.Distance(transform.position, b != null ? b.position : d.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = d;
                }
            }
            return closest;
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

        private Quaternion CalculateWaveRotation()
        {
            return Quaternion.Euler(0f, _currentYaw, 0f);
        }

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

        public void PlayEnterSound()
        {
            if (enterBoatSound != null)
            {
                EnsureAudioSource();
                _audioSource.PlayOneShot(enterBoatSound);
            }
        }

        public void PlayExitSound()
        {
            if (exitBoatSound != null)
            {
                EnsureAudioSource();
                _audioSource.PlayOneShot(exitBoatSound);
            }
        }

        public void PlayAccelerateSound()
        {
            if (accelerateSound != null && Time.time - _lastAccelSoundTime > 0.45f)
            {
                _lastAccelSoundTime = Time.time;
                EnsureAudioSource();
                _audioSource.PlayOneShot(accelerateSound);
            }
        }

        private void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 0f;
            }
        }

        private void FixedUpdate()
        {
            Vector2 input = _piloting && InputReader.Instance != null ? InputReader.Instance.MoveInput : Vector2.zero;

            if (input.y > 0.05f)
            {
                PlayAccelerateSound();
            }

            // Steering yaw update
            float targetAngularVel = input.x * TurnSpeed;
            float turnRate = Mathf.Abs(input.x) > 0.01f ? AngularAcceleration : AngularDeceleration;
            _currentAngularVelocity = Mathf.MoveTowards(_currentAngularVelocity, targetAngularVel, turnRate * Time.fixedDeltaTime);

            if (Mathf.Abs(_currentAngularVelocity) > 0.001f)
            {
                _currentYaw += _currentAngularVelocity * Time.fixedDeltaTime;
            }

            _rb.MoveRotation(Quaternion.Euler(0f, _currentYaw, 0f));

            // Linear speed update
            float targetSpeed = input.y > 0.01f ? input.y * MaxSpeed : 0f;
            float accelRate = input.y > 0.01f ? Acceleration : Deceleration;

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);
            _currentSpeed = Mathf.Max(0f, _currentSpeed);

            Vector3 nextPos = _rb.position;

            if (Mathf.Abs(_currentSpeed) > 0.0001f)
            {
                Vector3 moveDelta = ForwardDirection * _currentSpeed * Time.fixedDeltaTime;
                if (!WouldHitLand(_rb.position, moveDelta))
                {
                    nextPos += moveDelta;
                }
                else
                {
                    _currentSpeed = 0f;
                }
            }

            nextPos.y = _baseY;

            _rb.MovePosition(nextPos);
        }
    }
}