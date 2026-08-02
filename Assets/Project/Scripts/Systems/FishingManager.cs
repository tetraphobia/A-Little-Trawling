using System.Collections.Generic;
using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Entities;
using LittleTrawling.Vehicles;

namespace LittleTrawling.Systems
{
    public enum FishingState
    {
        Idle,
        Charging,
        WaitingForBite,
        BiteActive
    }

    /// <summary>
    /// Manages charged casting, bobber simulation, bite interval checks, and the 1.5-second reaction window.
    /// </summary>
    public class FishingManager : MonoBehaviour
    {
        public static FishingManager Instance { get; private set; }

        [Header("Fish Species Pool")]
        [SerializeField] private List<Fish> fishPool = new List<Fish>();

        [Header("Cast Settings")]
        [SerializeField] private float minCastDistance = 4.0f;
        [SerializeField] private float maxCastDistance = 20.0f;
        [SerializeField] private float maxChargeTime = 1.5f;

        [Header("Bite Settings")]
        [SerializeField] private float minBiteCheckInterval = 2.0f;
        [SerializeField] private float maxBiteCheckInterval = 4.0f;
        [SerializeField] private float biteProbability = 0.5f;
        [SerializeField] private int maxFailedBiteChecks = 3;
        [SerializeField] private float biteWindowDuration = 1.5f;

        public event System.Action OnFishingStarted;
        public event System.Action<Fish, float, float, int> OnFishCaught;
        public event System.Action OnFishingCompleted;
        public event System.Action OnFishingFailed;

        public FishingState CurrentState { get; private set; } = FishingState.Idle;
        public float ChargeRatio => Mathf.Clamp01(_chargeTimer / Mathf.Max(0.01f, maxChargeTime));
        public float BiteTimeRemaining => _biteTimer;

        private float _chargeTimer;
        private float _nextBiteCheckTime;
        private int _failedBiteChecks;
        private float _biteTimer;

        private GameObject _bobberObject;
        private Vector3 _bobberTargetPosition;

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
            if (InputReader.Instance != null)
            {
                InputReader.Instance.FishPressed += OnFishPressed;
                InputReader.Instance.FishReleased += OnFishReleased;
            }
        }

