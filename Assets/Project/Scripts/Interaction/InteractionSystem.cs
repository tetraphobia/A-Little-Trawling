using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LittleTrawling.Core;
using LittleTrawling.UI;

namespace LittleTrawling.Interaction
{
    /// <summary>
    /// Central manager for interaction prompts and execution.
    /// Tracks active interactables in range, listens to input, and renders uGUI prompts in AC:NH style.
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        public static InteractionSystem Instance { get; private set; }

        private readonly List<IInteractable> _activeInteractables = new List<IInteractable>();

        private Canvas _canvas;
        private GameObject _promptRoot;
        private TextMeshProUGUI _promptLabel;

        public IInteractable CurrentInteractable => _activeInteractables.Count > 0 ? _activeInteractables[^1] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildUI();
        }

        private void Start()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed += OnInteractPressed;
            }
        }

        private void OnDestroy()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed -= OnInteractPressed;
            }
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            _canvas = UITheme.CreateScreenCanvas("InteractionSystem_Canvas", 35);
            _canvas.transform.SetParent(transform, false);

            // Border (gold)
            Image border = UITheme.CreatePanel("PromptBorder", _canvas.transform,
                UITheme.BadgeSprite, UITheme.Gold);
            UITheme.AnchorBottomCenter(border.rectTransform, 520f, 64f, 120f);
            _promptRoot = border.gameObject;

            // Background pill (warm white)
            Image bg = UITheme.CreatePanel("PromptBg", border.transform,
                UITheme.BadgeSprite, UITheme.CardWhite);
            UITheme.StretchFill(bg.rectTransform, 3f, 3f, 3f, 3f);

            // Label
            _promptLabel = UITheme.CreateLabel("PromptLabel", bg.transform, "",
                UITheme.TitleFontSize, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.Center);
            _promptLabel.richText = true;
            UITheme.StretchFill(_promptLabel.rectTransform, 16f, 16f, 0f, 0f);

            _promptRoot.SetActive(false);
        }

        public void RegisterInteractable(IInteractable interactable)
        {
            if (interactable != null && !_activeInteractables.Contains(interactable))
            {
                _activeInteractables.Add(interactable);
            }
        }

        public void UnregisterInteractable(IInteractable interactable)
        {
            if (interactable != null)
            {
                _activeInteractables.Remove(interactable);
            }
        }

        private void OnInteractPressed()
        {
            var gm = GameManager.Instance;
            if (gm == null || (!gm.IsState(GameState.Walking) && !gm.IsState(GameState.Piloting))) return;

            var target = CurrentInteractable;
            if (target != null)
            {
                target.Interact();
            }
        }

        private void Update()
        {
            var target = CurrentInteractable;
            var gm = GameManager.Instance;
            bool isWalkingOrPiloting = gm == null || gm.IsState(GameState.Walking) || gm.IsState(GameState.Piloting);

            if (target == null || !isWalkingOrPiloting)
            {
                if (_promptRoot != null && _promptRoot.activeSelf)
                    _promptRoot.SetActive(false);
                return;
            }

            string prompt = target.GetInteractionPrompt();
            if (string.IsNullOrEmpty(prompt))
            {
                if (_promptRoot != null && _promptRoot.activeSelf)
                    _promptRoot.SetActive(false);
                return;
            }

            if (_promptRoot != null)
            {
                if (!_promptRoot.activeSelf) _promptRoot.SetActive(true);
                if (_promptLabel != null) _promptLabel.text = prompt;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureInteractionSystem()
        {
            if (Object.FindAnyObjectByType<InteractionSystem>() == null)
            {
                var obj = new GameObject("InteractionSystem");
                obj.AddComponent<InteractionSystem>();
            }
        }
    }
}
