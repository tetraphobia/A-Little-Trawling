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
        public bool FishHeld { get; private set; }
        public bool CameraLookHeld { get; private set; }
        public bool SprintHeld { get; private set; }

        public event Action FishPressed;
        public event Action FishReleased;
        public event Action InteractPressed;
        public event Action JumpPressed;
        public event Action InventoryPressed;

        private InputAction _fishAction;
        private InputAction _inventoryAction;

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
            _input.Gameplay.Interact.performed     += OnInteract;

            TryBindOptionalActions();
        }

        private void TryBindOptionalActions()
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

                _fishAction = _input.Gameplay.Get().FindAction("Fish");
                if (_fishAction != null)
                {
                    _fishAction.performed += OnFishDown;
                    _fishAction.canceled  += OnFishUp;
                }

                _inventoryAction = _input.Gameplay.Get().FindAction("Inventory");
                if (_inventoryAction != null)
                {
                    _inventoryAction.performed += OnInventoryDown;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InputReader] Optional action binding info: {ex.Message}");
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

                    if (_fishAction != null)
                    {
                        _fishAction.performed -= OnFishDown;
                        _fishAction.canceled  -= OnFishUp;
                    }

                    if (_inventoryAction != null)
                    {
                        _inventoryAction.performed -= OnInventoryDown;
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
        private void OnInventoryDown(InputAction.CallbackContext ctx) => InventoryPressed?.Invoke();

        private void OnFishDown(InputAction.CallbackContext ctx)
        {
            FishHeld = true;
            FishPressed?.Invoke();
        }

        private void OnFishUp(InputAction.CallbackContext ctx)
        {
            FishHeld = false;
            FishReleased?.Invoke();
        }

        private void OnInteract(InputAction.CallbackContext ctx) => InteractPressed?.Invoke();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}