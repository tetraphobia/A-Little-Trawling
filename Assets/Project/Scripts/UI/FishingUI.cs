using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LittleTrawling.Systems;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Displays onscreen fishing state indicators using uGUI:
    /// charging progress bar, waiting-for-bite banner, and bite alert.
    /// Styled with Animal Crossing warm pastel theme.
    /// </summary>
    public class FishingUI : MonoBehaviour
    {
        public static FishingUI Instance { get; private set; }

        private Canvas _canvas;

        // Charging state
        private GameObject _chargingRoot;
        private Image _chargeBarFill;
        private TextMeshProUGUI _chargingLabel;

        // Waiting state
        private GameObject _waitingRoot;

        // Bite state
        private GameObject _biteRoot;
        private TextMeshProUGUI _biteLabel;
        private Image _biteBorderImage;

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
            _canvas = UITheme.CreateScreenCanvas("FishingUI_Canvas", 30);
            _canvas.transform.SetParent(transform, false);

            BuildChargingUI();
            BuildWaitingUI();
            BuildBiteUI();

            // Hide all initially
            _chargingRoot.SetActive(false);
            _waitingRoot.SetActive(false);
            _biteRoot.SetActive(false);
        }

        private void BuildChargingUI()
        {
            // Outer border (gold)
            Image border = UITheme.CreatePanel("ChargingBorder", _canvas.transform,
                UITheme.ProgressTrackSprite, UITheme.Gold);
            UITheme.AnchorBottomCenter(border.rectTransform, 420f, 60f, 90f);
            _chargingRoot = border.gameObject;

            // Track background (warm white)
            Image track = UITheme.CreatePanel("ChargingTrack", border.transform,
                UITheme.ProgressTrackSprite, UITheme.CardWhite);
            UITheme.StretchFill(track.rectTransform, 3f, 3f, 3f, 3f);

            // Fill bar (leaf green)
            _chargeBarFill = UITheme.CreatePanel("ChargingFill", track.transform,
                UITheme.ProgressFillSprite, UITheme.LeafGreen);
            RectTransform fillRt = _chargeBarFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.offsetMin = new Vector2(3, 3);
            fillRt.offsetMax = new Vector2(0, -3);

            // Label overlay
            _chargingLabel = UITheme.CreateLabel("ChargingLabel", track.transform, "Casting... 0%",
                UITheme.BodyFontSize, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.Center);
            UITheme.StretchFill(_chargingLabel.rectTransform);
        }

        private void BuildWaitingUI()
        {
            // Border
            Image border = UITheme.CreatePanel("WaitingBorder", _canvas.transform,
                UITheme.BadgeSprite, UITheme.Gold);
            UITheme.AnchorBottomCenter(border.rectTransform, 500f, 60f, 90f);
            _waitingRoot = border.gameObject;

            // Background pill
            Image bg = UITheme.CreatePanel("WaitingBg", border.transform,
                UITheme.BadgeSprite, UITheme.CardWhite);
            UITheme.StretchFill(bg.rectTransform, 3f, 3f, 3f, 3f);

            // Label
            TextMeshProUGUI label = UITheme.CreateLabel("WaitingLabel", bg.transform,
                "Waiting for a bite... (Press <b>[F]</b> to recall)",
                UITheme.BodyFontSize, UITheme.TextBrown, FontStyles.Normal, TextAlignmentOptions.Center);
            label.richText = true;
            UITheme.StretchFill(label.rectTransform, 16f, 16f, 0f, 0f);
        }

        private void BuildBiteUI()
        {
            // Border (pulsing)
            Image border = UITheme.CreatePanel("BiteBorder", _canvas.transform,
                UITheme.BadgeSprite, UITheme.Gold);
            UITheme.AnchorBottomCenter(border.rectTransform, 520f, 76f, 130f);
            _biteRoot = border.gameObject;
            _biteBorderImage = border;

            // Background pill
            Image bg = UITheme.CreatePanel("BiteBg", border.transform,
                UITheme.BadgeSprite, UITheme.CardWhite);
            UITheme.StretchFill(bg.rectTransform, 3f, 3f, 3f, 3f);

            // Label
            _biteLabel = UITheme.CreateLabel("BiteLabel", bg.transform,
                "BITE! PRESS [F] NOW!",
                30f, UITheme.TextGold, FontStyles.Bold, TextAlignmentOptions.Center);
            UITheme.StretchFill(_biteLabel.rectTransform, 16f, 16f, 0f, 0f);
        }

        private void Update()
        {
            if (FishingManager.Instance == null) return;

            FishingState state = FishingManager.Instance.CurrentState;

            _chargingRoot.SetActive(state == FishingState.Charging);
            _waitingRoot.SetActive(state == FishingState.WaitingForBite);
            _biteRoot.SetActive(state == FishingState.BiteActive);

            switch (state)
            {
                case FishingState.Charging:
                    UpdateChargingBar();
                    break;
                case FishingState.BiteActive:
                    UpdateBitePulse();
                    break;
            }
        }

        private void UpdateChargingBar()
        {
            float ratio = FishingManager.Instance.ChargeRatio;

            // Update fill width via anchor
            RectTransform fillRt = _chargeBarFill.rectTransform;
            fillRt.anchorMax = new Vector2(ratio, 1);

            _chargingLabel.text = $"Casting... {Mathf.RoundToInt(ratio * 100)}%";
        }

        private void UpdateBitePulse()
        {
            // Pulsing alpha on the border and label
            float alpha = 0.6f + Mathf.PingPong(Time.time * 3f, 0.4f);
            if (_biteBorderImage != null)
            {
                Color c = UITheme.Gold;
                c.a = alpha;
                _biteBorderImage.color = c;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureFishingUI()
        {
            if (Object.FindAnyObjectByType<FishingUI>() == null)
            {
                var uiObj = new GameObject("FishingUI");
                uiObj.AddComponent<FishingUI>();
            }
        }
    }
}
