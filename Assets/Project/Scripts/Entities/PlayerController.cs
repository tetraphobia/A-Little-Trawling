using UnityEngine;
using LittleTrawling.Core;

namespace LittleTrawling.Entities
{
    /// <summary>
    /// Handles player avatar movement. Chuck this on the player avatar and make it a child of the boat.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private float gravity = -20f;

        [Header("Deck Boundaries")]
        [Tooltip("If true, clamps the player's position to stay on the boat deck.")]
        [SerializeField] private bool restrictToDeck = true;
        [Tooltip("Local X bounds (min, max) relative to the parent.")]
        [SerializeField] private Vector2 deckBoundsX = new Vector2(-1.1f, 1.1f);
        [Tooltip("Local Z bounds (min, max) relative to the parent.")]
        [SerializeField] private Vector2 deckBoundsZ = new Vector2(-0.6f, 0.6f);

        private CharacterController _cc;
        private bool _active;
        private float _verticalVel;

        private void Awake() => _cc = GetComponent<CharacterController>();

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

        private void OnStateChanged(GameState state) => _active = state == GameState.Walking;

        private void Update()
        {
            if (!_active || InputReader.Instance == null) return;

            Vector2 input = InputReader.Instance.MoveInput;

            // Convert stick input into a direction relative to the boat.
            Vector3 local = new Vector3(input.x, 0f, input.y);
            Vector3 planar = transform.parent != null
                ? transform.parent.TransformDirection(local)
                : local;
            planar.y = 0f;

            Vector3 move = planar.sqrMagnitude > 1f ? planar.normalized : planar;
            move *= moveSpeed;

            // Keep grounded on the deck.
            if (_cc.isGrounded && _verticalVel < 0f) _verticalVel = -2f;
            _verticalVel += gravity * Time.deltaTime;

            _cc.Move((move + Vector3.up * _verticalVel) * Time.deltaTime);

            // Clamp local position so player remains on top of the boat deck
            if (restrictToDeck && transform.parent != null)
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