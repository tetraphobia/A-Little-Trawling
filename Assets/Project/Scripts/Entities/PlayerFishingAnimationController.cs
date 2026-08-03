using System.Collections;
using UnityEngine;
using LittleTrawling.Systems;
using LittleTrawling.Data;

namespace LittleTrawling.Entities
{
    /// <summary>
    /// Handles frame-accurate fishing animations:
    /// - Player charges rod: frames 0-80 PlayerFishingCast (mapped to ChargeRatio)
    /// - Player throws rod: frames 80-256 PlayerFishingCast
    /// - Player waits for bite: loop PlayerFishingIdle animation
    /// - Player catches fish: frames 300-360 PlayerFishingCast
    /// </summary>
    public class PlayerFishingAnimationController : MonoBehaviour
    {
        public static PlayerFishingAnimationController Instance { get; private set; }

        [Header("Animation Clips")]
        [SerializeField] private AnimationClip fishingCastClip;
        [SerializeField] private AnimationClip fishingIdleClip;

        [Header("Frame Rate Settings")]
        [Tooltip("The frame rate of the exported animation clips (default 30 fps).")]
        [SerializeField] private float frameRate = 30f;

        private Animator _animator;
        private bool _isPlayingCustomFishingAnim;
        private bool _isPlayingCustomSequence;
        private Coroutine _activeAnimCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _animator = GetComponentInChildren<Animator>() ?? GetComponent<Animator>();
            LoadClipsIfNull();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LoadClipsIfNull()
        {
#if UNITY_EDITOR
            if (fishingCastClip == null)
            {
                fishingCastClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Project/Art/Models/Player/PlayerFishingCast.anim");
            }
            if (fishingIdleClip == null)
            {
                fishingIdleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Project/Art/Models/Player/PlayerFishingIdle.anim");
            }
#endif
        }

        private void Start()
        {
            var fm = FishingManager.Instance;
            if (fm != null)
            {
                fm.OnFishingStarted += OnCastReleased;
                fm.OnFishCaught += OnFishCaught;
            }
        }

        private void OnDisable()
        {
            var fm = FishingManager.Instance;
            if (fm != null)
            {
                fm.OnFishingStarted -= OnCastReleased;
                fm.OnFishCaught -= OnFishCaught;
            }
        }

        private void Update()
        {
            var fm = FishingManager.Instance;
            if (fm == null) return;

            // If a throw (frames 80-256) or catch (frames 300-360) sequence is playing, let it run in full!
            if (_isPlayingCustomSequence) return;

            // 1. Charge rod: frames 0 to 50 of PlayerFishingCast
            if (fm.CurrentState == FishingState.Charging)
            {
                _isPlayingCustomFishingAnim = true;
                if (_animator != null && _animator.enabled) _animator.enabled = false;

                float chargeRatio = fm.ChargeRatio;
                float startFrame = 0f;
                float endFrame = 50f;
                float currentFrame = Mathf.Lerp(startFrame, endFrame, chargeRatio);
                float sampleTime = currentFrame / Mathf.Max(1f, frameRate);

                if (fishingCastClip != null)
                {
                    fishingCastClip.SampleAnimation(gameObject, sampleTime);
                }
            }
            // 2. Wait for bite: loop PlayerFishingIdle animation
            else if (fm.CurrentState == FishingState.WaitingForBite || fm.CurrentState == FishingState.BiteActive)
            {
                _isPlayingCustomFishingAnim = true;
                if (_animator != null && _animator.enabled) _animator.enabled = false;

                if (fishingIdleClip != null && fishingIdleClip.length > 0f)
                {
                    float cycleTime = Time.time % fishingIdleClip.length;
                    fishingIdleClip.SampleAnimation(gameObject, cycleTime);
                }
            }
            // 3. Return control to standard locomotion Animator when not fishing
            else
            {
                if (_isPlayingCustomFishingAnim)
                {
                    _isPlayingCustomFishingAnim = false;
                    if (_animator != null && !_animator.enabled) _animator.enabled = true;
                }
            }
        }

        private void OnCastReleased()
        {
            // Throw rod: frames 50 to 256 of PlayerFishingCast
            PlayFrameRange(50f, 256f, () =>
            {
                _isPlayingCustomSequence = false;
            });
        }

        private void OnFishCaught(Fish species, float size, float weight, int value, LunkerStatus status)
        {
            // Catch fish: frames 300 to 360 of PlayerFishingCast
            PlayFrameRange(300f, 360f, () =>
            {
                _isPlayingCustomSequence = false;
            });
        }

        public void PlayFrameRange(float startFrame, float endFrame, System.Action onComplete = null)
        {
            if (_activeAnimCoroutine != null) StopCoroutine(_activeAnimCoroutine);
            _activeAnimCoroutine = StartCoroutine(PlayFrameRangeRoutine(startFrame, endFrame, onComplete));
        }

        private IEnumerator PlayFrameRangeRoutine(float startFrame, float endFrame, System.Action onComplete)
        {
            _isPlayingCustomSequence = true;
            _isPlayingCustomFishingAnim = true;
            if (_animator != null && _animator.enabled) _animator.enabled = false;

            float fps = Mathf.Max(1f, frameRate);
            float startTime = startFrame / fps;
            float endTime = endFrame / fps;
            float duration = Mathf.Max(0.01f, endTime - startTime);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentTime = Mathf.Lerp(startTime, endTime, t);

                if (fishingCastClip != null)
                {
                    fishingCastClip.SampleAnimation(gameObject, currentTime);
                }

                yield return null;
            }

            if (fishingCastClip != null)
            {
                fishingCastClip.SampleAnimation(gameObject, endTime);
            }

            onComplete?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsurePlayerFishingAnimationController()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null && player.GetComponent<PlayerFishingAnimationController>() == null)
            {
                player.gameObject.AddComponent<PlayerFishingAnimationController>();
            }
        }
    }
}
