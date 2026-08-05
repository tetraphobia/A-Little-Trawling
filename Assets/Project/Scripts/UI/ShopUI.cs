using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LittleTrawling.Audio;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Entities;
using LittleTrawling.Systems;
using LittleTrawling.Vehicles;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Manages the Upgrade Shop UI overlay and equipment purchases using uGUI.
    /// Styled with Animal Crossing warm pastel theme.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        [Header("Available Engine Upgrades")]
        [SerializeField] private List<Engine> availableEngines = new List<Engine>();

        [Header("Available Rod Upgrades")]
        [SerializeField] private List<Rod> availableRods = new List<Rod>();

        [Header("Audio SFX")]
        [Tooltip("Sound played when window opens.")]
        [SerializeField] private AudioClip windowOpenSound;
        [Tooltip("Sound played when window closes.")]
        [SerializeField] private AudioClip windowCloseSound;
        [Tooltip("Sound played when selling fish.")]
        [SerializeField] private AudioClip sellFishSound;
        [Tooltip("Sound played when buying an item upgrade.")]
        [SerializeField] private AudioClip buyItemSound;

        private AudioSource _audioSource;

        private void PlaySFX(AudioClip clip, float baseVolume = 0.50f)
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
            float vol = VolumeManager.Instance != null ? VolumeManager.Instance.UiSoundVolume : baseVolume;
            if (VolumeManager.Instance != null)
            {
                VolumeManager.Instance.PlayOneShot(_audioSource, clip, vol, AudioCategory.UI);
            }
            else
            {
                _audioSource.PlayOneShot(clip, vol);
            }
        }

        private bool _isOpen;
        private bool _openedThisFrame;

        private Canvas _canvas;
        private GameObject _modalRoot;
        private RectTransform _contentContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadDefaultCatalog();
            BuildUI();
        }

        private void LoadDefaultCatalog()
        {
            if (availableEngines == null) availableEngines = new List<Engine>();
            if (availableRods == null) availableRods = new List<Rod>();

            availableEngines.Clear();
            availableRods.Clear();

            var engines = Resources.LoadAll<Engine>("Data/Engines");
            if (engines != null) availableEngines.AddRange(engines);

            var rods = Resources.LoadAll<Rod>("Data/Rods");
            if (rods != null) availableRods.AddRange(rods);

            // Sort catalog items by cost
            availableEngines.Sort((a, b) => a.cost.CompareTo(b.cost));
            availableRods.Sort((a, b) => a.cost.CompareTo(b.cost));
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.StateChanged += OnStateChanged;
                OnStateChanged(gm.CurrentState);
            }

            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed += OnInteractPressed;
                InputReader.Instance.ClosePressed += OnClosePressed;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= OnStateChanged;

            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed -= OnInteractPressed;
                InputReader.Instance.ClosePressed -= OnClosePressed;
            }

            if (_canvas != null) Destroy(_canvas.gameObject);
            if (Instance == this) Instance = null;
        }

        private void OnStateChanged(GameState state)
        {
            bool wasOpen = _isOpen;
            _isOpen = (state == GameState.Shopping);
            if (_isOpen && !wasOpen)
            {
                _openedThisFrame = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _modalRoot.SetActive(true);
                RebuildShopContent();
                PlaySFX(windowOpenSound != null ? windowOpenSound : ProceduralAudioSynthesizer.GetWindowOpenSound());
            }
            else if (!_isOpen && wasOpen)
            {
                _modalRoot.SetActive(false);
                PlaySFX(windowCloseSound != null ? windowCloseSound : ProceduralAudioSynthesizer.GetWindowCloseSound());
            }
        }

        private void LateUpdate()
        {
            _openedThisFrame = false;
        }

        private void OnClosePressed()
        {
            if (_isOpen && !_openedThisFrame)
            {
                CloseShop();
            }
        }

        private void OnInteractPressed()
        {
            if (_isOpen && !_openedThisFrame)
            {
                CloseShop();
            }
        }

        public void CloseShop()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Walking);
            }
        }

        // ── UI Construction ────────────────────────────────────────────

        private void BuildUI()
        {
            _canvas = UITheme.CreateScreenCanvas("ShopUI_Canvas", 45);
            _canvas.transform.SetParent(transform, false);

            _modalRoot = new GameObject("ShopModalRoot");
            _modalRoot.transform.SetParent(_canvas.transform, false);
            RectTransform modalRootRt = _modalRoot.AddComponent<RectTransform>();
            UITheme.StretchFill(modalRootRt);

            UITheme.CreateDimOverlay("DimOverlay", _modalRoot.transform);

            Image panelBorder = UITheme.CreatePanel("PanelBorder", _modalRoot.transform,
                UITheme.PanelSprite, UITheme.Gold);
            UITheme.CenterWithSize(panelBorder.rectTransform, 800f, 640f);

            Image panelBg = UITheme.CreatePanel("PanelBg", panelBorder.transform,
                UITheme.PanelSprite, UITheme.CardWhite);
            UITheme.StretchFill(panelBg.rectTransform, 4f, 4f, 4f, 4f);

            RectTransform headerBar = UITheme.CreateRect("HeaderBar", panelBg.transform);
            headerBar.anchorMin = new Vector2(0, 1);
            headerBar.anchorMax = new Vector2(1, 1);
            headerBar.pivot = new Vector2(0.5f, 1);
            headerBar.sizeDelta = new Vector2(0, 60f);
            headerBar.anchoredPosition = Vector2.zero;

            TextMeshProUGUI headerLabel = UITheme.CreateLabel("HeaderLabel", headerBar,
                "Shop",
                UITheme.HeaderFontSize - 2f, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            RectTransform headerLabelRt = headerLabel.rectTransform;
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
            closeBtn.onClick.AddListener(CloseShop);

            Image sep = UITheme.CreatePanel("Separator", panelBg.transform, null, UITheme.AccentSkyBlue);
            RectTransform sepRt = sep.rectTransform;
            sepRt.anchorMin = new Vector2(0, 1);
            sepRt.anchorMax = new Vector2(1, 1);
            sepRt.pivot = new Vector2(0.5f, 1);
            sepRt.sizeDelta = new Vector2(0, 3f);
            sepRt.anchoredPosition = new Vector2(0, -66f);
            sepRt.offsetMin = new Vector2(UITheme.Padding, sepRt.offsetMin.y);
            sepRt.offsetMax = new Vector2(-UITheme.Padding, sepRt.offsetMax.y);

            var (scrollRect, content) = UITheme.CreateScrollView("ShopScroll", panelBg.transform, true, 8f);
            RectTransform scrollRt = scrollRect.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(UITheme.Padding, UITheme.Padding);
            scrollRt.offsetMax = new Vector2(-UITheme.Padding, -78f);
            _contentContainer = content;

            _modalRoot.SetActive(false);
        }

        private void RebuildShopContent()
        {
            for (int i = _contentContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_contentContainer.GetChild(i).gameObject);
            }

            BuildSectionHeader("Sell");
            BuildSellFishCard();

            BuildSpacer(10f);

            BuildSectionHeader("Engines");
            var boat = BoatController.Instance;
            Engine currentEngine = boat != null ? boat.Engine : null;

            foreach (var eng in availableEngines)
            {
                if (eng == null) continue;
                BuildEngineCard(eng, currentEngine);
            }

            BuildSpacer(10f);

            BuildSectionHeader("Fishing Rods");
            var player = PlayerController.Instance;
            Rod currentRod = player != null ? player.Rod : null;

            foreach (var rod in availableRods)
            {
                if (rod == null) continue;
                BuildRodCard(rod, currentRod);
            }
        }

        private void BuildSectionHeader(string title)
        {
            RectTransform header = UITheme.CreateRect("SectionHeader_" + title, _contentContainer);
            LayoutElement le = header.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 34f;
            le.flexibleWidth = 1f;

            TextMeshProUGUI label = UITheme.CreateLabel("Label", header,
                title, UITheme.BodyFontSize, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
            UITheme.StretchFill(label.rectTransform);
        }

        private void BuildSpacer(float height)
        {
            RectTransform spacer = UITheme.CreateRect("Spacer", _contentContainer);
            LayoutElement le = spacer.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
        }

        private void BuildSellFishCard()
        {
            var invMgr = InventoryManager.Instance;
            int caughtCount = invMgr != null ? invMgr.TotalCount : 0;
            int totalFishValue = invMgr != null ? invMgr.CalculateTotalValue() : 0;

            // Card border
            Image cardBorder = UITheme.CreatePanel("SellFishCard", _contentContainer,
                UITheme.CardSprite, UITheme.AccentSkyBlue);
            LayoutElement le = cardBorder.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 60f;
            le.flexibleWidth = 1f;

            // Card fill
            Image cardBg = UITheme.CreatePanel("CardBg", cardBorder.transform,
                UITheme.CardSprite, UITheme.CardWhite);
            UITheme.StretchFill(cardBg.rectTransform, 2f, 2f, 2f, 2f);

            if (caughtCount == 0)
            {
                // Empty state
                TextMeshProUGUI emptyLabel = UITheme.CreateLabel("EmptyLabel", cardBg.transform,
                    "No fish in inventory to sell.",
                    UITheme.SmallFontSize, UITheme.TextMuted, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
                RectTransform emptyLabelRt = emptyLabel.rectTransform;
                emptyLabelRt.anchorMin = Vector2.zero;
                emptyLabelRt.anchorMax = new Vector2(0.6f, 1);
                emptyLabelRt.offsetMin = new Vector2(16f, 0);
                emptyLabelRt.offsetMax = Vector2.zero;

                Button sellBtn = UITheme.CreateButton("SellBtn", cardBg.transform, "Sell All Fish ($0)",
                    UITheme.MutedButton, UITheme.TextWhite, UITheme.SmallFontSize, 190f, 38f);
                sellBtn.interactable = false;
                RectTransform sellBtnRt = sellBtn.GetComponent<RectTransform>();
                sellBtnRt.anchorMin = new Vector2(1, 0.5f);
                sellBtnRt.anchorMax = new Vector2(1, 0.5f);
                sellBtnRt.pivot = new Vector2(1, 0.5f);
                sellBtnRt.anchoredPosition = new Vector2(-12f, 0);
            }
            else
            {
                // Fish info
                TextMeshProUGUI fishInfo = UITheme.CreateLabel("FishInfo", cardBg.transform,
                    $"Inventory: <b>{caughtCount} Fish</b> (Total Value: <color=#{ColorUtility.ToHtmlStringRGB(UITheme.TextGold)}>${totalFishValue}</color>)",
                    UITheme.SmallFontSize, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
                fishInfo.richText = true;
                RectTransform fishInfoRt = fishInfo.rectTransform;
                fishInfoRt.anchorMin = Vector2.zero;
                fishInfoRt.anchorMax = new Vector2(0.55f, 1);
                fishInfoRt.offsetMin = new Vector2(16f, 0);
                fishInfoRt.offsetMax = Vector2.zero;

                Button sellBtn = UITheme.CreateButton("SellBtn", cardBg.transform, $"Sell All Fish (${totalFishValue})",
                    UITheme.LeafGreen, UITheme.TextWhite, UITheme.SmallFontSize, 200f, 38f);
                RectTransform sellBtnRt = sellBtn.GetComponent<RectTransform>();
                sellBtnRt.anchorMin = new Vector2(1, 0.5f);
                sellBtnRt.anchorMax = new Vector2(1, 0.5f);
                sellBtnRt.pivot = new Vector2(1, 0.5f);
                sellBtnRt.anchoredPosition = new Vector2(-12f, 0);
                sellBtn.onClick.AddListener(() =>
                {
                    if (Wallet.Instance != null)
                    {
                        Wallet.Instance.AddGold(totalFishValue);
                    }
                    if (invMgr != null) invMgr.ClearInventory();
                    sellFishSound = (AudioClip)Resources.Load("sell");
                    PlaySFX(sellFishSound);
                    RebuildShopContent();
                });
            }
        }

        private void BuildEngineCard(Engine eng, Engine currentEngine)
        {
            bool isEquipped = (currentEngine == eng);

            Image cardBorder = UITheme.CreatePanel("EngineCard_" + eng.displayName, _contentContainer,
                UITheme.CardSprite, UITheme.AccentSkyBlue);
            LayoutElement le = cardBorder.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 84f;
            le.flexibleWidth = 1f;

            Image cardBg = UITheme.CreatePanel("CardBg", cardBorder.transform,
                UITheme.CardSprite, UITheme.CardWhite);
            UITheme.StretchFill(cardBg.rectTransform, 2f, 2f, 2f, 2f);

            // Title + stats
            string title = $"<b>{eng.displayName}</b> ({eng.tier})";
            string stats = $"Speed: <b>{eng.maxSpeed:F1} m/s</b>  |  Accel: <b>{eng.acceleration:F1} m/s²</b>\nDecel: <b>{eng.deceleration:F1} m/s²</b>  |  Turn: <b>{eng.turnSpeed:F0}°/s</b>";

            TextMeshProUGUI titleLabel = UITheme.CreateLabel("Title", cardBg.transform,
                title, UITheme.SmallFontSize + 1f, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            titleLabel.richText = true;
            RectTransform titleRt = titleLabel.rectTransform;
            titleRt.anchorMin = new Vector2(0, 0.5f);
            titleRt.anchorMax = new Vector2(0.7f, 1);
            titleRt.offsetMin = new Vector2(16f, 0);
            titleRt.offsetMax = Vector2.zero;

            TextMeshProUGUI statsLabel = UITheme.CreateLabel("Stats", cardBg.transform,
                stats, UITheme.SmallFontSize - 2f, UITheme.TextMuted, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            statsLabel.richText = true;
            RectTransform statsRt = statsLabel.rectTransform;
            statsRt.anchorMin = new Vector2(0, 0);
            statsRt.anchorMax = new Vector2(0.7f, 0.5f);
            statsRt.offsetMin = new Vector2(16f, 0);
            statsRt.offsetMax = Vector2.zero;

            // Buy / Equipped button
            if (isEquipped)
            {
                Button btn = UITheme.CreateButton("EquippedBtn", cardBg.transform, "EQUIPPED",
                    UITheme.BackgroundMint, UITheme.TextBrown, UITheme.SmallFontSize, 120f, 38f);
                btn.interactable = false;
                RectTransform btnRt = btn.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(1, 0.5f);
                btnRt.anchorMax = new Vector2(1, 0.5f);
                btnRt.pivot = new Vector2(1, 0.5f);
                btnRt.anchoredPosition = new Vector2(-12f, 0);
            }
            else
            {
                bool canAfford = Wallet.Instance != null && Wallet.Instance.CanAfford(eng.cost);
                Button btn = UITheme.CreateButton("BuyBtn", cardBg.transform, $"Buy ${eng.cost}",
                    canAfford ? UITheme.Gold : UITheme.MutedButton,
                    UITheme.TextWhite, UITheme.SmallFontSize, 120f, 38f);
                btn.interactable = canAfford;
                RectTransform btnRt = btn.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(1, 0.5f);
                btnRt.anchorMax = new Vector2(1, 0.5f);
                btnRt.pivot = new Vector2(1, 0.5f);
                btnRt.anchoredPosition = new Vector2(-12f, 0);

                var capturedEng = eng;
                btn.onClick.AddListener(() =>
                {
                    if (Wallet.Instance != null && Wallet.Instance.TrySpendGold(capturedEng.cost))
                    {
                        var boat = BoatController.Instance;
                        if (boat != null) boat.Engine = capturedEng;
                        buyItemSound = (AudioClip)Resources.Load("buy");
                        PlaySFX(buyItemSound);
                        RebuildShopContent();
                    }
                });
            }
        }

        private void BuildRodCard(Rod rod, Rod currentRod)
        {
            bool isEquipped = (currentRod == rod);

            Image cardBorder = UITheme.CreatePanel("RodCard_" + rod.displayName, _contentContainer,
                UITheme.CardSprite, UITheme.AccentSkyBlue);
            LayoutElement le = cardBorder.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 84f;
            le.flexibleWidth = 1f;

            Image cardBg = UITheme.CreatePanel("CardBg", cardBorder.transform,
                UITheme.CardSprite, UITheme.CardWhite);
            UITheme.StretchFill(cardBg.rectTransform, 2f, 2f, 2f, 2f);

            // Title + stats
            string title = $"<b>{rod.displayName}</b> ({rod.tier})";
            string stats = $"Upgrades fishing gear.\nIncreases chance of catching higher tier fish!";

            TextMeshProUGUI titleLabel = UITheme.CreateLabel("Title", cardBg.transform,
                title, UITheme.SmallFontSize + 1f, UITheme.TextBrown, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            titleLabel.richText = true;
            RectTransform titleRt = titleLabel.rectTransform;
            titleRt.anchorMin = new Vector2(0, 0.5f);
            titleRt.anchorMax = new Vector2(0.7f, 1);
            titleRt.offsetMin = new Vector2(16f, 0);
            titleRt.offsetMax = Vector2.zero;

            TextMeshProUGUI statsLabel = UITheme.CreateLabel("Stats", cardBg.transform,
                stats, UITheme.SmallFontSize - 2f, UITheme.TextMuted, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            statsLabel.richText = true;
            RectTransform statsRt = statsLabel.rectTransform;
            statsRt.anchorMin = new Vector2(0, 0);
            statsRt.anchorMax = new Vector2(0.7f, 0.5f);
            statsRt.offsetMin = new Vector2(16f, 0);
            statsRt.offsetMax = Vector2.zero;

            // Buy / Equipped button
            if (isEquipped)
            {
                Button btn = UITheme.CreateButton("EquippedBtn", cardBg.transform, "EQUIPPED",
                    UITheme.BackgroundMint, UITheme.TextBrown, UITheme.SmallFontSize, 120f, 38f);
                btn.interactable = false;
                RectTransform btnRt = btn.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(1, 0.5f);
                btnRt.anchorMax = new Vector2(1, 0.5f);
                btnRt.pivot = new Vector2(1, 0.5f);
                btnRt.anchoredPosition = new Vector2(-12f, 0);
            }
            else
            {
                bool canAfford = Wallet.Instance != null && Wallet.Instance.CanAfford(rod.cost);
                Button btn = UITheme.CreateButton("BuyBtn", cardBg.transform, $"Buy ${rod.cost}",
                    canAfford ? UITheme.Gold : UITheme.MutedButton,
                    UITheme.TextWhite, UITheme.SmallFontSize, 120f, 38f);
                btn.interactable = canAfford;
                RectTransform btnRt = btn.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(1, 0.5f);
                btnRt.anchorMax = new Vector2(1, 0.5f);
                btnRt.pivot = new Vector2(1, 0.5f);
                btnRt.anchoredPosition = new Vector2(-12f, 0);

                var capturedRod = rod;
                btn.onClick.AddListener(() =>
                {
                    if (Wallet.Instance != null && Wallet.Instance.TrySpendGold(capturedRod.cost))
                    {
                        var player = PlayerController.Instance;
                        if (player != null) player.Rod = capturedRod;
                        PlaySFX(buyItemSound);
                        RebuildShopContent();
                    }
                });
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureShopUI()
        {
            if (Object.FindAnyObjectByType<ShopUI>() == null)
            {
                var uiObj = new GameObject("ShopUI");
                uiObj.AddComponent<ShopUI>();
            }
        }
    }
}
