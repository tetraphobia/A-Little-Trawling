using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LittleTrawling.Audio;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Systems;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Displays a scrollable uGUI inventory modal when pressing 'I'.
    /// Shows fish sprite, name, rarity badge, description, size, weight, and sell price.
    /// Styled with Animal Crossing warm pastel theme.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        private bool _isOpen;
        [Header("Audio SFX")]
        [Tooltip("Sound played when inventory opens.")]
        [SerializeField] private AudioClip windowOpenSound;
        [Tooltip("Sound played when inventory closes.")]
        [SerializeField] private AudioClip windowCloseSound;

        private AudioSource _audioSource;

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
                _audioSource.spatialBlend = 0f;
            }
            _audioSource.PlayOneShot(clip);
        }

        private Canvas _canvas;
        private GameObject _modalRoot;
        private RectTransform _contentContainer;
        private TextMeshProUGUI _headerLabel;
        private GameObject _emptyState;

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
                InputReader.Instance.InventoryPressed += ToggleInventory;
                InputReader.Instance.ClosePressed += OnClosePressed;
            }

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged += RebuildCardList;
            }
        }

        private void OnDestroy()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.InventoryPressed -= ToggleInventory;
                InputReader.Instance.ClosePressed -= OnClosePressed;
            }
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= RebuildCardList;
            }
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (Instance == this) Instance = null;
        }

        private void ToggleInventory()
        {
            _isOpen = !_isOpen;
            _modalRoot.SetActive(_isOpen);
            if (_isOpen)
            {
                RebuildCardList();
                PlaySFX(windowOpenSound != null ? windowOpenSound : ProceduralAudioSynthesizer.GetWindowOpenSound());
            }
            else
            {
                PlaySFX(windowCloseSound != null ? windowCloseSound : ProceduralAudioSynthesizer.GetWindowCloseSound());
            }
        }

        private void OnClosePressed()
        {
            if (_isOpen)
            {
                _isOpen = false;
                _modalRoot.SetActive(false);
                PlaySFX(windowCloseSound != null ? windowCloseSound : ProceduralAudioSynthesizer.GetWindowCloseSound());
            }
        }

        // ── UI Construction ────────────────────────────────────────────

        private void BuildUI()
        {
            _canvas = UITheme.CreateScreenCanvas("InventoryUI_Canvas", 40);
            _canvas.transform.SetParent(transform, false);

            _modalRoot = new GameObject("ModalRoot");
            _modalRoot.transform.SetParent(_canvas.transform, false);
            RectTransform modalRootRt = _modalRoot.AddComponent<RectTransform>();
            UITheme.StretchFill(modalRootRt);

            UITheme.CreateDimOverlay("DimOverlay", _modalRoot.transform);

            Image panelBorder = UITheme.CreatePanel("PanelBorder", _modalRoot.transform,
                UITheme.PanelSprite, UITheme.Gold);
            UITheme.CenterWithSize(panelBorder.rectTransform, 800f, 660f);

            Image panelBg = UITheme.CreatePanel("PanelBg", panelBorder.transform,
                UITheme.PanelSprite, UITheme.CardWhite);
            UITheme.StretchFill(panelBg.rectTransform, 4f, 4f, 4f, 4f);

            RectTransform headerBar = UITheme.CreateRect("HeaderBar", panelBg.transform);
            headerBar.anchorMin = new Vector2(0, 1);
            headerBar.anchorMax = new Vector2(1, 1);
            headerBar.pivot = new Vector2(0.5f, 1);
            headerBar.sizeDelta = new Vector2(0, 60f);
            headerBar.anchoredPosition = Vector2.zero;

            _headerLabel = UITheme.CreateLabel("HeaderLabel", headerBar, "FISH INVENTORY",
                UITheme.HeaderFontSize - 2f, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            RectTransform headerLabelRt = _headerLabel.rectTransform;
            headerLabelRt.anchorMin = Vector2.zero;
            headerLabelRt.anchorMax = Vector2.one;
            headerLabelRt.offsetMin = new Vector2(UITheme.Padding, 0);
            headerLabelRt.offsetMax = new Vector2(-60f, 0);

            Button closeBtn = UITheme.CreateButton("CloseBtn", headerBar, "X",
                UITheme.Gold, UITheme.TextWhite, UITheme.BodyFontSize, 48f, 48f);
            RectTransform closeBtnRt = closeBtn.GetComponent<RectTransform>();
            closeBtnRt.anchorMin = new Vector2(1, 0.5f);
            closeBtnRt.anchorMax = new Vector2(1, 0.5f);
            closeBtnRt.pivot = new Vector2(1, 0.5f);
            closeBtnRt.anchoredPosition = new Vector2(-12f, 0);
            closeBtn.onClick.AddListener(() =>
            {
                _isOpen = false;
                _modalRoot.SetActive(false);
            });

            Image sep = UITheme.CreatePanel("Separator", panelBg.transform, null, UITheme.AccentSkyBlue);
            RectTransform sepRt = sep.rectTransform;
            sepRt.anchorMin = new Vector2(0, 1);
            sepRt.anchorMax = new Vector2(1, 1);
            sepRt.pivot = new Vector2(0.5f, 1);
            sepRt.sizeDelta = new Vector2(0, 3f);
            sepRt.anchoredPosition = new Vector2(0, -66f);
            sepRt.offsetMin = new Vector2(UITheme.Padding, sepRt.offsetMin.y);
            sepRt.offsetMax = new Vector2(-UITheme.Padding, sepRt.offsetMax.y);

            var (scrollRect, content) = UITheme.CreateScrollView("FishScroll", panelBg.transform);
            RectTransform scrollRt = scrollRect.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(UITheme.Padding, UITheme.Padding);
            scrollRt.offsetMax = new Vector2(-UITheme.Padding, -78f);
            _contentContainer = content;

            _emptyState = new GameObject("EmptyState");
            _emptyState.transform.SetParent(panelBg.transform, false);
            RectTransform emptyRt = _emptyState.AddComponent<RectTransform>();
            UITheme.CenterWithSize(emptyRt, 400f, 80f);

            TextMeshProUGUI emptyTitle = UITheme.CreateLabel("EmptyTitle", _emptyState.transform,
                "Your inventory is empty!",
                UITheme.TitleFontSize, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.Center);
            UITheme.StretchFill(emptyTitle.rectTransform);

            _emptyState.SetActive(false);
            _modalRoot.SetActive(false);
        }

        private void RebuildCardList()
        {
            for (int i = _contentContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_contentContainer.GetChild(i).gameObject);
            }

            var mgr = InventoryManager.Instance;
            var items = mgr != null ? mgr.Items : null;
            int itemCount = items != null ? items.Count : 0;
            int totalValue = mgr != null ? mgr.CalculateTotalValue() : 0;

            if (itemCount == 0)
            {
                _headerLabel.text = "FISH INVENTORY";
                _emptyState.SetActive(true);
                return;
            }

            _emptyState.SetActive(false);
            _headerLabel.text = $"FISH INVENTORY  <size={UITheme.SmallFontSize}><color=#{ColorUtility.ToHtmlStringRGB(UITheme.TextMuted)}>({itemCount} Caught  |  Total: <color=#{ColorUtility.ToHtmlStringRGB(UITheme.TextGold)}>${totalValue} Gold</color>)</color></size>";
            _headerLabel.richText = true;

            for (int i = 0; i < itemCount; i++)
            {
                var item = items[i];
                if (item == null) continue;
                BuildFishCard(item);
            }
        }

        private void BuildFishCard(CaughtFish item)
        {
            Image cardBorder = UITheme.CreatePanel("FishCard", _contentContainer,
                UITheme.CardSprite, UITheme.AccentSkyBlue);
            RectTransform cardBorderRt = cardBorder.rectTransform;
            LayoutElement le = cardBorder.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 152f;
            le.flexibleWidth = 1f;

            Image cardBg = UITheme.CreatePanel("CardBg", cardBorder.transform,
                UITheme.CardSprite, UITheme.CardWhite);
            UITheme.StretchFill(cardBg.rectTransform, 2f, 2f, 2f, 2f);

            Sprite fishSprite = item.species != null ? item.species.sprite : null;
            float frameWidth = 52f;
            float aspect = (fishSprite != null && fishSprite.rect.width > 0)
                ? (fishSprite.rect.height / fishSprite.rect.width)
                : 0.55f;
            float frameHeight = Mathf.Clamp(frameWidth * aspect, 20f, 60f);

            Image spriteFrame = UITheme.CreatePanel("SpriteFrame", cardBg.transform,
                UITheme.CardSprite, UITheme.BackgroundMint);
            RectTransform spriteFrameRt = spriteFrame.rectTransform;
            spriteFrameRt.anchorMin = new Vector2(0, 0.5f);
            spriteFrameRt.anchorMax = new Vector2(0, 0.5f);
            spriteFrameRt.pivot = new Vector2(0, 0.5f);
            spriteFrameRt.sizeDelta = new Vector2(frameWidth, frameHeight);
            spriteFrameRt.anchoredPosition = new Vector2(12f, 0);

            if (fishSprite != null && fishSprite.texture != null)
            {
                Image fishImg = UITheme.CreatePanel("FishSprite", spriteFrame.transform, fishSprite, Color.white);
                fishImg.preserveAspect = true;
                UITheme.StretchFill(fishImg.rectTransform, 4f, 4f, 4f, 4f);
            }
            else
            {
                TextMeshProUGUI noSprite = UITheme.CreateLabel("NoSprite", spriteFrame.transform,
                    "NO\nSPRITE", UITheme.SmallFontSize - 4f, UITheme.TextMuted, FontStyles.Italic, TextAlignmentOptions.Center);
                noSprite.textWrappingMode = TextWrappingModes.Normal;
                UITheme.StretchFill(noSprite.rectTransform);
            }

            float textLeft = 78f;
            float textWidth = -150f;

            string fishName = item.species != null ? item.species.displayName : "Unknown Fish";
            FishRarity rarity = item.species != null ? item.species.rarity : FishRarity.Common;
            string rarityHex = GetRarityColorHex(rarity);
            string rarityText = rarity.ToString();

            string lunkerBadge = item.lunkerStatus switch
            {
                LunkerStatus.MegaLunker => "<color=#FFD700><b>[MEGA LUNKER!]</b></color> ",
                LunkerStatus.Lunker => "<color=#EE5D5D><b>[LUNKER!]</b></color> ",
                _ => ""
            };

            TextMeshProUGUI nameLabel = UITheme.CreateLabel("NameLabel", cardBg.transform,
                $"{lunkerBadge}{fishName}  <size={UITheme.SmallFontSize}><color={rarityHex}><b>[{rarityText}]</b></color></size>",
                20f, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            nameLabel.richText = true;
            RectTransform nameLabelRt = nameLabel.rectTransform;
            nameLabelRt.anchorMin = new Vector2(0, 0.72f);
            nameLabelRt.anchorMax = new Vector2(1, 1f);
            nameLabelRt.offsetMin = new Vector2(textLeft, 0);
            nameLabelRt.offsetMax = new Vector2(textWidth, -6f);

            string desc = item.species != null ? item.species.description : "No description available.";
            TextMeshProUGUI descLabel = UITheme.CreateLabel("DescLabel", cardBg.transform,
                $"<i>\"{desc}\"</i>",
                UITheme.SmallFontSize - 1f, UITheme.TextMuted, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            descLabel.richText = true;
            descLabel.textWrappingMode = TextWrappingModes.Normal;
            descLabel.overflowMode = TextOverflowModes.Overflow;
            RectTransform descLabelRt = descLabel.rectTransform;
            descLabelRt.anchorMin = new Vector2(0, 0.25f);
            descLabelRt.anchorMax = new Vector2(1, 0.72f);
            descLabelRt.offsetMin = new Vector2(textLeft, 0);
            descLabelRt.offsetMax = new Vector2(textWidth, 0);

            TextMeshProUGUI statsLabel = UITheme.CreateLabel("StatsLabel", cardBg.transform,
                $"Size: <b>{item.sizeCm:F1} cm</b>  |  Weight: <b>{item.weightKg:F2} kg</b>",
                UITheme.SmallFontSize, UITheme.AccentSkyBlue, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            statsLabel.richText = true;
            Color statsColor = new Color32(70, 150, 200, 255);
            statsLabel.color = statsColor;
            RectTransform statsLabelRt = statsLabel.rectTransform;
            statsLabelRt.anchorMin = new Vector2(0, 0f);
            statsLabelRt.anchorMax = new Vector2(1, 0.25f);
            statsLabelRt.offsetMin = new Vector2(textLeft, 6f);
            statsLabelRt.offsetMax = new Vector2(textWidth, 0);

            Image priceBadge = UITheme.CreatePanel("PriceBadge", cardBg.transform,
                UITheme.BadgeSprite, UITheme.Gold);
            RectTransform priceRt = priceBadge.rectTransform;
            priceRt.anchorMin = new Vector2(1, 0.5f);
            priceRt.anchorMax = new Vector2(1, 0.5f);
            priceRt.pivot = new Vector2(1, 0.5f);
            priceRt.sizeDelta = new Vector2(120f, 44f);
            priceRt.anchoredPosition = new Vector2(-12f, 0);

            TextMeshProUGUI priceLabel = UITheme.CreateLabel("PriceLabel", priceBadge.transform,
                $"${item.sellPrice}", UITheme.BodyFontSize, UITheme.TextWhite, FontStyles.Bold, TextAlignmentOptions.Center);
            UITheme.StretchFill(priceLabel.rectTransform);
        }

        private string GetRarityColorHex(FishRarity rarity)
        {
            return rarity switch
            {
                FishRarity.Common => "#6BBF6B",
                FishRarity.Uncommon => "#5BA8D8",
                FishRarity.Rare => "#E88BE8",
                _ => "#FFFFFF"
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureInventoryUI()
        {
            if (UnityEngine.Object.FindAnyObjectByType<InventoryUI>() == null)
            {
                var uiObj = new GameObject("InventoryUI");
                uiObj.AddComponent<InventoryUI>();
            }
        }
    }
}
