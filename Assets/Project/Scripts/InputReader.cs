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
        public bool CastHeld { get; private set; }

        public event Action CastPressed;
        public event Action CastReleased;
        public event Action InteractPressed;

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

            _input.Gameplay.Move.performed     += OnMove;
            _input.Gameplay.Move.canceled      += OnMove;
            _input.Gameplay.Cast.performed     += OnCastDown;
            _input.Gameplay.Cast.canceled      += OnCastUp;
            _input.Gameplay.Interact.performed += OnInteract;
        }

        private void OnDisable()
        {
            _input.Gameplay.Move.performed     -= OnMove;
            _input.Gameplay.Move.canceled      -= OnMove;
            _input.Gameplay.Cast.performed     -= OnCastDown;
            _input.Gameplay.Cast.canceled      -= OnCastUp;
            _input.Gameplay.Interact.performed -= OnInteract;

            _input.Gameplay.Disable();
        }

        private void OnMove(InputAction.CallbackContext ctx) => MoveInput = ctx.ReadValue<Vector2>();

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