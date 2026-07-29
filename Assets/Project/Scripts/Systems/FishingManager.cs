using System.Collections.Generic;
using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Entities;

namespace LittleTrawling.Systems
{
    /// <summary>
    /// Manages fishing interactions, minigame execution, fish reward calculations, and completion events.
    /// </summary>
    public class FishingManager : MonoBehaviour
    {
        public static FishingManager Instance { get; private set; }

        [Header("Fish Species Pool")]
        [SerializeField] private List<Fish> fishPool = new List<Fish>();

        public event System.Action<FishSchool> OnFishingStarted;
        public event System.Action<Fish, float, float, int> OnFishCaught;
        public event System.Action OnFishingCompleted;
        public event System.Action OnFishingFailed;

        private bool _enteredWalkingThisFrame;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadFishCatalog();
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.StateChanged += OnStateChanged;
            }

            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed += OnInteract;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnStateChanged;
            }

            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed -= OnInteract;
            }

            if (Instance == this) Instance = null;
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Walking)
            {
                _enteredWalkingThisFrame = true;
            }
        }

        private void LateUpdate()
        {
            _enteredWalkingThisFrame = false;
        }

        private void LoadFishCatalog()
        {
            if (fishPool == null) fishPool = new List<Fish>();
            fishPool.Clear();

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Fish");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var f = UnityEditor.AssetDatabase.LoadAssetAtPath<Fish>(path);
                if (f != null && !fishPool.Contains(f))
                {
                    fishPool.Add(f);
                }
            }
#else
            var loaded = Resources.FindObjectsOfTypeAll<Fish>();
            if (loaded != null) fishPool.AddRange(loaded);
#endif

            // Ensure fallback fish species if pool is empty
            if (fishPool.Count == 0)
            {
                EnsureFallbackFishCatalog();
            }
        }

        private void EnsureFallbackFishCatalog()
        {
            var trout = ScriptableObject.CreateInstance<Fish>();
            trout.displayName = "Rainbow Trout";
            trout.minSize = 0.3f;
            trout.maxSize = 0.7f;
            trout.minWeight = 0.5f;
            trout.maxWeight = 2.5f;
            trout.baseValue = 20;

            var salmon = ScriptableObject.CreateInstance<Fish>();
            salmon.displayName = "Atlantic Salmon";
            salmon.minSize = 0.5f;
            salmon.maxSize = 1.1f;
            salmon.minWeight = 2.0f;
            salmon.maxWeight = 6.0f;
            salmon.baseValue = 35;

            var bass = ScriptableObject.CreateInstance<Fish>();
            bass.displayName = "Largemouth Bass";
            bass.minSize = 0.25f;
            bass.maxSize = 0.6f;
            bass.minWeight = 0.4f;
            bass.maxWeight = 2.0f;
            bass.baseValue = 15;

            fishPool.Add(trout);
            fishPool.Add(salmon);
            fishPool.Add(bass);
        }

        private void OnInteract()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsState(GameState.Walking)) return;

            // Ignore interact press if state just transitioned to Walking in this exact frame
            if (_enteredWalkingThisFrame) return;

            // Find nearest valid fish school in range
            var schools = Object.FindObjectsByType<FishSchool>(FindObjectsSortMode.None);
            FishSchool targetSchool = null;
            float closestDist = float.MaxValue;

            foreach (var s in schools)
            {
                if (s != null && s.CanFish())
                {
                    float dist = Vector3.Distance(transform.position, s.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        targetSchool = s;
                    }
                }
            }

            if (targetSchool != null)
            {
                StartFishing(targetSchool);
            }
        }

        public void StartFishing(FishSchool school)
        {
            if (school == null || school.IsDepleted) return;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetState(GameState.Fishing);
            }

            OnFishingStarted?.Invoke(school);

            // Execute fishing completion
            ExecuteMinigameSuccess(school);
        }

        private void ExecuteMinigameSuccess(FishSchool school)
        {
            if (fishPool == null || fishPool.Count == 0)
            {
                LoadFishCatalog();
            }

            // Roll random fish species
            Fish species = fishPool[Random.Range(0, fishPool.Count)];
            float size = species.RollSize();
            float weight = species.RollWeight();
            float sizeCm = size * 100f; // Convert meters to cm for UI

            int goldEarned = Mathf.RoundToInt(species.baseValue * (size / Mathf.Max(0.01f, species.minSize)));

            // Award gold to Wallet
            if (Wallet.Instance != null)
            {
                Wallet.Instance.AddGold(goldEarned);
            }

            // Consume one fish from the school
            school.ConsumeFish();

            // Trigger catch UI & completion events
            OnFishCaught?.Invoke(species, sizeCm, weight, goldEarned);
            OnFishingCompleted?.Invoke();

            EndFishing(true);
        }

        private void EndFishing(bool success)
        {
            if (!success)
            {
                OnFishingFailed?.Invoke();
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetState(GameState.Walking);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureFishingManager()
        {
            if (Object.FindAnyObjectByType<FishingManager>() == null)
            {
                var mgrObj = new GameObject("FishingManager");
                mgrObj.AddComponent<FishingManager>();
            }
        }
    }
}
