using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LittleTrawling.Core;
using LittleTrawling.Systems;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Always-on HUD anchored to the top-right corner showing gold balance and fish count.
    /// Visible only during Walking and Fishing game states.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        public static PlayerHUD Instance { get; private set; }

        [Header("Fish Icon (assign later)")]
        [Tooltip("Optional sprite for the fish count icon. Leave null for a text placeholder.")]
        [SerializeField] private Sprite fishIconSprite;

        private Canvas _canvas;
        private GameObject _hudRoot;
        private TextMeshProUGUI _goldLabel;
        private TextMeshProUGUI _fishLabel;
        private Image _fishIconImage;

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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged += OnStateChanged;
                OnStateChanged(GameManager.Instance.CurrentState);
            }
            if (Wallet.Instance != null)
            {
                Wallet.Instance.GoldChanged += OnGoldChanged;
            }
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;
            }

            RefreshValues();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= OnStateChanged;
            if (Wallet.Instance != null)
                Wallet.Instance.GoldChanged -= OnGoldChanged;
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;

            if (_canvas != null) Destroy(_canvas.gameObject);
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            _canvas = UITheme.CreateScreenCanvas("PlayerHUD_Canvas", 50);
            _canvas.transform.SetParent(transform, false);

            Image pillBorder = UITheme.CreatePanel("HUD_Border", _canvas.transform,
                UITheme.BadgeSprite, UITheme.Gold);
            UITheme.AnchorTopRight(pillBorder.rectTransform, 340f, 62f, 28f, 24f);

            Image pillBg = UITheme.CreatePanel("HUD_Background", pillBorder.transform,
                UITheme.BadgeSprite, UITheme.CardWhite);
            UITheme.StretchFill(pillBg.rectTransform, 3f, 3f, 3f, 3f);

            _hudRoot = pillBorder.gameObject;

            RectTransform goldSection = UITheme.CreateRect("GoldSection", pillBg.transform);
            goldSection.anchorMin = new Vector2(0, 0);
            goldSection.anchorMax = new Vector2(0.48f, 1);
            goldSection.offsetMin = new Vector2(14, 0);
            goldSection.offsetMax = new Vector2(0, 0);

            _goldLabel = UITheme.CreateLabel("GoldLabel", goldSection, "$0",
                UITheme.BodyFontSize, UITheme.TextGold, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UITheme.StretchFill(_goldLabel.rectTransform);

            Image divider = UITheme.CreatePanel("Divider", pillBg.transform, null, UITheme.AccentSkyBlue);
            RectTransform divRt = divider.rectTransform;
            divRt.anchorMin = new Vector2(0.5f, 0.15f);
            divRt.anchorMax = new Vector2(0.5f, 0.85f);
            divRt.sizeDelta = new Vector2(2, 0);
            divRt.anchoredPosition = Vector2.zero;

            RectTransform fishSection = UITheme.CreateRect("FishSection", pillBg.transform);
            fishSection.anchorMin = new Vector2(0.52f, 0);
            fishSection.anchorMax = new Vector2(1, 1);
            fishSection.offsetMin = new Vector2(0, 0);
            fishSection.offsetMax = new Vector2(-14, 0);

            Image fishIcon = UITheme.CreatePanel("FishIcon", fishSection, null, Color.white);
            _fishIconImage = fishIcon;
            RectTransform iconRt = fishIcon.rectTransform;
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(28, 28);
            iconRt.anchoredPosition = new Vector2(4, 0);

            if (fishIconSprite != null)
            {
                fishIcon.sprite = fishIconSprite;
                fishIcon.preserveAspect = true;
            }
            else
            {
                fishIcon.enabled = false;
            }

            _fishLabel = UITheme.CreateLabel("FishLabel", fishSection, "Fish: 0",
                UITheme.BodyFontSize, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            RectTransform fishLabelRt = _fishLabel.rectTransform;
            fishLabelRt.anchorMin = new Vector2(0, 0);
            fishLabelRt.anchorMax = new Vector2(1, 1);

            if (fishIconSprite != null)
            {
                fishLabelRt.offsetMin = new Vector2(36, 0);
            }
            else
            {
                fishLabelRt.offsetMin = new Vector2(4, 0);
            }
            fishLabelRt.offsetMax = Vector2.zero;
        }

        private void OnStateChanged(GameState state)
        {
            bool visible = (state == GameState.Walking || state == GameState.Fishing || state == GameState.Piloting || state == GameState.Shopping);
            if (_hudRoot != null) _hudRoot.SetActive(visible);
        }

        private void OnGoldChanged(int newGold)
        {
            RefreshGold(newGold);
        }

        private void OnInventoryChanged()
        {
            RefreshFishCount();
        }

        private void RefreshValues()
        {
            int gold = Wallet.Instance != null ? Wallet.Instance.CurrentGold : 0;
            RefreshGold(gold);
            RefreshFishCount();
        }

        private void RefreshGold(int gold)
        {
            if (_goldLabel != null)
                _goldLabel.text = $"${gold:N0}";
        }

        private void RefreshFishCount()
        {
            int count = InventoryManager.Instance != null ? InventoryManager.Instance.TotalCount : 0;
            if (_fishLabel != null)
                _fishLabel.text = $"Fish: {count}";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsurePlayerHUD()
        {
            if (UnityEngine.Object.FindAnyObjectByType<PlayerHUD>() == null)
            {
                var obj = new GameObject("PlayerHUD");
                obj.AddComponent<PlayerHUD>();
            }
        }
    }
}
