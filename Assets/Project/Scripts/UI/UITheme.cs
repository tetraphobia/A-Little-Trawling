using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Centralized Animal Crossing: New Horizons–style theme utility.
    /// Provides color palette constants, runtime rounded-rect sprite generation,
    /// and helper builders for constructing themed uGUI elements programmatically.
    /// </summary>
    public static class UITheme
    {
        // ── Color Palette ──────────────────────────────────────────────
        // Pastel mint / sky blue with warm white cards and gold accents.

        /// <summary>Pastel mint screen-level background.</summary>
        public static readonly Color BackgroundMint = new Color32(200, 240, 232, 255);   // #C8F0E8

        /// <summary>Sky blue accent for secondary panels / highlights.</summary>
        public static readonly Color AccentSkyBlue = new Color32(168, 216, 234, 255);    // #A8D8EA

        /// <summary>Warm white card/panel fill.</summary>
        public static readonly Color CardWhite = new Color32(255, 248, 240, 255);        // #FFF8F0

        /// <summary>Warm gold used for borders, accents, and price badges.</summary>
        public static readonly Color Gold = new Color32(240, 192, 80, 255);              // #F0C050

        /// <summary>Soft brown primary text color.</summary>
        public static readonly Color TextBrown = new Color32(92, 64, 51, 255);           // #5C4033

        /// <summary>Muted tan for secondary/subtext.</summary>
        public static readonly Color TextMuted = new Color32(139, 125, 107, 255);        // #8B7D6B

        /// <summary>White text for badges and buttons.</summary>
        public static readonly Color TextWhite = Color.white;

        /// <summary>Semi-transparent dim overlay behind modals.</summary>
        public static readonly Color DimOverlay = new Color(0f, 0f, 0f, 0.35f);

        /// <summary>Leaf green for positive actions and common rarity.</summary>
        public static readonly Color LeafGreen = new Color32(107, 191, 107, 255);        // #6BBF6B

        /// <summary>Soft pink for rare rarity badges.</summary>
        public static readonly Color SoftPink = new Color32(232, 139, 232, 255);         // #E88BE8

        /// <summary>Disabled/muted button fill.</summary>
        public static readonly Color MutedButton = new Color32(200, 200, 190, 255);

        /// <summary>Gold text color (slightly brighter for readability).</summary>
        public static readonly Color TextGold = new Color32(200, 160, 40, 255);

        // ── Layout Constants ───────────────────────────────────────────

        public const float Padding = 24f;
        public const float CardHeight = 140f;
        public const float CardSpacing = 12f;
        public const float ButtonHeight = 54f;
        public const float HeaderFontSize = 34f;
        public const float TitleFontSize = 28f;
        public const float BodyFontSize = 22f;
        public const float SmallFontSize = 18f;
        public const int DefaultCornerRadius = 24;
        public const int ButtonCornerRadius = 16;
        public const int BadgeCornerRadius = 14;

        // ── Cached Sprites ─────────────────────────────────────────────

        private static Sprite _panelSprite;
        private static Sprite _cardSprite;
        private static Sprite _buttonSprite;
        private static Sprite _badgeSprite;
        private static Sprite _progressTrackSprite;
        private static Sprite _progressFillSprite;

        /// <summary>Large rounded-rect for modal panels (warm white).</summary>
        public static Sprite PanelSprite => _panelSprite ??= CreateRoundedRectSprite(64, 64, DefaultCornerRadius, Color.white);

        /// <summary>Medium rounded-rect for item cards.</summary>
        public static Sprite CardSprite => _cardSprite ??= CreateRoundedRectSprite(64, 64, DefaultCornerRadius, Color.white);

        /// <summary>Button-sized rounded-rect.</summary>
        public static Sprite ButtonSprite => _buttonSprite ??= CreateRoundedRectSprite(48, 48, ButtonCornerRadius, Color.white);

        /// <summary>Small rounded pill for badges.</summary>
        public static Sprite BadgeSprite => _badgeSprite ??= CreateRoundedRectSprite(32, 32, BadgeCornerRadius, Color.white);

        /// <summary>Very rounded pill for progress bar tracks.</summary>
        public static Sprite ProgressTrackSprite => _progressTrackSprite ??= CreateRoundedRectSprite(32, 32, 14, Color.white);

        /// <summary>Very rounded pill for progress bar fill.</summary>
        public static Sprite ProgressFillSprite => _progressFillSprite ??= CreateRoundedRectSprite(32, 32, 14, Color.white);

        // ── Rounded-Rect Sprite Generator ──────────────────────────────

        /// <summary>
        /// Creates a white rounded-rectangle Texture2D and wraps it as a 9-sliceable Sprite.
        /// Tint via Image.color to change the fill colour at runtime.
        /// </summary>
        public static Sprite CreateRoundedRectSprite(int width, int height, int radius, Color fillColor)
        {
            // Clamp radius so it doesn't exceed half the smallest dimension
            radius = Mathf.Min(radius, width / 2, height / 2);

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color transparent = new Color(0, 0, 0, 0);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsInsideRoundedRect(x, y, width, height, radius))
                    {
                        pixels[y * width + x] = fillColor;
                    }
                    else
                    {
                        // Anti-aliased edge: check distance from nearest rounded corner
                        float dist = DistanceFromRoundedRect(x, y, width, height, radius);
                        if (dist < 1.5f)
                        {
                            float alpha = Mathf.Clamp01(1.5f - dist);
                            pixels[y * width + x] = new Color(fillColor.r, fillColor.g, fillColor.b, fillColor.a * alpha);
                        }
                        else
                        {
                            pixels[y * width + x] = transparent;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            // 9-slice border insets = radius on each side
            Vector4 border = new Vector4(radius, radius, radius, radius);
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border
            );

            return sprite;
        }

        private static bool IsInsideRoundedRect(int x, int y, int w, int h, int r)
        {
            // Check if inside the four corner circles
            if (x < r && y < r)
                return (Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) <= r);
            if (x >= w - r && y < r)
                return (Vector2.Distance(new Vector2(x, y), new Vector2(w - r - 1, r)) <= r);
            if (x < r && y >= h - r)
                return (Vector2.Distance(new Vector2(x, y), new Vector2(r, h - r - 1)) <= r);
            if (x >= w - r && y >= h - r)
                return (Vector2.Distance(new Vector2(x, y), new Vector2(w - r - 1, h - r - 1)) <= r);

            return true; // Inside rectangular body
        }

        private static float DistanceFromRoundedRect(int x, int y, int w, int h, int r)
        {
            Vector2 p = new Vector2(x, y);

            if (x < r && y < r)
                return Vector2.Distance(p, new Vector2(r, r)) - r;
            if (x >= w - r && y < r)
                return Vector2.Distance(p, new Vector2(w - r - 1, r)) - r;
            if (x < r && y >= h - r)
                return Vector2.Distance(p, new Vector2(r, h - r - 1)) - r;
            if (x >= w - r && y >= h - r)
                return Vector2.Distance(p, new Vector2(w - r - 1, h - r - 1)) - r;

            return -1f; // Inside
        }

        // ── Canvas Factory ─────────────────────────────────────────────

        /// <summary>
        /// Creates a ScreenSpace-Overlay Canvas with a CanvasScaler and GraphicRaycaster.
        /// </summary>
        public static Canvas CreateScreenCanvas(string name, int sortOrder = 0)
        {
            EnsureEventSystem();

            GameObject canvasObj = new GameObject(name);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        /// <summary>
        /// Ensures a Unity EventSystem exists in the scene (required for uGUI button clicks).
        /// Compatible with Unity New Input System package.
        /// </summary>
        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
            }

            StandaloneInputModule legacyModule = es.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                Object.Destroy(legacyModule);
            }

            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        // ── Element Builders ───────────────────────────────────────────

        /// <summary>Creates a RectTransform GameObject parented under the given transform.</summary>
        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            return rt;
        }

        /// <summary>Creates an Image with a rounded-rect sprite, tinted to the given color.</summary>
        public static Image CreatePanel(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rt = CreateRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        /// <summary>Creates a TextMeshProUGUI label.</summary>
        public static TextMeshProUGUI CreateLabel(string name, Transform parent, string text,
            float fontSize, Color color, FontStyles fontStyle = FontStyles.Normal,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            RectTransform rt = CreateRect(name, parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = fontStyle;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>
        /// Creates a clickable button with rounded-rect background and TMP label.
        /// Returns the Button component. Access .transform to get children.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string label,
            Color bgColor, Color textColor, float fontSize = BodyFontSize,
            float width = 140f, float height = ButtonHeight)
        {
            // Background image
            Image bg = CreatePanel(name, parent, ButtonSprite, bgColor);
            RectTransform bgRt = bg.rectTransform;
            bgRt.sizeDelta = new Vector2(width, height);

            // Button component
            Button btn = bg.gameObject.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            btn.colors = cb;

            // Label
            TextMeshProUGUI tmp = CreateLabel(name + "_Label", bg.transform, label,
                fontSize, textColor, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform labelRt = tmp.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            return btn;
        }

        /// <summary>
        /// Creates a ScrollRect with a viewport mask and vertical content container.
        /// Returns (ScrollRect, contentTransform).
        /// </summary>
        public static (ScrollRect scrollRect, RectTransform content) CreateScrollView(
            string name, Transform parent)
        {
            // Scroll View root
            RectTransform scrollRt = CreateRect(name, parent);
            ScrollRect scrollRect = scrollRt.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 30f;

            // Viewport with mask
            Image viewportImg = CreatePanel(name + "_Viewport", scrollRt, PanelSprite, Color.white);
            viewportImg.color = new Color(1, 1, 1, 0.01f); // Nearly invisible but needed for mask
            RectTransform viewportRt = viewportImg.rectTransform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            Mask mask = viewportImg.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content container
            RectTransform contentRt = CreateRect(name + "_Content", viewportRt);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(0, 0);
            contentRt.offsetMax = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentRt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = CardSpacing;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentRt.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            return (scrollRect, contentRt);
        }

        /// <summary>
        /// Creates a full-screen semi-transparent dim overlay (for modal backgrounds).
        /// </summary>
        public static Image CreateDimOverlay(string name, Transform parent)
        {
            Image overlay = CreatePanel(name, parent, null, DimOverlay);
            RectTransform rt = overlay.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return overlay;
        }

        /// <summary>
        /// Stretches a RectTransform to fill its parent with optional inset margins.
        /// </summary>
        public static void StretchFill(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>
        /// Sets anchors and pivot to center, then applies size and position.
        /// </summary>
        public static void CenterWithSize(RectTransform rt, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Anchors a RectTransform to the top-right corner of its parent.
        /// </summary>
        public static void AnchorTopRight(RectTransform rt, float width, float height, float marginRight = 20f, float marginTop = 20f)
        {
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(-marginRight, -marginTop);
        }

        /// <summary>
        /// Anchors a RectTransform to the bottom-center of its parent.
        /// </summary>
        public static void AnchorBottomCenter(RectTransform rt, float width, float height, float marginBottom = 30f)
        {
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(0, marginBottom);
        }
    }
}
