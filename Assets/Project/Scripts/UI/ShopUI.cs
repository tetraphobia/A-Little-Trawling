using System.Collections.Generic;
using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Entities;
using LittleTrawling.Systems;
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

        [Header("Audio SFX")]
        [Tooltip("Sound played when selling fish.")]
        [SerializeField] private AudioClip sellFishSound;
        [Tooltip("Sound played when buying an item upgrade.")]
        [SerializeField] private AudioClip buyItemSound;

        private AudioSource _audioSource;

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            if (_audioSource == null)
            {
                _audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 0f;
            }
            _audioSource.PlayOneShot(clip);
        }

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
            if (availableEngines == null) availableEngines = new List<Engine>();
            if (availableRods == null) availableRods = new List<Rod>();

            availableEngines.Clear();
            availableRods.Clear();

#if UNITY_EDITOR
            // Search all Engine assets in Data subdirectories
            string[] engineGuids = UnityEditor.AssetDatabase.FindAssets("t:Engine");
            foreach (string guid in engineGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var eng = UnityEditor.AssetDatabase.LoadAssetAtPath<Engine>(path);
                if (eng != null && !availableEngines.Contains(eng))
                {
                    availableEngines.Add(eng);
                }
            }

            // Search all Rod assets in Data subdirectories
            string[] rodGuids = UnityEditor.AssetDatabase.FindAssets("t:Rod");
            foreach (string guid in rodGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var rod = UnityEditor.AssetDatabase.LoadAssetAtPath<Rod>(path);
                if (rod != null && !availableRods.Contains(rod))
                {
                    availableRods.Add(rod);
                }
            }
#else
            var engines = Resources.FindObjectsOfTypeAll<Engine>();
            if (engines != null) availableEngines.AddRange(engines);

            var rods = Resources.FindObjectsOfTypeAll<Rod>();
            if (rods != null) availableRods.AddRange(rods);
#endif

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

        private void OnGUI()
        {
            if (!_isOpen) return;

            // Create a GUI Shop Window
            int winWidth = Mathf.Min(650, Screen.width - 40);
            int winHeight = Mathf.Min(500, Screen.height - 40);
            Rect winRect = new Rect((Screen.width - winWidth) / 2f, (Screen.height - winHeight) / 2f, winWidth, winHeight);

            GUI.Box(winRect, "");

            GUILayout.BeginArea(new Rect(winRect.x + 15, winRect.y + 15, winRect.width - 30, winRect.height - 30));

            // Header & Gold Balance
            GUILayout.BeginHorizontal();
            GUILayout.Label("<size=22><b>Hey, fisherbird. Wanna buy something?</b></size>", GUILayout.Height(35));
            GUILayout.FlexibleSpace();
            int currentGold = Wallet.Instance != null ? Wallet.Instance.CurrentGold : 0;
            GUILayout.Label($"<size=18><b>Gold: ${currentGold}</b></size>", GUILayout.Height(35));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(winHeight - 120));

            // Sell Caught Fish
            GUILayout.Label("<size=16><b>Sell Caught Fish</b></size>");
            var invMgr = InventoryManager.Instance;
            var caughtItems = invMgr != null ? invMgr.Items : null;
            int caughtCount = caughtItems != null ? caughtItems.Count : 0;
            int totalFishValue = invMgr != null ? invMgr.CalculateTotalValue() : 0;

            GUILayout.BeginHorizontal("box");
            if (caughtCount == 0)
            {
                GUILayout.Label("<color=#aaaaaa><i>No fish in inventory to sell. Go fishing with [F]!</i></color>");
                GUILayout.FlexibleSpace();
                GUI.enabled = false;
                GUILayout.Button("Sell All Fish ($0)", GUILayout.Width(180), GUILayout.Height(35));
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label($"<b>Inventory: {caughtCount} Fish</b> (Total Value: <color=yellow>${totalFishValue}</color>)");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button($"Sell All Fish (${totalFishValue})", GUILayout.Width(180), GUILayout.Height(35)))
                {
                    if (Wallet.Instance != null)
                    {
                        Wallet.Instance.AddGold(totalFishValue);
                    }
                    if (invMgr != null) invMgr.ClearInventory();
                    PlaySFX(sellFishSound);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            // Engines
            GUILayout.Label("<size=16><b>Engines</b></size>");
            var boat = BoatController.Instance;
            Engine currentEngine = boat != null ? boat.Engine : null;

            foreach (var eng in availableEngines)
            {
                if (eng == null) continue;

                GUILayout.BeginHorizontal("box");
                bool isEquipped = (currentEngine == eng);
                string title = $"<b>{eng.displayName}</b> ({eng.tier} Tier)";
                string stats = $"Speed: {eng.maxSpeed:F1} m/s | Accel: {eng.acceleration:F1} m/s² | Decel: {eng.deceleration:F1} m/s² | Turn: {eng.turnSpeed:F0}°/s";
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
                    bool canAfford = Wallet.Instance != null && Wallet.Instance.CanAfford(eng.cost);
                    GUI.enabled = canAfford;
                    if (GUILayout.Button($"Buy ${eng.cost}", GUILayout.Width(110), GUILayout.Height(35)))
                    {
                        if (Wallet.Instance != null && Wallet.Instance.TrySpendGold(eng.cost))
                        {
                            if (boat != null) boat.Engine = eng;
                            PlaySFX(buyItemSound);
                        }
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(15);

            // Fishing rods
            GUILayout.Label("<size=16><b>Fishing Rods</b></size>");
            var player = PlayerController.Instance;
            Rod currentRod = player != null ? player.Rod : null;

            foreach (var rod in availableRods)
            {
                if (rod == null) continue;

                GUILayout.BeginHorizontal("box");
                bool isEquipped = (currentRod == rod);
                string title = $"<b>{rod.displayName}</b> ({rod.tier} Tier)";
                string stats = $"Unlocks: Tier {(int)rod.tier} Fish Species | High-Tier Catch Rate: +{((int)rod.tier * 30)}%";
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
                    bool canAfford = Wallet.Instance != null && Wallet.Instance.CanAfford(rod.cost);
                    GUI.enabled = canAfford;
                    if (GUILayout.Button($"Buy ${rod.cost}", GUILayout.Width(110), GUILayout.Height(35)))
                    {
                        if (Wallet.Instance != null && Wallet.Instance.TrySpendGold(rod.cost))
                        {
                            if (player != null) player.Rod = rod;
                            PlaySFX(buyItemSound);
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
