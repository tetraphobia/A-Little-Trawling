using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LittleTrawling.Audio;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.Entities;
using LittleTrawling.Environment;
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
        [SerializeField] private float minCastDistance = 1.5f;
        [SerializeField] private float maxCastDistance = 7.0f;
        [SerializeField] private float maxChargeTime = 1.5f;

        [Header("Bite Settings")]
        [SerializeField] private float minBiteCheckInterval = 2.0f;
        [SerializeField] private float maxBiteCheckInterval = 4.0f;
        [SerializeField] private float biteProbability = 0.5f;
        [SerializeField] private int maxFailedBiteChecks = 3;
        [SerializeField] private float biteWindowDuration = 1.5f;

        [Header("Bobber Settings")]
        [Tooltip("The 2D sprite used for the fishing bobber.")]
        [SerializeField] private Sprite bobberSprite;

        [Header("Audio SFX")]
        [Tooltip("Sound played while holding [F] to charge cast distance.")]
        [SerializeField] private AudioClip castChargeSound;
        [Tooltip("Sound played when releasing [F] to hurl the bobber.")]
        [SerializeField] private AudioClip castReleaseSound;
        [Tooltip("Sound played when the bobber lands in the water.")]
        [SerializeField] private AudioClip bobberWaterLandingSound;
        [Tooltip("Sound played when a fish bites and bobber dips under.")]
        [SerializeField] private AudioClip fishBiteSound;
        [Tooltip("General sound played when a fish is successfully hooked/caught.")]
        [SerializeField] private AudioClip fishCaughtSuccessSound;
        [Tooltip("Sound played when a bite is missed or fish escapes.")]
        [SerializeField] private AudioClip fishEscapedSound;
        [Tooltip("Minimum pitch shift for fish catch audio.")]
        [SerializeField] private float minCatchPitch = 0.70f;
        [Tooltip("Maximum pitch shift for fish catch audio.")]
        [SerializeField] private float maxCatchPitch = 1.45f;
        [Tooltip("Volume multiplier for fish catch audio.")]
        [Range(0f, 1f)]
        [SerializeField] private float catchAudioVolume = 0.30f;

        public event System.Action OnFishingStarted;
        public event System.Action<Fish, float, float, int, LunkerStatus> OnFishCaught;
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

        private GameObject _previewBobberObject;
        private MeshRenderer _previewBodyRenderer;
        private MeshRenderer _previewRippleRenderer;

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

            DestroyPreviewBobber();
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

                        // AudioClip chargeClip = castChargeSound != null ? castChargeSound : ProceduralAudioSynthesizer.GetCastChargeSound();
                        
                        PlayAudioClip("cast charge");
                        // AudioSource.PlayClipAtPoint(chargeClip, transform.position, 1.0f);
                    }
                    break;

                case FishingState.Charging:
                    // Pressing again while charging cancels cast
                    CancelCast();
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

            DestroyPreviewBobber();

            // Instantly end fishing minigame if the bobber does not land in water!
            if (!IsPositionOnWater(_bobberTargetPosition))
            {
                OnMissedWaterLanding();
                return;
            }

            // AudioClip releaseClip = castReleaseSound != null ? castReleaseSound : ProceduralAudioSynthesizer.GetCastReleaseSound();
            // AudioSource.PlayClipAtPoint(releaseClip, origin, 10.0f);
            PlayAudioClip("cast release");
           

            StartCoroutine(AnimateBobberFlyArcRoutine(origin, _bobberTargetPosition));

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

        private void OnMissedWaterLanding()
        {
            DestroyBobber();
            CurrentState = FishingState.Idle;

            OnFishingFailed?.Invoke();

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetState(GameState.Walking);
            }
        }

        public void CancelCast()
        {
            if (CurrentState == FishingState.Idle) return;

            DestroyPreviewBobber();
            DestroyBobber();
            CurrentState = FishingState.Idle;

            var gm = GameManager.Instance;
            if (gm != null && gm.IsState(GameState.Fishing))
            {
                gm.SetState(GameState.Walking);
            }

            OnFishingFailed?.Invoke();
        }

        public bool IsPositionOnWater(Vector3 pos)
        {
            float waterY = OceanController.Instance != null ? OceanController.Instance.CurrentWaterHeight : 0f;

            Vector3 rayOrigin = new Vector3(pos.x, waterY + 50f, pos.z);
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 100f);

            bool isRejected = false;
            string rejectReason = "";

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;

                // Ignore player, boat, and bobbers
                if (hit.collider.CompareTag("Player")) continue;
                if (hit.collider.GetComponentInParent<BoatController>() != null) continue;
                if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;

                string colName = hit.collider.name.ToLower();
                if (colName.Contains("ocean") || colName.Contains("water") || colName.Contains("bobber")) continue;

                // Land or Dock is any surface elevated above ocean water level (y > waterY + 0.15f) or attached to a Dock
                if (hit.point.y > waterY + 0.15f || hit.collider.GetComponentInParent<Dock>() != null)
                {
                    isRejected = true;
                    rejectReason = $"Hit elevated land/dock surface '{hit.collider.name}' at y={hit.point.y:F2} (waterY+0.15={waterY + 0.15f:F2})";
                    break;
                }
            }

            return !isRejected;
        }

        private void UpdatePreviewBobber()
        {
            float ratio = ChargeRatio;
            float distance = Mathf.Lerp(minCastDistance, maxCastDistance, ratio);
            Vector3 origin = CalculateCastOrigin(out Vector3 forwardDir);
            Vector3 previewPos = origin + forwardDir * distance;
            previewPos.y = 0.05f;

            bool isWater = IsPositionOnWater(previewPos);

            if (_previewBobberObject == null)
            {
                SpawnPreviewBobber(previewPos, isWater);
            }
            else
            {
                _previewBobberObject.transform.position = previewPos;
                UpdatePreviewBobberColor(isWater);
            }
        }

        private void SpawnPreviewBobber(Vector3 position, bool isWater)
        {
            DestroyPreviewBobber();

            _previewBobberObject = new GameObject("PreviewFishingBobber");
            _previewBobberObject.transform.position = position;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "PreviewBobberBody";
            sphere.transform.SetParent(_previewBobberObject.transform, false);
            sphere.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            sphere.transform.localPosition = new Vector3(0f, 0.25f, 0f);

            var col1 = sphere.GetComponent<Collider>();
            if (col1 != null) Destroy(col1);

            _previewBodyRenderer = sphere.GetComponent<MeshRenderer>();
            if (_previewBodyRenderer != null)
            {
                _previewBodyRenderer.material = Create3DMaterial(Color.white);
                _previewBodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _previewBodyRenderer.receiveShadows = false;
            }

            var ripple = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ripple.name = "PreviewBobberRipple";
            ripple.transform.SetParent(_previewBobberObject.transform, false);
            ripple.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            ripple.transform.localScale = new Vector3(1.4f, 0.005f, 1.4f);

            var col2 = ripple.GetComponent<Collider>();
            if (col2 != null) Destroy(col2);

            _previewRippleRenderer = ripple.GetComponent<MeshRenderer>();
            if (_previewRippleRenderer != null)
            {
                _previewRippleRenderer.material = Create3DMaterial(Color.white);
                _previewRippleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _previewRippleRenderer.receiveShadows = false;
            }

            UpdatePreviewBobberColor(isWater);
        }

        private void UpdatePreviewBobberColor(bool isWater)
        {
            Color bodyColor = isWater
                ? new Color(0.1f, 0.95f, 0.35f, 0.55f)
                : new Color(0.95f, 0.15f, 0.15f, 0.55f);

            Color rippleColor = isWater
                ? new Color(0.1f, 0.95f, 0.35f, 0.35f)
                : new Color(0.95f, 0.15f, 0.15f, 0.35f);

            if (_previewBodyRenderer != null && _previewBodyRenderer.material != null)
                _previewBodyRenderer.material.color = bodyColor;

            if (_previewRippleRenderer != null && _previewRippleRenderer.material != null)
                _previewRippleRenderer.material.color = rippleColor;
        }

        private void DestroyPreviewBobber()
        {
            if (_previewBobberObject != null)
            {
                Destroy(_previewBobberObject);
                _previewBobberObject = null;
                _previewBodyRenderer = null;
                _previewRippleRenderer = null;
            }
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

                if (_bobberObject != null)
                {
                    PlayAudioClip("fish bite");
                }
            }
            else
            {
                ScheduleNextBiteCheck();
            }
        }

        private void OnBiteExpired()
        {
            if (_bobberObject != null)
            {
                PlayAudioClip("fish escaped");
            }

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
            var player = PlayerController.Instance;
            if (player != null)
            {
                forward = player.transform.forward;
                forward.y = 0f;
                forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
                return player.transform.position;
            }

            var boat = BoatController.Instance;
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

        private static Shader Get3DShader()
        {
            return Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Legacy Shaders/Diffuse")
                ?? Shader.Find("Sprites/Default");
        }

        private static Material Create3DMaterial(Color color)
        {
            Shader shader = Get3DShader();
            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
            return mat;
        }

        private Sprite GetBobberSprite()
        {
            if (bobberSprite != null) return bobberSprite;

            bobberSprite = Resources.Load<Sprite>("Sprites/Bobber");
            return bobberSprite;
        }

        private bool _isBobberFlyingArc;

        private System.Collections.IEnumerator AnimateBobberFlyArcRoutine(Vector3 startPos, Vector3 targetPos)
        {
            _isBobberFlyingArc = true;
            SpawnBobber(startPos, playLandingSound: false);

            float distance = Vector3.Distance(startPos, targetPos);
            float duration = Mathf.Clamp(distance * 0.12f, 0.45f, 0.75f);
            float maxArcHeight = Mathf.Clamp(distance * 0.35f, 1.2f, 3.8f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                float arcY = Mathf.Sin(t * Mathf.PI) * maxArcHeight;
                currentPos.y += arcY;

                if (_bobberObject != null)
                {
                    _bobberObject.transform.position = currentPos;
                    if (Camera.main != null)
                    {
                        _bobberObject.transform.rotation = Camera.main.transform.rotation;
                    }
                }

                yield return null;
            }

            _isBobberFlyingArc = false;
            if (_bobberObject != null)
            {
                _bobberObject.transform.position = targetPos;
                bobberWaterLandingSound = (AudioClip) Resources.Load("bobber");
                if (bobberWaterLandingSound != null)
                {
                    AudioSource.PlayClipAtPoint(bobberWaterLandingSound, targetPos, 1.0f);
                }
            }
        }

        private void SpawnBobber(Vector3 position, bool playLandingSound = true)
        {
            DestroyBobber();

            _bobberObject = new GameObject("FishingBobber");
            _bobberObject.transform.position = position;

            if (playLandingSound && bobberWaterLandingSound != null)
            {
                AudioSource.PlayClipAtPoint(bobberWaterLandingSound, position, 1.0f);
            }

            var spriteObj = new GameObject("BobberSprite");
            spriteObj.transform.SetParent(_bobberObject.transform, false);
            spriteObj.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

            var sr = spriteObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetBobberSprite();
            sr.sortingOrder = 50;
            if (sr.material != null && sr.material.HasProperty("_Cull"))
            {
                sr.material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }
        }

        private void AnimateBobber()
        {
            if (_bobberObject == null || _isBobberFlyingArc) return;

            float waterY = OceanController.Instance != null ? OceanController.Instance.CurrentWaterHeight : 0f;
            float bobbingOffset = 0f;

            if (CurrentState == FishingState.BiteActive)
            {
                bobbingOffset = -0.18f + Mathf.Sin(Time.time * 28f) * 0.06f;
            }
            else
            {
                bobbingOffset = 0.05f + Mathf.Sin(Time.time * 3.5f) * 0.04f;
            }

            Vector3 pos = _bobberTargetPosition;
            pos.y = waterY + 0.20f + bobbingOffset;
            _bobberObject.transform.position = pos;

            if (Camera.main != null)
            {
                _bobberObject.transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void DestroyBobber()
        {
            _isBobberFlyingArc = false;
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

            var loaded = Resources.LoadAll<Fish>("Data/Fish");
            if (loaded != null)
            {
                foreach (var f in loaded)
                {
                    if (f != null && !fishPool.Contains(f))
                    {
                        fishPool.Add(f);
                    }
                }
            }

            if (fishPool.Count == 0)
            {
                EnsureFallbackFishCatalog();
            }
        }

        private void EnsureFallbackFishCatalog()
        {
            var trout = ScriptableObject.CreateInstance<Fish>();
            trout.displayName = "Rainbow Trout";
            trout.tier = FishTier.Tier0;
            trout.minSize = 0.3f;
            trout.maxSize = 0.7f;
            trout.minWeight = 0.5f;
            trout.maxWeight = 2.5f;
            trout.baseValue = 25;

            var salmon = ScriptableObject.CreateInstance<Fish>();
            salmon.displayName = "Atlantic Salmon";
            salmon.tier = FishTier.Tier1;
            salmon.minSize = 0.5f;
            salmon.maxSize = 1.1f;
            salmon.minWeight = 2.0f;
            salmon.maxWeight = 6.0f;
            salmon.baseValue = 44;

            var bass = ScriptableObject.CreateInstance<Fish>();
            bass.displayName = "Largemouth Bass";
            bass.tier = FishTier.Tier2;
            bass.minSize = 0.25f;
            bass.maxSize = 0.6f;
            bass.minWeight = 0.4f;
            bass.maxWeight = 2.0f;
            bass.baseValue = 19;

            var tuna = ScriptableObject.CreateInstance<Fish>();
            tuna.displayName = "Bluefin Tuna";
            tuna.tier = FishTier.Tier3;
            tuna.minSize = 0.8f;
            tuna.maxSize = 1.8f;
            tuna.minWeight = 5.0f;
            tuna.maxWeight = 15.0f;
            tuna.baseValue = 100;

            fishPool.Add(trout);
            fishPool.Add(salmon);
            fishPool.Add(bass);
            fishPool.Add(tuna);
        }

        private void ExecuteMinigameSuccess()
        {
            Vector3 bobberPos = _bobberTargetPosition;
            DestroyBobber();
            CurrentState = FishingState.Idle;

            if (fishCaughtSuccessSound != null)
            {
                PlayPitchShiftedSFX(fishCaughtSuccessSound, bobberPos);
            }

            Fish species = SelectWeightedFishByRodTier(bobberPos);
            float size = species.RollSize();
            float weight = species.RollWeight();
            float sizeCm = size * 100f;

            int goldEarned = Mathf.RoundToInt(species.baseValue * (size / Mathf.Max(0.01f, species.minSize)));

            bool isFirstCatch = InventoryManager.Instance != null && !InventoryManager.Instance.HasDiscovered(species);
            if (isFirstCatch)
            {
                int firstCatchBonus = Mathf.Max(15, Mathf.RoundToInt(goldEarned * 0.5f));
                goldEarned += firstCatchBonus;
            }

            LunkerStatus lunkerStatus = LunkerStatus.Normal;
            float lunkerRoll = Random.value;
            if (lunkerRoll <= 0.01f)
            {
                lunkerStatus = LunkerStatus.MegaLunker;
                sizeCm *= 6f;
                weight *= 6f;
                goldEarned *= 6;
            }
            else if (lunkerRoll <= 0.11f)
            {
                lunkerStatus = LunkerStatus.Lunker;
                sizeCm *= 3f;
                weight *= 3f;
                goldEarned *= 3;
            }

            StartCoroutine(AnimateFishCatchArc(species, bobberPos, sizeCm, weight, goldEarned, lunkerStatus));
        }

        public float GetWaterDepthAt(Vector3 pos)
        {
            float waterY = OceanController.Instance != null ? OceanController.Instance.CurrentWaterHeight : 0f;
            Vector3 rayOrigin = new Vector3(pos.x, waterY + 50f, pos.z);
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 150f);

            float lowestHitY = waterY;
            bool hitSeabed = false;

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (hit.collider.CompareTag("Player")) continue;
                if (hit.collider.name.ToLower().Contains("ocean") || hit.collider.name.ToLower().Contains("water")) continue;

                if (hit.point.y < lowestHitY)
                {
                    lowestHitY = hit.point.y;
                    hitSeabed = true;
                }
            }

            if (!hitSeabed)
            {
                float distFromCenter = Vector3.Distance(new Vector3(pos.x, 0, pos.z), Vector3.zero);
                return Mathf.Clamp(distFromCenter * 0.25f, 1f, 25f);
            }

            return Mathf.Max(0f, waterY - lowestHitY);
        }

        private static int GetMaxTierForDepth(float depth)
        {
            if (depth < 1.5f) return 0; // Shallow dock/shore
            if (depth < 4.0f) return 1; // Near-shore ocean
            if (depth < 8.0f) return 2; // Deep water
            return 3;                   // Abyssal ocean (8m+)
        }

        private Fish SelectWeightedFishByRodTier(Vector3 castPos)
        {
            if (fishPool == null || fishPool.Count == 0)
            {
                LoadFishCatalog();
            }

            var player = PlayerController.Instance;
            Rod rod = player != null ? player.Rod : null;
            int rodTier = Mathf.Clamp((int)(rod != null ? rod.tier : RodTier.Tier0), 0, 3);

            float depth = GetWaterDepthAt(castPos);
            int depthTier = GetMaxTierForDepth(depth);

            float[] probs = GetFishTierProbabilitiesForDepthAndRod(depthTier, rodTier);

            float totalWeight = 0f;
            for (int t = 0; t <= depthTier; t++)
            {
                totalWeight += probs[t];
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            int targetTier = 0;

            for (int t = 0; t <= depthTier; t++)
            {
                cumulative += probs[t];
                if (roll <= cumulative)
                {
                    targetTier = t;
                    break;
                }
            }

            List<Fish> eligible = fishPool.FindAll(f => f != null && (int)f.tier == targetTier);

            if (eligible.Count == 0)
            {
                eligible = fishPool.FindAll(f => f != null && (int)f.tier <= depthTier);
            }
            if (eligible.Count == 0)
            {
                eligible.AddRange(fishPool);
            }

            int randomIndex = Random.Range(0, eligible.Count);
            return eligible[randomIndex];
        }

        private static float[] GetFishTierProbabilitiesForDepthAndRod(int depthTier, int rodTier)
        {
            float[] probs = depthTier switch
            {
                0 => new float[] { 1.00f, 0.00f, 0.00f, 0.00f },
                1 => new float[] { 0.25f, 0.75f, 0.00f, 0.00f },
                2 => new float[] { 0.05f, 0.25f, 0.70f, 0.00f },
                3 => new float[] { 0.02f, 0.08f, 0.40f, 0.50f },
                _ => new float[] { 1.00f, 0.00f, 0.00f, 0.00f }
            };

            if (rodTier > 0 && depthTier > 0)
            {
                float bonus = rodTier * 0.10f;
                probs[depthTier] += bonus;
                probs[0] = Mathf.Max(0.01f, probs[0] - bonus);
            }

            return probs;
        }

        private System.Collections.IEnumerator AnimateFishCatchArc(Fish species, Vector3 startPos, float sizeCm, float weight, int goldEarned, LunkerStatus lunkerStatus = LunkerStatus.Normal)
        {
            Vector3 origin = CalculateCastOrigin(out _);
            Vector3 targetPos = origin + Vector3.up * 1.2f;

            GameObject flyingFish = new GameObject($"FlyingFish_{species.displayName}");
            flyingFish.transform.position = startPos;

            SpriteRenderer sr = flyingFish.AddComponent<SpriteRenderer>();
            if (species != null && species.sprite != null)
            {
                sr.sprite = species.sprite;
            }
            else
            {
                sr.sprite = CreateFallbackFishSprite();
            }

            float scaleMultiplier = Mathf.Clamp(sizeCm / 100f, 0.2f, 0.6f);
            flyingFish.transform.localScale = Vector3.one * scaleMultiplier;

            float duration = 0.75f;
            float elapsed = 0f;
            float distance = Vector3.Distance(startPos, targetPos);
            float maxArcHeight = Mathf.Clamp(distance * 0.45f, 2.0f, 5.0f);

            AudioSource catchAudio = null;
            if (species != null && species.catchSound != null)
            {
                catchAudio = flyingFish.AddComponent<AudioSource>();
                catchAudio.clip = species.catchSound;
                catchAudio.spatialBlend = 1.0f; // 3D directional audio emitting from the flying fish
                catchAudio.minDistance = 1.0f;
                catchAudio.maxDistance = 50.0f;
                catchAudio.rolloffMode = AudioRolloffMode.Linear;
                float catchVol = VolumeManager.Instance != null ? VolumeManager.Instance.FishCatchVolume : catchAudioVolume;
                catchAudio.volume = VolumeManager.Instance != null ? VolumeManager.Instance.GetEffectiveVolume(catchVol, AudioCategory.SFX) : catchVol;
                catchAudio.pitch = Random.Range(minCatchPitch, maxCatchPitch);
                catchAudio.Play();
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                float arcY = Mathf.Sin(t * Mathf.PI) * maxArcHeight;
                currentPos.y += arcY;

                flyingFish.transform.position = currentPos;

                if (Camera.main != null)
                {
                    float wiggleZ = Mathf.Sin(t * 24f) * 18f;
                    flyingFish.transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up)
                                                  * Quaternion.Euler(0f, 0f, wiggleZ);
                }

                yield return null;
            }

            if (catchAudio != null && catchAudio.isPlaying)
            {
                catchAudio.Stop();
            }

            Destroy(flyingFish);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetState(GameState.Walking);
            }

            OnFishCaught?.Invoke(species, sizeCm, weight, goldEarned, lunkerStatus);
            OnFishingCompleted?.Invoke();
        }

        private static Sprite _fallbackSprite;

        private Sprite CreateFallbackFishSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;

            int width = 64;
            int height = 32;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            Color bodyColor = new Color(0.95f, 0.5f, 0.15f, 1.0f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (float)x / width;
                    float ny = (float)y / height - 0.5f;

                    bool inBody = (Mathf.Pow(nx - 0.45f, 2) / 0.12f + Mathf.Pow(ny, 2) / 0.12f) <= 1.0f;
                    bool inTail = nx > 0.75f && Mathf.Abs(ny) <= (nx - 0.75f) * 1.5f;

                    if (inBody || inTail)
                    {
                        tex.SetPixel(x, y, bodyColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                }
            }

            tex.Apply();
            _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            return _fallbackSprite;
        }

        private void PlayPitchShiftedSFX(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;
            float baseVol = VolumeManager.Instance != null ? VolumeManager.Instance.FishCatchVolume : catchAudioVolume;
            if (VolumeManager.Instance != null)
            {
                VolumeManager.Instance.PlayPitchShiftedSFX(clip, position, minCatchPitch, maxCatchPitch, baseVol, AudioCategory.SFX);
            }
            else
            {
                GameObject tempGO = new GameObject("TempAudio_PitchShifted");
                tempGO.transform.position = position;
                AudioSource source = tempGO.AddComponent<AudioSource>();
                source.clip = clip;
                source.spatialBlend = 0f;
                source.volume = baseVol;
                source.pitch = Random.Range(minCatchPitch, maxCatchPitch);
                source.Play();
                Destroy(tempGO, clip.length / Mathf.Max(0.1f, source.pitch) + 0.1f);
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

        private void PlayAudioClip(string path)
        {
            AudioClip clip = (AudioClip)Resources.Load(path);
            if (clip == null) return;
            float castVol = VolumeManager.Instance != null ? VolumeManager.Instance.CastVolume : 0.7f;
            if (VolumeManager.Instance != null)
            {
                VolumeManager.Instance.PlayClipAtPoint(clip, transform.position, castVol, AudioCategory.SFX);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, transform.position, castVol);
            }
        }
    }

    
}
