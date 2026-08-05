using UnityEngine;

namespace LittleTrawling.Audio
{
    public enum AudioCategory
    {
        SFX,
        UI
    }

    /// <summary>
    /// Centralized Volume & Audio Manager for controlling master volume, category multipliers, and sound effect levels across the game.
    /// </summary>
    public class VolumeManager : MonoBehaviour
    {
        public static VolumeManager Instance { get; private set; }

        [Header("Master & Category Volume Sliders")]
        [Tooltip("Global master volume multiplier for all game audio.")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1.0f;

        [Tooltip("Volume multiplier for sound effects (movement, fishing, vehicles).")]
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1.0f;

        [Tooltip("Volume multiplier for user interface audio (menus, dialogues, choices).")]
        [Range(0f, 1f)]
        [SerializeField] private float uiVolume = 1.0f;

        [Header("Specific SFX Volume Controls")]
        [Tooltip("Volume scale for player jump grunts.")]
        [Range(0f, 1f)]
        [SerializeField] private float jumpVolume = 0.35f;

        [Tooltip("Volume scale for player footstep sounds.")]
        [Range(0f, 1f)]
        [SerializeField] private float footstepVolume = 0.50f;

        [Tooltip("Volume scale for flying fish catch sound effects.")]
        [Range(0f, 1f)]
        [SerializeField] private float fishCatchVolume = 0.30f;

        [Tooltip("Volume scale for catch celebration fanfares.")]
        [Range(0f, 1f)]
        [SerializeField] private float celebrationFanfareVolume = 0.70f;

        [Tooltip("Volume scale for casting charges and releases.")]
        [Range(0f, 1f)]
        [SerializeField] private float castVolume = 0.70f;

        [Tooltip("Volume scale for dialogue typewriter blip sounds.")]
        [Range(0f, 1f)]
        [SerializeField] private float dialogueBlipVolume = 0.35f;

        [Tooltip("Volume scale for UI window and button click sounds.")]
        [Range(0f, 1f)]
        [SerializeField] private float uiSoundVolume = 0.50f;

        [Tooltip("Volume scale for boat engine, enter, exit, and dock collision sounds.")]
        [Range(0f, 1f)]
        [SerializeField] private float boatVolume = 0.60f;

        public float MasterVolume { get => masterVolume; set => masterVolume = Mathf.Clamp01(value); }
        public float SfxVolume { get => sfxVolume; set => sfxVolume = Mathf.Clamp01(value); }
        public float UiVolume { get => uiVolume; set => uiVolume = Mathf.Clamp01(value); }

        public float JumpVolume { get => jumpVolume; set => jumpVolume = Mathf.Clamp01(value); }
        public float FootstepVolume { get => footstepVolume; set => footstepVolume = Mathf.Clamp01(value); }
        public float FishCatchVolume { get => fishCatchVolume; set => fishCatchVolume = Mathf.Clamp01(value); }
        public float CelebrationFanfareVolume { get => celebrationFanfareVolume; set => celebrationFanfareVolume = Mathf.Clamp01(value); }
        public float CastVolume { get => castVolume; set => castVolume = Mathf.Clamp01(value); }
        public float DialogueBlipVolume { get => dialogueBlipVolume; set => dialogueBlipVolume = Mathf.Clamp01(value); }
        public float UiSoundVolume { get => uiSoundVolume; set => uiSoundVolume = Mathf.Clamp01(value); }
        public float BoatVolume { get => boatVolume; set => boatVolume = Mathf.Clamp01(value); }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Computes the effective volume given a base clip volume scale and audio category.
        /// </summary>
        public float GetEffectiveVolume(float baseVolume = 1.0f, AudioCategory category = AudioCategory.SFX)
        {
            float categoryMult = (category == AudioCategory.UI) ? uiVolume : sfxVolume;
            return Mathf.Clamp01(baseVolume * masterVolume * categoryMult);
        }

        /// <summary>
        /// Plays a one-shot SFX on the provided AudioSource scaled by Master & Category volume settings.
        /// </summary>
        public void PlayOneShot(AudioSource source, AudioClip clip, float baseVolume = 1.0f, AudioCategory category = AudioCategory.SFX)
        {
            if (source == null || clip == null) return;
            float finalVolume = GetEffectiveVolume(baseVolume, category);
            source.PlayOneShot(clip, finalVolume);
        }

        /// <summary>
        /// Plays a clip at a 3D world position scaled by Master & Category volume settings.
        /// </summary>
        public void PlayClipAtPoint(AudioClip clip, Vector3 position, float baseVolume = 1.0f, AudioCategory category = AudioCategory.SFX)
        {
            if (clip == null) return;
            float finalVolume = GetEffectiveVolume(baseVolume, category);
            AudioSource.PlayClipAtPoint(clip, position, finalVolume);
        }

        /// <summary>
        /// Plays a pitch-shifted sound effect at a position scaled by Master & Category volume settings.
        /// </summary>
        public void PlayPitchShiftedSFX(AudioClip clip, Vector3 position, float minPitch = 0.8f, float maxPitch = 1.2f, float baseVolume = 1.0f, AudioCategory category = AudioCategory.SFX)
        {
            if (clip == null) return;
            GameObject tempGO = new GameObject("TempAudio_PitchShifted");
            tempGO.transform.position = position;
            AudioSource source = tempGO.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.volume = GetEffectiveVolume(baseVolume, category);
            source.pitch = Random.Range(minPitch, maxPitch);
            source.Play();
            Destroy(tempGO, clip.length / Mathf.Max(0.1f, source.pitch) + 0.1f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureVolumeManager()
        {
            if (Instance != null) return;

            var systemsObj = GameObject.Find("Systems");
            if (systemsObj != null)
            {
                Instance = systemsObj.GetComponent<VolumeManager>();
                if (Instance == null)
                {
                    Instance = systemsObj.AddComponent<VolumeManager>();
                }
            }
            else if (Object.FindAnyObjectByType<VolumeManager>() == null)
            {
                var mgrObj = new GameObject("VolumeManager");
                Instance = mgrObj.AddComponent<VolumeManager>();
            }
        }
    }
}
