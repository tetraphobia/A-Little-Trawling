using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Environment;
using LittleTrawling.Vehicles;

namespace LittleTrawling.Entities
{
    /// <summary>
    /// Handles third-person player movement, sprinting, jumping, deck surface hugging, dynamic parenting, and platform movement on board the boat.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Equipment")]
        [Tooltip("The equipped fishing rod.")]
        [SerializeField] private Rod rod;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private float gravity = -20f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        public Rod Rod
        {
            get => rod;
            set => rod = value;
        }

        private CharacterController _cc;
        private Rigidbody _boatRigidbody;
        private BoatController _boatController;
        private Camera _mainCamera;
        private Renderer[] _renderers;
        private readonly RaycastHit[] _raycastHits = new RaycastHit[8];

        private bool _active;
        private bool _isGroundedOnDeck;
        private float _verticalVel;
        private Vector3 _lastBoatPosition;
        private Quaternion _lastBoatRotation;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _boatController = GetComponentInParent<BoatController>() ?? Object.FindAnyObjectByType<BoatController>();
            if (_boatController != null)
            {
                _boatRigidbody = _boatController.GetComponent<Rigidbody>();
            }

            _mainCamera = Camera.main;
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.StateChanged += OnStateChanged;
                OnStateChanged(gm.CurrentState);
            }

            if (InputReader.Instance != null)
            {
                InputReader.Instance.JumpPressed += OnJumpPressed;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnStateChanged;
            }

            if (InputReader.Instance != null)
            {
                InputReader.Instance.JumpPressed -= OnJumpPressed;
            }
        }

        private void OnStateChanged(GameState state)
        {
            _active = state == GameState.Walking;
            _verticalVel = 0f;

            if (_active && _boatRigidbody != null)
            {
                _lastBoatPosition = _boatRigidbody.position;
                _lastBoatRotation = _boatRigidbody.rotation;
            }

            SetVisibility(state != GameState.Piloting);
        }

        private void SetVisibility(bool visible)
        {
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }

        private void OnJumpPressed()
        {
            if (!_active) return;

            if (_cc.isGrounded || _isGroundedOnDeck)
            {
                _verticalVel = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
                _isGroundedOnDeck = false;
            }
        }

        private void Update()
        {
            if (!_active || InputReader.Instance == null) return;

            Vector3 platformDisplacement = CalculatePlatformDisplacement();
            Vector3 moveDir = CalculateMovementDirection();

            ApplyDeckMeshHuggingAndParenting();

            _cc.Move((moveDir + Vector3.up * _verticalVel) * Time.deltaTime + platformDisplacement);

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(new Vector3(moveDir.x, 0f, moveDir.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            // Lock rotation strictly upright relative to world Y-axis (prevent pitch/roll swaying from ocean waves)
            Vector3 currentEuler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);

            if (animator != null)
            {
                bool isSprinting = InputReader.Instance != null && InputReader.Instance.SprintHeld;
                float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
                float speedRatio = new Vector2(moveDir.x, moveDir.z).magnitude / targetSpeed;
                if (isSprinting && speedRatio > 0.1f) speedRatio *= 1.5f;
                animator.SetFloat(SpeedHash, speedRatio, 0.1f, Time.deltaTime);
            }
        }

        private Vector3 CalculatePlatformDisplacement()
        {
            if (_boatRigidbody == null || transform.parent == null) return Vector3.zero;

            Vector3 boatPosDelta = _boatRigidbody.position - _lastBoatPosition;
            Quaternion boatRotDelta = _boatRigidbody.rotation * Quaternion.Inverse(_lastBoatRotation);

            Vector3 localPlayerPos = transform.position - _boatRigidbody.position;
            Vector3 rotatedLocalPos = boatRotDelta * localPlayerPos;
            Vector3 boatRotOffset = rotatedLocalPos - localPlayerPos;

            _lastBoatPosition = _boatRigidbody.position;
            _lastBoatRotation = _boatRigidbody.rotation;

            return boatPosDelta + boatRotOffset;
        }

        private Vector3 CalculateMovementDirection()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            Vector3 camFwd = Vector3.forward;
            Vector3 camRight = Vector3.right;

            if (_mainCamera != null)
            {
                camFwd = _mainCamera.transform.forward;
                camRight = _mainCamera.transform.right;
                camFwd.y = 0f;
                camRight.y = 0f;
                camFwd.Normalize();
                camRight.Normalize();
            }

            Vector2 input = InputReader.Instance.MoveInput;
            Vector3 planar = camRight * input.x + camFwd * input.y;
            Vector3 move = planar.sqrMagnitude > 1f ? planar.normalized : planar;

            bool isSprinting = InputReader.Instance != null && InputReader.Instance.SprintHeld;
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

            return move * currentSpeed;
        }

        private void ApplyDeckMeshHuggingAndParenting()
        {
            if (_cc.isGrounded && _verticalVel < 0f)
            {
                _verticalVel = -2f;
                _isGroundedOnDeck = true;
            }

            _verticalVel += gravity * Time.deltaTime;

            if (_boatController == null)
            {
                _boatController = Object.FindAnyObjectByType<BoatController>();
                if (_boatController != null) _boatRigidbody = _boatController.GetComponent<Rigidbody>();
            }

            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, _raycastHits, 3.0f);

            bool isOnBoat = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _raycastHits[i];
                if (hit.collider == null || hit.collider.isTrigger) continue;

                if (_boatController != null && hit.collider.transform.IsChildOf(_boatController.transform))
                {
                    isOnBoat = true;

                    // Ensure player is parented to boat when standing on deck and reset platform trackers
                    if (transform.parent != _boatController.transform)
                    {
                        transform.SetParent(_boatController.transform);
                        if (_boatRigidbody != null)
                        {
                            _lastBoatPosition = _boatRigidbody.position;
                            _lastBoatRotation = _boatRigidbody.rotation;
                        }
                    }

                    // Height snapping: NEVER allow positive upward launch velocity during ground snapping
                    if (_verticalVel <= 0f)
                    {
                        float deckHeightY = hit.point.y;
                        float heightDiff = deckHeightY - transform.position.y;

                        if (Mathf.Abs(heightDiff) <= 0.6f)
                        {
                            // Clamp velocity to downward/grounding only [ -4.0, 0.0 ] to prevent upward launch
                            _verticalVel = Mathf.Clamp(heightDiff / Time.deltaTime, -4.0f, 0.0f);
                            _isGroundedOnDeck = true;
                        }
                    }
                    break;
                }
                else if (hit.collider.name.Contains("Dock") || hit.collider.name.Contains("Land") || hit.collider.name.Contains("Terrain") || hit.collider.GetComponentInParent<Dock>() != null)
                {
                    // Player is standing on land or dock — unparent from boat and reset platform trackers
                    if (transform.parent != null)
                    {
                        transform.SetParent(null);
                    }
                    break;
                }
            }

            // If not hitting boat collider, ensure unparented
            if (!isOnBoat && transform.parent != null && transform.parent == _boatController?.transform)
            {
                var gm = GameManager.Instance;
                if (gm != null && !gm.IsState(GameState.Piloting))
                {
                    transform.SetParent(null);
                }
            }
        }

        /// <summary>
        /// Snaps the avatar to a specific anchor point transform.
        /// </summary>
        public void SnapTo(Transform anchor)
        {
            if (anchor == null) return;

            if (_boatController == null) _boatController = Object.FindAnyObjectByType<BoatController>();
            if (_boatController != null && anchor.IsChildOf(_boatController.transform))
            {
                transform.SetParent(_boatController.transform);
                if (_boatRigidbody != null)
                {
                    _lastBoatPosition = _boatRigidbody.position;
                    _lastBoatRotation = _boatRigidbody.rotation;
                }
            }

            _cc.enabled = false;
            transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            _cc.enabled = true;
        }
    }
}