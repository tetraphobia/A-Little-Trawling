using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LittleTrawling.Systems;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Displays onscreen fishing state indicators using uGUI:
    /// charging progress bar. Styled with Animal Crossing warm pastel theme.
    /// </summary>
    public class FishingUI : MonoBehaviour
    {
        public static FishingUI Instance { get; private set; }

        private Canvas _canvas;

        private GameObject _chargingRoot;
        private Image _chargeBarFill;
        private TextMeshProUGUI _chargingLabel;

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

            _chargingRoot.SetActive(false);
        }

        private void BuildChargingUI()
        {
            Image border = UITheme.CreatePanel("ChargingBorder", _canvas.transform,
                UITheme.ProgressTrackSprite, UITheme.Gold);
            UITheme.AnchorBottomCenter(border.rectTransform, 420f, 60f, 90f);
            _chargingRoot = border.gameObject;

            Image track = UITheme.CreatePanel("ChargingTrack", border.transform,
                UITheme.ProgressTrackSprite, UITheme.CardWhite);
            UITheme.StretchFill(track.rectTransform, 3f, 3f, 3f, 3f);

            _chargeBarFill = UITheme.CreatePanel("ChargingFill", track.transform,
                UITheme.ProgressFillSprite, UITheme.LeafGreen);
            RectTransform fillRt = _chargeBarFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.offsetMin = new Vector2(3, 3);
            fillRt.offsetMax = new Vector2(0, -3);

            _chargingLabel = UITheme.CreateLabel("ChargingLabel", track.transform, "Casting... 0%",
                UITheme.BodyFontSize, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.Center);
            UITheme.StretchFill(_chargingLabel.rectTransform);
        }

        private void Update()
        {
            if (FishingManager.Instance == null) return;

            FishingState state = FishingManager.Instance.CurrentState;
            bool isCharging = (state == FishingState.Charging);

            if (_chargingRoot.activeSelf != isCharging)
            {
                _chargingRoot.SetActive(isCharging);
            }

            if (isCharging)
            {
                UpdateChargingBar();
            }
        }

        private void UpdateChargingBar()
        {
            float ratio = FishingManager.Instance.ChargeRatio;

            RectTransform fillRt = _chargeBarFill.rectTransform;
            fillRt.anchorMax = new Vector2(ratio, 1);

            _chargingLabel.text = $"Casting... {Mathf.RoundToInt(ratio * 100)}%";
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
