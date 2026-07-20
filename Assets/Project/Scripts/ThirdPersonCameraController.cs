using UnityEngine;
using LittleTrawling.Core;

namespace LittleTrawling.Core
{
    /// <summary>
    /// WoW-style third-person orbit camera. Follows the active target from behind and
    /// orbits when the player holds right-click and drags the mouse.
    /// Switches between walking (player) and piloting (boat) profiles based on game state.
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

        private void Start()
        {
            // Default to walking profile.
            _activeTarget = playerTarget;
            _targetDistance = walkingDistance;
            _targetOffset = walkingOffset;
            _currentDistance = _targetDistance;
            _currentOffset = _targetOffset;

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

            // --- Orbit input (only while right-click is held) ---
            if (InputReader.Instance.CameraLookHeld)
            {
                Vector2 look = InputReader.Instance.LookInput;
                _yaw += look.x * mouseSensitivity;
                _pitch -= look.y * mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // --- Smoothly transition between state profiles ---
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, transitionSpeed * Time.deltaTime);
            _currentOffset = Vector3.Lerp(_currentOffset, _targetOffset, transitionSpeed * Time.deltaTime);

            // --- Compute desired position ---
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focusPoint = _activeTarget.position + _currentOffset;
            Vector3 desiredPosition = focusPoint - (rotation * Vector3.forward) * _currentDistance;

            // --- Smooth follow ---
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _smoothVelocity, followSmoothTime);
            transform.rotation = Quaternion.LookRotation(focusPoint - transform.position);
        }
    }
}
