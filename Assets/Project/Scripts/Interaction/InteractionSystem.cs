using System.Collections.Generic;
using UnityEngine;
using LittleTrawling.Core;

namespace LittleTrawling.Interaction
{
    /// <summary>
    /// Central manager for interaction prompts and execution.
    /// Tracks active interactables in range, listens to input, and renders UI prompts.
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        public static InteractionSystem Instance { get; private set; }

        private readonly List<IInteractable> _activeInteractables = new List<IInteractable>();

        private static GUIStyle _boxStyle;
        private static GUIStyle _labelStyle;

        public IInteractable CurrentInteractable => _activeInteractables.Count > 0 ? _activeInteractables[^1] : null;

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
            if (Instance == this) Instance = null;
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

        private static Texture2D _bgTexture;
        private static Texture2D _borderTexture;
        private static GUIStyle _textStyle;

        private void OnGUI()
        {
            var target = CurrentInteractable;
            if (target == null) return;

            var gm = GameManager.Instance;
            if (gm != null && !gm.IsState(GameState.Walking)) return;

            string prompt = target.GetInteractionPrompt();
            if (string.IsNullOrEmpty(prompt)) return;

            InitPromptResources();
            GUI.depth = -9999;

            int width = 460;
            int height = 54;
            int x = (Screen.width - width) / 2;
            int y = (int)(Screen.height * 0.70f);

            Rect outerRect = new Rect(x - 3, y - 3, width + 6, height + 6);
            Rect innerRect = new Rect(x, y, width, height);

            // Draw crisp gold border outline
            GUI.DrawTexture(outerRect, _borderTexture);

            // Draw solid dark background card
            GUI.DrawTexture(innerRect, _bgTexture);

            // Draw bright yellow prompt text
            string formattedPrompt = $"<size=18><b>{prompt}</b></size>";
            GUI.Label(innerRect, formattedPrompt, _textStyle);
        }

        private static Texture2D MakeSolidTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(w, h);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private static void InitPromptResources()
        {
            if (_bgTexture == null)
            {
                _bgTexture = MakeSolidTex(1, 1, new Color(0.06f, 0.08f, 0.12f, 0.95f));
            }
            if (_borderTexture == null)
            {
                _borderTexture = MakeSolidTex(1, 1, new Color(0.95f, 0.75f, 0.20f, 1.0f));
            }
            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
                _textStyle.normal.textColor = Color.yellow;
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
