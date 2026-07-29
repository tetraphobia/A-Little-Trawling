using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Vehicles;

namespace LittleTrawling.Entities
{
    /// <summary>
    /// Handles player avatar movement. Chuck this on the player avatar and make it a child of the boat.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Equipment")]
        [Tooltip("The equipped fishing rod.")]
        [SerializeField] private Rod rod;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
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
        private bool _active;
        private float _verticalVel;
        private Vector3 _lastBoatPosition;

        private int _updateCount;
        private bool _wasGrounded;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            // Track the boat's movement so that the player will move along with it even when not piloting.
            _boatRigidbody = GetComponentInParent<Rigidbody>();
            _boatController = GetComponentInParent<BoatController>();

            Debug.Log($"[PlayerController] Awake. Parent={(transform.parent != null ? transform.parent.name : "null")}, WorldPos={transform.position}, LocalPos={transform.localPosition}, CC={(_cc != null ? "found" : "NULL")}, BoatRB={(_boatRigidbody != null ? _boatRigidbody.name : "NULL")}");
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            Debug.Log($"[PlayerController] Start. GameManager={(gm != null ? "found" : "NULL")}");
            if (gm != null)
            {
                gm.StateChanged += OnStateChanged;
                OnStateChanged(gm.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= OnStateChanged;
        }

        private Quaternion _lastBoatRotation;

        private void OnStateChanged(GameState state)
        {
            _active = state == GameState.Walking;
            _verticalVel = 0f; // Reset gravity accumulator so player never gets pulled down through deck
            if (_active && _boatRigidbody != null)
            {
                _lastBoatPosition = _boatRigidbody.position;
                _lastBoatRotation = _boatRigidbody.rotation;
            }

            // Hide the player avatar while piloting the boat
            bool isVisible = state != GameState.Piloting;
            SetVisibility(isVisible);

            Debug.Log($"[PlayerController] OnStateChanged: New State={state}, Active={_active}, Visible={isVisible}");
        }

        private void SetVisibility(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }

        private void Update()
        {
            if (!_active || InputReader.Instance == null) return;

            Vector3 platformDisplacement = Vector3.zero;
            if (_boatRigidbody != null)
            {
                // Calculate translation delta
                Vector3 boatPosDelta = _boatRigidbody.position - _lastBoatPosition;

                // Calculate rotational displacement around boat center
                Quaternion boatRotDelta = _boatRigidbody.rotation * Quaternion.Inverse(_lastBoatRotation);
                Vector3 localPlayerPos = transform.position - _boatRigidbody.position;
                Vector3 rotatedLocalPos = boatRotDelta * localPlayerPos;
                Vector3 boatRotOffset = rotatedLocalPos - localPlayerPos;

                platformDisplacement = boatPosDelta + boatRotOffset;

                _lastBoatPosition = _boatRigidbody.position;
                _lastBoatRotation = _boatRigidbody.rotation;
            }

            Vector2 input = InputReader.Instance.MoveInput;

            // Convert input into a direction relative to the camera.
            Camera cam = Camera.main;
            Vector3 camFwd = Vector3.forward;
            Vector3 camRight = Vector3.right;
            if (cam != null)
            {
                camFwd = cam.transform.forward;
                camRight = cam.transform.right;
                camFwd.y = 0f;
                camRight.y = 0f;
                camFwd.Normalize();
                camRight.Normalize();
            }

            Vector3 planar = camRight * input.x + camFwd * input.y;

            Vector3 move = planar.sqrMagnitude > 1f ? planar.normalized : planar;
            move *= moveSpeed;

            // Keep grounded on the deck.
            if (_cc.isGrounded && _verticalVel < 0f) _verticalVel = -2f;
            _verticalVel += gravity * Time.deltaTime;

            // Dynamic Mesh Surface Hugging (Active only while sailing / NOT docked)
            bool isDocked = _boatController != null && _boatController.IsDocked;
            if (!isDocked && transform.parent != null && _boatController != null)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 2.0f);
                foreach (var hit in hits)
                {
                    if (hit.collider != null && !hit.collider.isTrigger && hit.collider.transform.IsChildOf(_boatController.transform))
                    {
                        float deckHeightY = hit.point.y;
                        float heightDiff = deckHeightY - transform.position.y;

                        // Smoothly hug the deck mesh surface height when standing on board
                        if (Mathf.Abs(heightDiff) <= 0.6f)
                        {
                            _verticalVel = heightDiff / Time.deltaTime;
                        }
                        break;
                    }
                }
            }

            _cc.Move((move + Vector3.up * _verticalVel) * Time.deltaTime + platformDisplacement);

            // Face the direction of travel.
            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(new Vector3(move.x, 0f, move.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
            }

            // Animations
            if (animator != null)
            {
                float speed = new Vector2(move.x, move.z).magnitude / moveSpeed;
                animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
            }

        }

        /// <summary>
        /// Snaps the avatar to a specific anchor point (i.e. when you'd pilot the boat)
        /// </summary>
        public void SnapTo(Transform anchor)
        {
            Debug.Log($"[PlayerController] SnapTo called! Anchor Pos={anchor.position}, Anchor Rot={anchor.eulerAngles}");
            _cc.enabled = false;
            transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            _cc.enabled = true;
        }
    }
}