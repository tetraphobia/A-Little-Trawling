using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Environment;
using LittleTrawling.Vehicles;
using LittleTrawling.Audio;

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
        [Tooltip("The 3D fishing rod GameObject held in the player's hand.")]
        [SerializeField] private GameObject rodGameObject;

        public GameObject RodGameObject
        {
            get => rodGameObject;
            set => rodGameObject = value;
        }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private float gravity = -20f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Audio SFX")]
        [Tooltip("Sound played for player footsteps while moving.")]
        [SerializeField] private AudioClip footstepSound;
        [Tooltip("Sound played when the player jumps.")]
        [SerializeField] private AudioClip jumpSound;
        [Tooltip("Volume multiplier for the player jump grunt sound effect.")]
        [Range(0f, 1f)]
        [SerializeField] private float jumpSoundVolume = 0.35f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        private static readonly int JumpHash = Animator.StringToHash("Jump");

        private float _lastFootstepTime;
        private AudioSource _sfxAudioSource;

        private void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float minPitch = 1.0f, float maxPitch = 1.0f)
        {
            if (clip == null) return;
            if (_sfxAudioSource == null)
            {
                _sfxAudioSource = GetComponent<AudioSource>();
                if (_sfxAudioSource == null)
                {
                    _sfxAudioSource = gameObject.AddComponent<AudioSource>();
                }
                _sfxAudioSource.spatialBlend = 0f;
            }

            _sfxAudioSource.pitch = (minPitch != 1.0f || maxPitch != 1.0f) ? Random.Range(minPitch, maxPitch) : 1.0f;

            if (VolumeManager.Instance != null)
            {
                VolumeManager.Instance.PlayOneShot(_sfxAudioSource, clip, volumeScale, AudioCategory.SFX);
            }
            else
            {
                _sfxAudioSource.PlayOneShot(clip, volumeScale);
            }
        }

        public static PlayerController Instance { get; private set; }

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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

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

            bool isGrounded = _cc.isGrounded || _isGroundedOnDeck;
            if (isGrounded)
            {
                _verticalVel = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
                _isGroundedOnDeck = false;
                jumpSound = (AudioClip)Resources.Load("jump");
                float jumpVol = VolumeManager.Instance != null ? VolumeManager.Instance.JumpVolume : jumpSoundVolume;
                PlaySFX(jumpSound, jumpVol, 0.90f, 1.10f);

                if (animator != null)
                {
                    animator.SetTrigger(JumpHash);
                }
            }
        }

        private void UpdateRodVisibility()
        {
            if (rodGameObject == null)
            {
                Transform rodChild = transform.Find("FishingRod") ?? transform.Find("Rod");
                if (rodChild == null)
                {
                    foreach (Transform t in GetComponentsInChildren<Transform>(true))
                    {
                        if (t != transform && t.name.ToLower().Contains("rod"))
                        {
                            rodChild = t;
                            break;
                        }
                    }
                }
                if (rodChild != null)
                {
                    rodGameObject = rodChild.gameObject;
                }
            }

            if (rodGameObject == null) return;

            var gm = GameManager.Instance;
            var fm = LittleTrawling.Systems.FishingManager.Instance;
            bool isFishingState = (gm != null && gm.IsState(GameState.Fishing)) || (fm != null && fm.CurrentState != LittleTrawling.Systems.FishingState.Idle);

            if (rodGameObject.activeSelf != isFishingState)
            {
                rodGameObject.SetActive(isFishingState);
            }
        }

        private void Update()
        {
            UpdateRodVisibility();

            if (!_active || InputReader.Instance == null) return;

            Vector3 platformDisplacement = CalculatePlatformDisplacement();
            Vector3 moveDir = CalculateMovementDirection();

            ApplyDeckMeshHuggingAndParenting();

            Vector3 finalMove = (moveDir + Vector3.up * _verticalVel) * Time.deltaTime + platformDisplacement;
            _cc.Move(finalMove);

            bool isGrounded = _cc.isGrounded || _isGroundedOnDeck;
            if (isGrounded && moveDir.sqrMagnitude > 0.05f)
            {
                bool isSprinting = InputReader.Instance != null && InputReader.Instance.SprintHeld;
                float stepInterval = isSprinting ? 0.28f : 0.42f;
                if (Time.time - _lastFootstepTime > stepInterval)
                {
                    _lastFootstepTime = Time.time;
                    footstepSound = (AudioClip)Resources.Load("footsteps");
                    float footstepVol = VolumeManager.Instance != null ? VolumeManager.Instance.FootstepVolume : 0.5f;
                    PlaySFX(footstepSound, footstepVol);
                }
            }

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(new Vector3(moveDir.x, 0f, moveDir.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            // Lock rotation strictly upright relative to world Y-axis (prevent pitch/roll swaying from ocean waves)
            Vector3 currentEuler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);

            UpdateAnimator(moveDir);
        }

        private void UpdateAnimator(Vector3 moveDir)
        {
            if (animator == null) return;

            bool isSprinting = InputReader.Instance != null && InputReader.Instance.SprintHeld;
            float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
            float speedMagnitude = new Vector2(moveDir.x, moveDir.z).magnitude;
            float speedRatio = speedMagnitude / targetSpeed;

            if (isSprinting && speedRatio > 0.1f)
            {
                speedRatio *= 2.0f; // Scale to 2.0 for Blend Trees where 0=Idle, 1=Walk, 2=Sprint
            }

            bool isGrounded = _cc.isGrounded || _isGroundedOnDeck;

            if (isGrounded)
            {
                animator.ResetTrigger(JumpHash);
            }

            animator.SetFloat(SpeedHash, speedRatio, 0.1f, Time.deltaTime);
            animator.SetBool(IsSprintingHash, isSprinting && speedRatio > 0.1f);
            animator.SetBool(IsGroundedHash, isGrounded);
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

                    // Automatically parent to the BoatBob mesh if present, or boat root
                    var boatBob = _boatController.GetComponentInChildren<BoatBob>();
                    Transform targetParent = boatBob != null ? boatBob.transform : _boatController.transform;

                    if (transform.parent != targetParent)
                    {
                        transform.SetParent(targetParent);
                        if (_boatRigidbody != null)
                        {
                            _lastBoatPosition = _boatRigidbody.position;
                            _lastBoatRotation = _boatRigidbody.rotation;
                        }
                    }

                    if (_verticalVel <= 0f)
                    {
                        float deckHeightY = hit.point.y;
                        float heightDiff = deckHeightY - transform.position.y;

                        // Only snap when player is at or slightly above the deck surface.
                        if (heightDiff <= 0.05f && heightDiff >= -0.3f)
                        {
                            float snappedVel = Mathf.Clamp(heightDiff / Time.deltaTime, -4.0f, 0.0f);
                            _verticalVel = snappedVel;
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
            if (!isOnBoat && transform.parent != null && _boatController != null && (transform.parent == _boatController.transform || transform.parent.IsChildOf(_boatController.transform)))
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
                var boatBob = _boatController.GetComponentInChildren<BoatBob>();
                Transform targetParent = boatBob != null ? boatBob.transform : _boatController.transform;

                transform.SetParent(targetParent);
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