        private void OnDestroy()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.FishPressed -= OnFishPressed;
                InputReader.Instance.FishReleased -= OnFishReleased;
            }

            DestroyBobber();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            switch (CurrentState)
            {
                case FishingState.Charging:
                    _chargeTimer += Time.deltaTime;
                    break;

                case FishingState.WaitingForBite:
                    AnimateBobber();
                    if (Time.time >= _nextBiteCheckTime)
                    {
                        EvaluateBiteCheck();
                    }
                    break;

                case FishingState.BiteActive:
                    AnimateBobber();
                    _biteTimer -= Time.deltaTime;
                    if (_biteTimer <= 0f)
                    {
                        OnBiteExpired();
                    }
                    break;
            }
        }

        private void OnFishPressed()
        {
            var gm = GameManager.Instance;

            switch (CurrentState)
            {
                case FishingState.Idle:
                    if (gm != null && gm.IsState(GameState.Walking))
                    {
                        CurrentState = FishingState.Charging;
                        _chargeTimer = 0f;
                    }
                    break;

                case FishingState.WaitingForBite:
                    // Premature press before bite window: Recall rod
                    RecallLine();
                    break;

                case FishingState.BiteActive:
                    // Success! Player pressed 'f' within 1.5s bite window
                    ExecuteMinigameSuccess();
                    break;
            }
        }

        private void OnFishReleased()
        {
            if (CurrentState != FishingState.Charging) return;

            // Calculate cast distance based on charge duration
            float ratio = ChargeRatio;
            float distance = Mathf.Lerp(minCastDistance, maxCastDistance, ratio);

            Vector3 origin = CalculateCastOrigin(out Vector3 forwardDir);
            _bobberTargetPosition = origin + forwardDir * distance;
            _bobberTargetPosition.y = 0.05f;

            SpawnBobber(_bobberTargetPosition);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetState(GameState.Fishing);
            }

            CurrentState = FishingState.WaitingForBite;
            _failedBiteChecks = 0;
            ScheduleNextBiteCheck();

            OnFishingStarted?.Invoke();
        }

        private void ScheduleNextBiteCheck()
        {
            float interval = Random.Range(minBiteCheckInterval, maxBiteCheckInterval);
            _nextBiteCheckTime = Time.time + interval;
        }

        private void EvaluateBiteCheck()
        {
            _failedBiteChecks++;

            bool isBite = (_failedBiteChecks >= maxFailedBiteChecks) || (Random.value <= biteProbability);
            if (isBite)
            {
                CurrentState = FishingState.BiteActive;
                _biteTimer = biteWindowDuration;
            }
            else
            {
                ScheduleNextBiteCheck();
            }
        }

        private void OnBiteExpired()
        {
            // Missed bite window
            DestroyBobber();
            CurrentState = FishingState.Idle;

            OnFishingFailed?.Invoke();

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetState(GameState.Walking);
            }
        }

        private void RecallLine()
        {
            DestroyBobber();
            CurrentState = FishingState.Idle;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetState(GameState.Walking);
            }
        }

        private Vector3 CalculateCastOrigin(out Vector3 forward)
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                forward = player.transform.forward;
                forward.y = 0f;
                forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
                return player.transform.position;
            }

            var boat = Object.FindAnyObjectByType<BoatController>();
            if (boat != null)
            {
                forward = boat.ForwardDirection;
                forward.y = 0f;
                forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
                return boat.transform.position;
            }

            if (Camera.main != null)
            {
                forward = Camera.main.transform.forward;
                forward.y = 0f;
                forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
                return Camera.main.transform.position;
            }

            forward = Vector3.forward;
            return Vector3.zero;
        }

        private void SpawnBobber(Vector3 position)
        {
            DestroyBobber();

            _bobberObject = new GameObject("FishingBobber");
            _bobberObject.transform.position = position;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Legacy Shaders/Diffuse");

            // Red & White Sphere Body
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "BobberBody";
            sphere.transform.SetParent(_bobberObject.transform, false);
            sphere.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            sphere.transform.localPosition = new Vector3(0f, 0.2f, 0f);

            var col1 = sphere.GetComponent<Collider>();
            if (col1 != null) Destroy(col1);

            var mr1 = sphere.GetComponent<MeshRenderer>();
            if (mr1 != null && shader != null)
            {
                mr1.material = new Material(shader);
                mr1.material.color = new Color(0.95f, 0.15f, 0.15f, 1.0f);
            }

            // Water Ripple Ring
            var ripple = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ripple.name = "BobberRipple";
            ripple.transform.SetParent(_bobberObject.transform, false);
            ripple.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            ripple.transform.localScale = new Vector3(1.4f, 0.005f, 1.4f);

            var col2 = ripple.GetComponent<Collider>();
            if (col2 != null) Destroy(col2);

            var mr2 = ripple.GetComponent<MeshRenderer>();
            if (mr2 != null && shader != null)
            {
                mr2.material = new Material(shader);
                mr2.material.color = new Color(0.2f, 0.75f, 0.95f, 0.45f);
            }
        }

        private void AnimateBobber()
        {
            if (_bobberObject == null) return;

            Vector3 pos = _bobberTargetPosition;
            if (CurrentState == FishingState.BiteActive)
            {
                // Tug underwater & rapid splash when biting
                pos.y = -0.2f + Mathf.Sin(Time.time * 28f) * 0.08f;
            }
            else
            {
                // Gentle floating wave bobber oscillation
                pos.y = 0.05f + Mathf.Sin(Time.time * 3.5f) * 0.04f;
            }
            _bobberObject.transform.position = pos;
        }

        private void DestroyBobber()
        {
            if (_bobberObject != null)
            {
                Destroy(_bobberObject);
                _bobberObject = null;
            }
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

        private void ExecuteMinigameSuccess()
        {
            DestroyBobber();
            CurrentState = FishingState.Idle;

            if (fishPool == null || fishPool.Count == 0)
            {
                LoadFishCatalog();
            }

            Fish species = fishPool[Random.Range(0, fishPool.Count)];
            float size = species.RollSize();
            float weight = species.RollWeight();
            float sizeCm = size * 100f;

            int goldEarned = Mathf.RoundToInt(species.baseValue * (size / Mathf.Max(0.01f, species.minSize)));

            if (Wallet.Instance != null)
            {
                Wallet.Instance.AddGold(goldEarned);
            }

            OnFishCaught?.Invoke(species, sizeCm, weight, goldEarned);
            OnFishingCompleted?.Invoke();

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
