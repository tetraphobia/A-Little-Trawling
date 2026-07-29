using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleTrawling.Core
{
    /// <summary>
    /// API for gameplay scripts to subscribe to input events.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        public static InputReader Instance { get; private set; }

        private GameInput _input;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool CastHeld { get; private set; }
        public bool CameraLookHeld { get; private set; }
        public bool SprintHeld { get; private set; }

        public event Action CastPressed;
        public event Action CastReleased;
        public event Action InteractPressed;
        public event Action JumpPressed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _input = new GameInput();
        }

        private void OnEnable()
        {
            _input ??= new GameInput();
            _input.Gameplay.Enable();

            _input.Gameplay.Move.performed        += OnMove;
            _input.Gameplay.Move.canceled          += OnMove;
            _input.Gameplay.Look.performed         += OnLook;
            _input.Gameplay.Look.canceled          += OnLook;
            _input.Gameplay.CameraLook.performed   += OnCameraLookDown;
            _input.Gameplay.CameraLook.canceled    += OnCameraLookUp;
            _input.Gameplay.Cast.performed         += OnCastDown;
            _input.Gameplay.Cast.canceled          += OnCastUp;
            _input.Gameplay.Interact.performed     += OnInteract;

            TryBindSprintAndJump();
        }

        private void TryBindSprintAndJump()
        {
            try
            {
                var sprintAction = _input.Gameplay.Get().FindAction("Sprint");
                if (sprintAction != null)
                {
                    sprintAction.performed += OnSprintDown;
                    sprintAction.canceled  += OnSprintUp;
                }

                var jumpAction = _input.Gameplay.Get().FindAction("Jump");
                if (jumpAction != null)
                {
                    jumpAction.performed += OnJumpDown;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InputReader] Sprint/Jump binding info: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.Gameplay.Move.performed        -= OnMove;
                _input.Gameplay.Move.canceled          -= OnMove;
                _input.Gameplay.Look.performed         -= OnLook;
                _input.Gameplay.Look.canceled          -= OnLook;
                _input.Gameplay.CameraLook.performed   -= OnCameraLookDown;
                _input.Gameplay.CameraLook.canceled    -= OnCameraLookUp;
                _input.Gameplay.Cast.performed         -= OnCastDown;
                _input.Gameplay.Cast.canceled          -= OnCastUp;
                _input.Gameplay.Interact.performed     -= OnInteract;

                try
                {
                    var sprintAction = _input.Gameplay.Get().FindAction("Sprint");
                    if (sprintAction != null)
                    {
                        sprintAction.performed -= OnSprintDown;
                        sprintAction.canceled  -= OnSprintUp;
                    }

                    var jumpAction = _input.Gameplay.Get().FindAction("Jump");
                    if (jumpAction != null)
                    {
                        jumpAction.performed -= OnJumpDown;
                    }
                }
                catch { }

                _input.Gameplay.Disable();
            }
        }

        private void OnMove(InputAction.CallbackContext ctx) => MoveInput = ctx.ReadValue<Vector2>();
        private void OnLook(InputAction.CallbackContext ctx) => LookInput = ctx.ReadValue<Vector2>();

        private void OnCameraLookDown(InputAction.CallbackContext ctx) => CameraLookHeld = true;
        private void OnCameraLookUp(InputAction.CallbackContext ctx) => CameraLookHeld = false;

        private void OnSprintDown(InputAction.CallbackContext ctx) => SprintHeld = true;
        private void OnSprintUp(InputAction.CallbackContext ctx) => SprintHeld = false;
        private void OnJumpDown(InputAction.CallbackContext ctx) => JumpPressed?.Invoke();

        private void OnCastDown(InputAction.CallbackContext ctx)
        {
            CastHeld = true;
            CastPressed?.Invoke();
        }

        private void OnCastUp(InputAction.CallbackContext ctx)
        {
            CastHeld = false;
            CastReleased?.Invoke();
        }

        private void OnInteract(InputAction.CallbackContext ctx) => InteractPressed?.Invoke();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}