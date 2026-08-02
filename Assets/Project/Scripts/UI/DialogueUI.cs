using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LittleTrawling.Core;
using LittleTrawling.Systems;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Renders Animal Crossing–style bottom-of-screen dialogue box using uGUI.
    /// Warm white rounded panel with gold border, speaker name badge, typewriter text,
    /// and a pulsing continuation prompt.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        private Canvas _canvas;
        private GameObject _dialogueRoot;
        private TextMeshProUGUI _bodyText;
        private TextMeshProUGUI _promptText;
        private Image _badgeImage;
        private TextMeshProUGUI _badgeLabel;

        private bool _isShowing;

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

        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            _canvas = UITheme.CreateScreenCanvas("DialogueUI_Canvas", 100);
            _canvas.transform.SetParent(transform, false);

            // ── Dialogue card container ──

            // Gold outer border
            Image border = UITheme.CreatePanel("DialogueBorder", _canvas.transform,
                UITheme.PanelSprite, UITheme.Gold);
            RectTransform borderRt = border.rectTransform;
            borderRt.anchorMin = new Vector2(0.5f, 0);
            borderRt.anchorMax = new Vector2(0.5f, 0);
            borderRt.pivot = new Vector2(0.5f, 0);
            borderRt.sizeDelta = new Vector2(840f, 190f);
            borderRt.anchoredPosition = new Vector2(0, 36f);
            _dialogueRoot = border.gameObject;

            // Warm white inner card
            Image card = UITheme.CreatePanel("DialogueCard", border.transform,
                UITheme.PanelSprite, UITheme.CardWhite);
            UITheme.StretchFill(card.rectTransform, 4f, 4f, 4f, 4f);

            // ── Speaker Name Badge (floating above top-left) ──
            Image badgeBorder = UITheme.CreatePanel("BadgeBorder", border.transform,
                UITheme.BadgeSprite, UITheme.Gold);
            RectTransform badgeBorderRt = badgeBorder.rectTransform;
            badgeBorderRt.anchorMin = new Vector2(0, 1);
            badgeBorderRt.anchorMax = new Vector2(0, 1);
            badgeBorderRt.pivot = new Vector2(0, 0);
            badgeBorderRt.sizeDelta = new Vector2(200f, 46f);
            badgeBorderRt.anchoredPosition = new Vector2(24f, 4f);

            _badgeImage = UITheme.CreatePanel("BadgeFill", badgeBorder.transform,
                UITheme.BadgeSprite, UITheme.Gold);
            UITheme.StretchFill(_badgeImage.rectTransform, 2f, 2f, 2f, 2f);

            _badgeLabel = UITheme.CreateLabel("BadgeLabel", _badgeImage.transform, "Speaker",
                UITheme.BodyFontSize, UITheme.TextWhite, FontStyles.Bold, TextAlignmentOptions.Center);
            UITheme.StretchFill(_badgeLabel.rectTransform);

            // ── Body text ──
            _bodyText = UITheme.CreateLabel("BodyText", card.transform, "",
                24f, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            _bodyText.textWrappingMode = TextWrappingModes.Normal;
            _bodyText.overflowMode = TextOverflowModes.Overflow;
            RectTransform bodyRt = _bodyText.rectTransform;
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(28f, 16f);
            bodyRt.offsetMax = new Vector2(-28f, -16f);

            // ── Continuation prompt ──
            _promptText = UITheme.CreateLabel("PromptText", card.transform, "▼ Press [E]",
                UITheme.SmallFontSize, UITheme.TextGold, FontStyles.Bold, TextAlignmentOptions.BottomRight);
            RectTransform promptRt = _promptText.rectTransform;
            promptRt.anchorMin = new Vector2(1, 0);
            promptRt.anchorMax = new Vector2(1, 0);
            promptRt.pivot = new Vector2(1, 0);
            promptRt.sizeDelta = new Vector2(150f, 30f);
            promptRt.anchoredPosition = new Vector2(-16f, 8f);
            _promptText.gameObject.SetActive(false);

            // Hide initially
            _dialogueRoot.SetActive(false);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool shouldShow = gm.IsState(GameState.Dialogue);
            var mgr = DialogueManager.Instance;
            if (mgr == null || !mgr.IsActive) shouldShow = false;

            if (shouldShow != _isShowing)
            {
                _isShowing = shouldShow;
                _dialogueRoot.SetActive(_isShowing);
            }

            if (!_isShowing) return;

            var session = mgr.CurrentSession;
            if (session == null) return;

            // Update speaker badge
            _badgeLabel.text = session.speakerName;
            _badgeImage.color = session.speakerColor;

            // Resize badge to fit speaker name
            float textWidth = _badgeLabel.preferredWidth + 32f;
            float badgeWidth = Mathf.Max(120f, textWidth);
            RectTransform badgeBorderRt = _badgeImage.transform.parent.GetComponent<RectTransform>();
            badgeBorderRt.sizeDelta = new Vector2(badgeWidth, 38f);

            // Update body text (typewriter)
            _bodyText.text = mgr.DisplayedText;

            // Update continuation prompt
            if (mgr.IsLineFullyTyped)
            {
                if (!_promptText.gameObject.activeSelf) _promptText.gameObject.SetActive(true);

                // Pulse alpha
                float alpha = 0.5f + Mathf.PingPong(Time.time * 3f, 0.5f);
                Color c = UITheme.TextGold;
                c.a = alpha;
                _promptText.color = c;
            }
            else
            {
                if (_promptText.gameObject.activeSelf) _promptText.gameObject.SetActive(false);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureDialogueUI()
        {
            if (UnityEngine.Object.FindAnyObjectByType<DialogueUI>() == null)
            {
                var obj = new GameObject("DialogueUI");
                obj.AddComponent<DialogueUI>();
            }
        }
    }
}
