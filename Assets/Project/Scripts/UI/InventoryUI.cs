using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Systems;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Displays a scrollable OnGUI inventory modal when pressing 'I'.
    /// Shows fish sprite, name, description, size, weight, and sell price.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        private bool _isOpen;
        private Vector2 _scrollPosition;
        private GUIStyle _cardStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _descStyle;
        private GUIStyle _priceStyle;
        private GUIStyle _emptyStyle;

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
                InputReader.Instance.InventoryPressed += ToggleInventory;
                InputReader.Instance.ClosePressed += OnClosePressed;
            }
        }

        private void OnDestroy()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.InventoryPressed -= ToggleInventory;
                InputReader.Instance.ClosePressed -= OnClosePressed;
            }
            if (Instance == this) Instance = null;
        }

        private void ToggleInventory()
        {
            _isOpen = !_isOpen;
        }

        private void OnClosePressed()
        {
            if (_isOpen)
            {
                _isOpen = false;
            }
        }

        private void OnGUI()
        {
            if (!_isOpen) return;

            InitStyles();

            int windowWidth = 640;
            int windowHeight = 520;
            Rect windowRect = new Rect((Screen.width - windowWidth) / 2f, (Screen.height - windowHeight) / 2f, windowWidth, windowHeight);

            // Modal Background Box
            GUI.Box(windowRect, "", GUI.skin.box);
            GUI.Box(windowRect, "", GUI.skin.window);

            var mgr = InventoryManager.Instance;
            var items = mgr != null ? mgr.Items : null;
            int itemCount = items != null ? items.Count : 0;
            int totalValue = mgr != null ? mgr.CalculateTotalValue() : 0;

            // Header Section
            Rect headerRect = new Rect(windowRect.x + 15, windowRect.y + 12, windowRect.width - 30, 40);
            GUI.Label(headerRect, $"<size=20><b>FISH INVENTORY</b></size> <size=13><color=#aaaaaa>({itemCount} Caught  |  Total Value: <color=yellow>${totalValue} Gold</color>)</color></size>", _headerStyle);

            // Close Button [X]
            Rect closeRect = new Rect(windowRect.x + windowRect.width - 45, windowRect.y + 12, 30, 28);
            if (GUI.Button(closeRect, "<b>X</b>"))
            {
                _isOpen = false;
            }

            // Separator Line
            GUI.Box(new Rect(windowRect.x + 15, windowRect.y + 54, windowRect.width - 30, 2), "");

            // Empty Inventory State
            if (itemCount == 0)
            {
                Rect emptyRect = new Rect(windowRect.x + 20, windowRect.y + 150, windowRect.width - 40, 160);
                GUI.Label(emptyRect, "<size=18><b>Your inventory is empty!</b></size>\n\n<size=14><color=#bbbbbb>Hold and release <b>[F]</b> near ocean water to catch fish.</color></size>", _emptyStyle);
                return;
            }

            // Scrollable List View
            Rect scrollOuterRect = new Rect(windowRect.x + 15, windowRect.y + 64, windowRect.width - 30, windowHeight - 80);
            int cardHeight = 105;
            int spacing = 8;
            int contentHeight = itemCount * (cardHeight + spacing);
            Rect contentRect = new Rect(0, 0, scrollOuterRect.width - 24, contentHeight);

            _scrollPosition = GUI.BeginScrollView(scrollOuterRect, _scrollPosition, contentRect);

            for (int i = 0; i < itemCount; i++)
            {
                var item = items[i];
                if (item == null) continue;

                Rect cardRect = new Rect(0, i * (cardHeight + spacing), contentRect.width, cardHeight);
                GUI.Box(cardRect, "", _cardStyle);

                // Fish 2D Sprite Preview
                Rect spriteRect = new Rect(cardRect.x + 12, cardRect.y + 12, 80, 80);
                DrawFishSprite(spriteRect, item.species != null ? item.species.sprite : null);

                // Fish Info Column
                float textLeft = cardRect.x + 104;
                float textWidth = cardRect.width - 220;

                string rarityColor = GetRarityColorHex(item.species != null ? item.species.rarity : FishRarity.Common);
                string fishName = item.species != null ? item.species.displayName : "Unknown Fish";
                string rarityText = item.species != null ? item.species.rarity.ToString() : "Common";

                Rect titleRect = new Rect(textLeft, cardRect.y + 8, textWidth, 26);
                GUI.Label(titleRect, $"<size=15><b>{fishName}</b></size>  <size=12><color={rarityColor}><b>[{rarityText}]</b></color></size>", _titleStyle);

                string description = item.species != null ? item.species.description : "No description available.";
                Rect descRect = new Rect(textLeft, cardRect.y + 34, textWidth, 38);
                GUI.Label(descRect, $"<size=12><color=#cccccc><i>\"{description}\"</i></color></size>", _descStyle);

                Rect statsRect = new Rect(textLeft, cardRect.y + 74, textWidth, 22);
                GUI.Label(statsRect, $"<size=12><color=#99d6ff>Size: <b>{item.sizeCm:F1} cm</b>  |  Weight: <b>{item.weightKg:F2} kg</b></color></size>", _titleStyle);

                // Sell Price Badge Column (Right Aligned)
                Rect priceRect = new Rect(cardRect.x + cardRect.width - 110, cardRect.y + 35, 100, 34);
                GUI.Box(priceRect, "");
                GUI.Label(priceRect, $"<size=14><color=yellow><b>${item.sellPrice}</b></color></size>", _priceStyle);
            }

            GUI.EndScrollView();
        }

        private void DrawFishSprite(Rect rect, Sprite sprite)
        {
            GUI.Box(rect, "");
            if (sprite != null && sprite.texture != null)
            {
                float targetWidth = 70f;
                float aspect = sprite.rect.height / Mathf.Max(1f, sprite.rect.width);
                float targetHeight = targetWidth * aspect;

                if (targetHeight > rect.height - 8f)
                {
                    targetHeight = rect.height - 8f;
                    targetWidth = targetHeight / Mathf.Max(0.01f, aspect);
                }

                Rect drawRect = new Rect(
                    rect.x + (rect.width - targetWidth) / 2f,
                    rect.y + (rect.height - targetHeight) / 2f,
                    targetWidth,
                    targetHeight
                );

                Rect uv = new Rect(
                    sprite.rect.x / sprite.texture.width,
                    sprite.rect.y / sprite.texture.height,
                    sprite.rect.width / sprite.texture.width,
                    sprite.rect.height / sprite.texture.height
                );

                GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv);
            }
            else
            {
                GUI.Label(rect, "<size=13><color=#aaaaaa>NO SPRITE</color></size>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });
            }
        }

        private string GetRarityColorHex(FishRarity rarity)
        {
            return rarity switch
            {
                FishRarity.Common => "#55ff55",
                FishRarity.Uncommon => "#55aaff",
                FishRarity.Rare => "#ff55ff",
                _ => "#ffffff"
            };
        }

        private void InitStyles()
        {
            if (_cardStyle != null) return;

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(6, 6, 6, 6)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };

            _descStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                richText = true
            };

            _priceStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            _emptyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true
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
