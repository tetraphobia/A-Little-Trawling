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

        [Header("Deck Boundaries")]
        [Tooltip("If true, clamps the player's position to stay on the boat deck when not docked.")]
        [SerializeField] private bool restrictToDeck = true;
        [Tooltip("Local X bounds (min, max) relative to the parent.")]
        [SerializeField] private Vector2 deckBoundsX = new Vector2(-1.1f, 1.1f);
        [Tooltip("Local Z bounds (min, max) relative to the parent.")]
        [SerializeField] private Vector2 deckBoundsZ = new Vector2(-0.6f, 0.6f);

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

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            // Track the boat's movement so that the player will move along with it even when not piloting.
            _boatRigidbody = GetComponentInParent<Rigidbody>();
            _boatController = GetComponentInParent<BoatController>();
        }

        private void Start()
        {
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
                GameManager.Instance.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            _active = state == GameState.Walking;
            _verticalVel = 0f; // Reset gravity accumulator so player never gets pulled down through deck
            if (_active && _boatRigidbody != null)
                _lastBoatPosition = _boatRigidbody.position;
        }

        private void Update()
        {
            if (!_active || InputReader.Instance == null) return;

            Vector3 boatDelta = Vector3.zero;
            if (_boatRigidbody != null)
            {
                boatDelta = _boatRigidbody.position - _lastBoatPosition;
                _lastBoatPosition = _boatRigidbody.position;
            }

            Vector2 input = InputReader.Instance.MoveInput;

            // Convert input into a direction relative to the camera.
            Camera cam = Camera.main;
            Vector3 camFwd = cam.transform.forward;
            Vector3 camRight = cam.transform.right;
            camFwd.y = 0f;
            camRight.y = 0f;
            camFwd.Normalize();
            camRight.Normalize();

            Vector3 planar = camRight * input.x + camFwd * input.y;

            Vector3 move = planar.sqrMagnitude > 1f ? planar.normalized : planar;
            move *= moveSpeed;

            // Keep grounded on the deck.
            if (_cc.isGrounded && _verticalVel < 0f) _verticalVel = -2f;
            _verticalVel += gravity * Time.deltaTime;

            _cc.Move((move + Vector3.up * _verticalVel) * Time.deltaTime + boatDelta);

            // Clamp local position so player remains on top of the boat deck only when NOT docked
            bool isDocked = _boatController != null && _boatController.IsDocked;
            if (restrictToDeck && !isDocked && transform.parent != null)
            {
                Vector3 localPos = transform.localPosition;
                float clampedX = Mathf.Clamp(localPos.x, deckBoundsX.x, deckBoundsX.y);
                float clampedZ = Mathf.Clamp(localPos.z, deckBoundsZ.x, deckBoundsZ.y);
                if (localPos.x != clampedX || localPos.z != clampedZ)
                {
                    transform.localPosition = new Vector3(clampedX, localPos.y, clampedZ);
                }
            }

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
            _cc.enabled = false;
            transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            _cc.enabled = true;
        }
    }
}