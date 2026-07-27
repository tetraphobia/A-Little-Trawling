using System.Collections.Generic;
using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Entities;
using LittleTrawling.Vehicles;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Manages the Upgrade Shop UI overlay and equipment purchases.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        [Header("Available Engine Upgrades")]
        [SerializeField] private List<Engine> availableEngines = new List<Engine>();

        [Header("Available Rod Upgrades")]
        [SerializeField] private List<Rod> availableRods = new List<Rod>();

        private bool _isOpen;
        private bool _openedThisFrame;
        private Vector2 _scrollPos;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadDefaultCatalog();
        }

        private void LoadDefaultCatalog()
        {
            // Load Engine assets if unassigned
            if (availableEngines == null || availableEngines.Count == 0)
            {
                var engines = Resources.LoadAll<Engine>("Data/Engines");
                if (engines != null && engines.Length > 0)
                    availableEngines.AddRange(engines);
                else
                {
                    var allEngines = Resources.FindObjectsOfTypeAll<Engine>();
                    if (allEngines != null) availableEngines.AddRange(allEngines);
                }
            }

            // Load Rod assets if unassigned
            if (availableRods == null || availableRods.Count == 0)
            {
                var rods = Resources.LoadAll<Rod>("Data/Rods");
                if (rods != null && rods.Length > 0)
                    availableRods.AddRange(rods);
                else
                {
                    var allRods = Resources.FindObjectsOfTypeAll<Rod>();
                    if (allRods != null) availableRods.AddRange(allRods);
                }
            }
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
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= OnStateChanged;

            if (InputReader.Instance != null)
                InputReader.Instance.InteractPressed -= OnInteractPressed;

            if (Instance == this) Instance = null;
        }

        private void OnStateChanged(GameState state)
        {
            Debug.Log($"[ShopUI] OnStateChanged received state: {state}");
            bool wasOpen = _isOpen;
            _isOpen = (state == GameState.Shopping);
            if (_isOpen && !wasOpen)
            {
                _openedThisFrame = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void LateUpdate()
        {
            _openedThisFrame = false;
        }

        private void OnInteractPressed()
        {
            Debug.Log($"[ShopUI] OnInteractPressed event received. _isOpen: {_isOpen}, openedThisFrame: {_openedThisFrame}");
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

        private void OnGUI()
        {
            if (!_isOpen) return;

            // Create a styled GUI Shop Window
            int winWidth = Mathf.Min(650, Screen.width - 40);
            int winHeight = Mathf.Min(500, Screen.height - 40);
            Rect winRect = new Rect((Screen.width - winWidth) / 2f, (Screen.height - winHeight) / 2f, winWidth, winHeight);

            GUI.Box(winRect, "");

            GUILayout.BeginArea(new Rect(winRect.x + 15, winRect.y + 15, winRect.width - 30, winRect.height - 30));

            // Header & Gold Balance
            GUILayout.BeginHorizontal();
            GUILayout.Label("<size=22><b>⚓ BOAT & GEAR UPGRADE SHOP</b></size>", GUILayout.Height(35));
            GUILayout.FlexibleSpace();
            int currentGold = PlayerWallet.Instance != null ? PlayerWallet.Instance.CurrentGold : 0;
            GUILayout.Label($"<size=18><b>💰 Gold: ${currentGold}</b></size>", GUILayout.Height(35));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(winHeight - 120));

            // Engines
            GUILayout.Label("<size=16><b>🚀 Engines</b></size>");
            var boat = UnityEngine.Object.FindAnyObjectByType<BoatController>();
            Engine currentEngine = boat != null ? boat.Engine : null;

            foreach (var eng in availableEngines)
            {
                if (eng == null) continue;

                GUILayout.BeginHorizontal("box");
                bool isEquipped = (currentEngine == eng);
                string title = $"<b>{eng.displayName}</b> ({eng.tier} Tier)";
                string stats = $"Speed: x{eng.speedMultiplier:F1} | Turn: x{eng.maneuverabilityMultiplier:F1}";
                GUILayout.Label($"{title}\n<color=#aaaaaa>{stats}</color>");

                GUILayout.FlexibleSpace();

                if (isEquipped)
                {
                    GUI.enabled = false;
                    GUILayout.Button("EQUIPPED", GUILayout.Width(110), GUILayout.Height(35));
                    GUI.enabled = true;
                }
                else
                {
                    bool canAfford = PlayerWallet.Instance != null && PlayerWallet.Instance.CanAfford(eng.cost);
                    GUI.enabled = canAfford;
                    if (GUILayout.Button($"Buy ${eng.cost}", GUILayout.Width(110), GUILayout.Height(35)))
                    {
                        if (PlayerWallet.Instance != null && PlayerWallet.Instance.TrySpendGold(eng.cost))
                        {
                            if (boat != null) boat.Engine = eng;
                        }
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(15);

            // Fishing rods
            GUILayout.Label("<size=16><b>🎣 Fishing Rods</b></size>");
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Rod currentRod = player != null ? player.Rod : null;

            foreach (var rod in availableRods)
            {
                if (rod == null) continue;

                GUILayout.BeginHorizontal("box");
                bool isEquipped = (currentRod == rod);
                string title = $"<b>{rod.displayName}</b>";
                string stats = $"Tier: {rod.tier}";
                GUILayout.Label($"{title}\n<color=#aaaaaa>{stats}</color>");

                GUILayout.FlexibleSpace();

                if (isEquipped)
                {
                    GUI.enabled = false;
                    GUILayout.Button("EQUIPPED", GUILayout.Width(110), GUILayout.Height(35));
                    GUI.enabled = true;
                }
                else
                {
                    bool canAfford = PlayerWallet.Instance != null && PlayerWallet.Instance.CanAfford(rod.cost);
                    GUI.enabled = canAfford;
                    if (GUILayout.Button($"Buy ${rod.cost}", GUILayout.Width(110), GUILayout.Height(35)))
                    {
                        if (PlayerWallet.Instance != null && PlayerWallet.Instance.TrySpendGold(rod.cost))
                        {
                            if (player != null) player.Rod = rod;
                        }
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            // Exit Button
            if (GUILayout.Button("Close Shop", GUILayout.Height(35)))
            {
                CloseShop();
            }

            GUILayout.EndArea();
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
