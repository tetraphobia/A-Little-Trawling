using UnityEngine;
using LittleTrawling.Core;

namespace LittleTrawling.Core
{
    /// <summary>
    /// Third-person camera that orbits when you right-click and drag.
    /// </summary>
    public class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform boatTarget;

        [Header("Walking Profile")]
        [SerializeField] private float walkingDistance = 6f;
        [SerializeField] private Vector3 walkingOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Piloting Profile")]
        [SerializeField] private float pilotingDistance = 14f;
        [SerializeField] private Vector3 pilotingOffset = new Vector3(0f, 3f, 0f);

        [Header("Orbit Settings")]
        [SerializeField] private float mouseSensitivity = 3f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Smoothing")]
        [Tooltip("How quickly the camera position follows the target.")]
        [SerializeField] private float followSmoothTime = 0.1f;
        [Tooltip("How quickly distance/offset lerps when switching states.")]
        [SerializeField] private float transitionSpeed = 5f;

        // Current orbit angles.
        private float _yaw;
        private float _pitch = 20f;

        // Smoothing state.
        private float _currentDistance;
        private Vector3 _currentOffset;
        private Vector3 _smoothVelocity;

        // Active target and profile driven by game state.
        private Transform _activeTarget;
        private float _targetDistance;
        private Vector3 _targetOffset;

        public static ThirdPersonCameraController Instance { get; private set; }

        private bool _isCelebrationOverride;
        private float _celebrationYaw;
        private float _celebrationPitch;
        private float _celebrationDistance;
        private Vector3 _celebrationOffset;

        private float _savedYaw;
        private float _savedPitch;
        private bool _isRestoringFromCelebration;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Default to walking profile.
            _activeTarget = playerTarget;
            _targetDistance = walkingDistance;
            _targetOffset = walkingOffset;
            _currentDistance = _targetDistance;
            _currentOffset = _targetOffset;

            var cam = GetComponent<Camera>() ?? Camera.main;
            if (cam != null && cam.farClipPlane < 500f)
            {
                cam.farClipPlane = 500f;
            }

            // Initialize yaw behind the target.
            if (_activeTarget != null)
            {
                _yaw = _activeTarget.eulerAngles.y;
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
                GameManager.Instance.StateChanged -= OnStateChanged;
            if (Instance == this) Instance = null;
        }

        public void SetCelebrationOverride(bool enabled, float yaw = 0f, float pitch = 5f, float distance = 3.8f, Vector3? offset = null)
        {
            if (enabled)
            {
                if (!_isCelebrationOverride)
                {
                    _savedYaw = _yaw;
                    _savedPitch = _pitch;
                }
                _isCelebrationOverride = true;
                _isRestoringFromCelebration = false;
                _celebrationYaw = yaw;
                _celebrationPitch = pitch;
                _celebrationDistance = distance;
                _celebrationOffset = offset ?? new Vector3(0f, 1.2f, 0f);
            }
            else
            {
                _isCelebrationOverride = false;
                _isRestoringFromCelebration = true;
                OnStateChanged(GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Walking);
            }
        }

        private void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Piloting:
                    _activeTarget = boatTarget;
                    _targetDistance = pilotingDistance;
                    _targetOffset = pilotingOffset;
                    break;

                case GameState.Dialogue:
                    _activeTarget = playerTarget;
                    _targetDistance = 4.2f;
                    _targetOffset = walkingOffset;
                    break;

                default:
                    _activeTarget = playerTarget;
                    _targetDistance = walkingDistance;
                    _targetOffset = walkingOffset;
                    break;
            }
        }

        private void LateUpdate()
        {
            if (_activeTarget == null || InputReader.Instance == null) return;

            if (_isCelebrationOverride)
            {
                _yaw = Mathf.LerpAngle(_yaw, _celebrationYaw, 8f * Time.deltaTime);
                _pitch = Mathf.Lerp(_pitch, _celebrationPitch, 8f * Time.deltaTime);
                _currentDistance = Mathf.Lerp(_currentDistance, _celebrationDistance, 8f * Time.deltaTime);
                _currentOffset = Vector3.Lerp(_currentOffset, _celebrationOffset, 8f * Time.deltaTime);
            }
            else
            {
                if (_isRestoringFromCelebration)
                {
                    _yaw = Mathf.LerpAngle(_yaw, _savedYaw, 6f * Time.deltaTime);
                    _pitch = Mathf.Lerp(_pitch, _savedPitch, 6f * Time.deltaTime);

                    if (Mathf.Abs(Mathf.DeltaAngle(_yaw, _savedYaw)) < 0.5f && Mathf.Abs(_pitch - _savedPitch) < 0.5f)
                    {
                        _isRestoringFromCelebration = false;
                    }
                }

                // Orbit while right click is held
                if (InputReader.Instance.CameraLookHeld)
                {
                    Vector2 look = InputReader.Instance.LookInput;
                    _yaw += look.x * mouseSensitivity;
                    _pitch -= look.y * mouseSensitivity;
                    _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
                    _isRestoringFromCelebration = false;

                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                // Transition between state profiles
                _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, transitionSpeed * Time.deltaTime);
                _currentOffset = Vector3.Lerp(_currentOffset, _targetOffset, transitionSpeed * Time.deltaTime);
            }

            // Compute desired position
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focusPoint = _activeTarget.position + _currentOffset;
            Vector3 desiredPosition = focusPoint - (rotation * Vector3.forward) * _currentDistance;

            // Follow
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _smoothVelocity, followSmoothTime);
            transform.rotation = Quaternion.LookRotation(focusPoint - transform.position);
        }
    }
}
