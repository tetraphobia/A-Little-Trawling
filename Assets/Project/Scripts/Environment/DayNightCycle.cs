using UnityEngine;

namespace LittleTrawling.Environment
{
    /// <summary>
    /// Controls the day/night cycle
    /// </summary>
    [ExecuteAlways]
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }

        [Header("Cycle Configuration")]
        [Tooltip("Duration of a full day/night cycle in seconds.")]
        [SerializeField] private float cycleDuration = 300.0f;

        [Tooltip("Target MeshRenderer of the SkyDome object.")]
        [SerializeField] private Renderer skyDomeRenderer;

        [Tooltip("Material texture property name controlling sky offset.")]
        [SerializeField] private string texturePropertyName = "_MainTex";

        [Tooltip("UV scrolling vector direction.")]
        [SerializeField] private Vector2 scrollDirection = new Vector2(1.0f, 0.0f);

        [Header("Camera & Lighting Integration")]
        [Tooltip("When enabled, SkyDome follows the main camera position.")]
        [SerializeField] private bool followCamera = true;

        [Tooltip("Directional Light to synchronize with the sky cycle.")]
        [SerializeField] private Light directionalLight;

        [Tooltip("Enable rotating directional light with the time of day.")]
        [SerializeField] private bool syncDirectionalLight = true;

        [Header("Light Tuning")]
        [SerializeField] private float dayLightIntensity = 1.0f;
        [SerializeField] private float nightLightIntensity = 0.08f;
        [SerializeField] private Color daySunColor = new Color(1.0f, 0.96f, 0.88f);
        [SerializeField] private Color sunsetSunColor = new Color(0.9f, 0.5f, 0.25f);
        [SerializeField] private Color moonLightColor = new Color(0.35f, 0.45f, 0.75f);
        [SerializeField] private Color dayAmbientColor = new Color(0.5f, 0.55f, 0.6f);
        [SerializeField] private Color nightAmbientColor = new Color(0.08f, 0.1f, 0.18f);

        [Header("Fog Integration")]
        [Tooltip("Enable dynamic fog synchronized with day/night cycle.")]
        [SerializeField] private bool syncFog = true;
        [SerializeField] private FogMode fogMode = FogMode.Linear;
        [SerializeField] private float fogDensity = 0.0025f;
        [SerializeField] private float fogStartDistance = 50.0f;
        [SerializeField] private float fogEndDistance = 350.0f;
        [SerializeField] private Color dayFogColor = new Color(0.55f, 0.65f, 0.75f);
        [SerializeField] private Color sunsetFogColor = new Color(0.85f, 0.5f, 0.3f);
        [SerializeField] private Color nightFogColor = new Color(0.04f, 0.06f, 0.12f);

        private Material _skyMaterial;
        private int _texturePropertyId;
        private Vector2 _initialOffset = Vector2.zero;
        private float _elapsedTime;

        public float CycleDuration { get { return cycleDuration; } }
        public float CurrentProgress { get { return (cycleDuration > 0f) ? (_elapsedTime % cycleDuration) / cycleDuration : 0f; } }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                Instance = this;
            }

            Initialize();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _texturePropertyId = Shader.PropertyToID(texturePropertyName);

            if (skyDomeRenderer == null)
            {
                skyDomeRenderer = GetComponentInChildren<Renderer>();
            }

            if (skyDomeRenderer != null)
            {
                _skyMaterial = Application.isPlaying ? skyDomeRenderer.material : skyDomeRenderer.sharedMaterial;
                if (_skyMaterial != null && _skyMaterial.HasProperty(_texturePropertyId))
                {
                    _initialOffset = _skyMaterial.GetTextureOffset(_texturePropertyId);
                }
            }

            if (syncDirectionalLight && directionalLight == null)
            {
                directionalLight = FindAnyObjectByType<Light>();
                if (directionalLight != null && directionalLight.type != LightType.Directional)
                {
                    directionalLight = null;
                }
            }
        }

        private void Update()
        {
            if (cycleDuration <= 0f) return;

            if (Application.isPlaying)
            {
                _elapsedTime += Time.deltaTime;
            }
            else
            {
                // In edit mode, initialize if missing
                if (_skyMaterial == null && skyDomeRenderer != null)
                {
                    _skyMaterial = skyDomeRenderer.sharedMaterial;
                }
            }

            float progress = (_elapsedTime % cycleDuration) / cycleDuration;
            UpdateSkyUV(progress);
            UpdateCameraPosition();

            if (syncDirectionalLight && directionalLight != null)
            {
                UpdateSunLight(progress);
            }
        }

        private Vector2 _currentOffset = Vector2.zero;

        private void UpdateSkyUV(float progress)
        {
            if (_skyMaterial == null) return;

            _currentOffset = new Vector2(
                (_initialOffset.x + progress * scrollDirection.x) % 1.0f,
                (_initialOffset.y + progress * scrollDirection.y) % 1.0f
            );

            if (_skyMaterial.HasProperty(_texturePropertyId))
            {
                _skyMaterial.SetTextureOffset(_texturePropertyId, _currentOffset);
            }

            // Fallback for standard or URP shaders
            if (_skyMaterial.HasProperty("_MainTex"))
            {
                _skyMaterial.SetTextureOffset("_MainTex", _currentOffset);
            }
            if (_skyMaterial.HasProperty("_BaseMap"))
            {
                _skyMaterial.SetTextureOffset("_BaseMap", _currentOffset);
            }
        }

        private void UpdateCameraPosition()
        {
            if (followCamera && Camera.main != null)
            {
                transform.position = Camera.main.transform.position;
            }
        }

        private float GetDayFactorFromProgress(float progress)
        {

            if (progress <= 0.18f || progress >= 0.85f)
            {
                return 1.0f;
            }
            else if (progress > 0.18f && progress < 0.36f)
            {
                float t = (progress - 0.18f) / (0.36f - 0.18f);
                // Power curve for faster initial dimming during sunset
                return Mathf.Pow(1.0f - t, 1.6f);
            }
            else if (progress > 0.70f && progress < 0.85f)
            {
                float t = (progress - 0.70f) / (0.85f - 0.70f);
                return Mathf.SmoothStep(0.0f, 1.0f, t);
            }
            else
            {
                return 0.0f;
            }
        }

        private void UpdateSunLight(float progress)
        {
            float dayFactor = GetDayFactorFromProgress(progress);

            if (dayFactor > 0f)
            {
                // Daytime phase - bright sunlight at start (progress = 0.0)
                float pitch = Mathf.Lerp(12.0f, 70.0f, dayFactor);

                // Yaw tracks sun movement across the sky
                float yaw = (progress <= 0.36f) ? Mathf.Lerp(180.0f, 315.0f, progress / 0.36f)
                                                : Mathf.Lerp(45.0f, 180.0f, (progress - 0.70f) / 0.30f);
                directionalLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0.0f);

                // Color lerps from warm sunset tint to bright daylight
                Color sunColor = Color.Lerp(sunsetSunColor, daySunColor, Mathf.SmoothStep(0f, 1f, dayFactor));
                directionalLight.color = sunColor;

                // Intensity lerps from night intensity up to bright day intensity using faster dimming curve
                directionalLight.intensity = Mathf.Lerp(nightLightIntensity, dayLightIntensity, dayFactor);

                // Ambient light
                RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, dayFactor);
            }
            else
            {
                // Full Nighttime phase (progress 0.36 to 0.70)
                float nightProgressNormalized = (progress - 0.36f) / 0.34f;
                float moonYaw = Mathf.Lerp(45.0f, 135.0f, Mathf.Clamp01(nightProgressNormalized));
                directionalLight.transform.rotation = Quaternion.Euler(45.0f, moonYaw, 0.0f);

                directionalLight.color = moonLightColor;
                directionalLight.intensity = nightLightIntensity;

                RenderSettings.ambientLight = nightAmbientColor;
            }

            UpdateFog(dayFactor);
        }

        private void UpdateFog(float dayFactor)
        {
            if (!syncFog) return;

            RenderSettings.fog = true;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = fogStartDistance;

            // In Linear mode, also scale end distance dynamically if fogDensity is adjusted
            if (fogMode == FogMode.Linear && fogDensity > 0.0001f)
            {
                RenderSettings.fogEndDistance = Mathf.Min(fogEndDistance, 1.0f / fogDensity);
            }
            else
            {
                RenderSettings.fogEndDistance = fogEndDistance;
            }

            Color currentFogColor = (dayFactor > 0.4f)
                ? Color.Lerp(sunsetFogColor, dayFogColor, (dayFactor - 0.4f) / 0.6f)
                : Color.Lerp(nightFogColor, sunsetFogColor, dayFactor / 0.4f);

            RenderSettings.fogColor = currentFogColor;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureDayNightCycle()
        {
            if (Object.FindAnyObjectByType<DayNightCycle>() == null)
            {
                GameObject skyDomeObj = GameObject.Find("SkyDome");

                if (skyDomeObj == null)
                {
                    GameObject prefab = Resources.Load<GameObject>("ThirdParty/SkyDome");
                    if (prefab != null)
                    {
                        skyDomeObj = Object.Instantiate(prefab);
                        skyDomeObj.name = "SkyDome";
                    }
                }

                if (skyDomeObj != null && skyDomeObj.GetComponent<DayNightCycle>() == null)
                {
                    skyDomeObj.AddComponent<DayNightCycle>();
                }
            }
        }
    }
}
