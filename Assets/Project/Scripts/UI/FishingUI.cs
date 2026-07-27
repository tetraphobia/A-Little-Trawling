using UnityEngine;
using LittleTrawling.Data;
using LittleTrawling.Systems;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Displays onscreen notification popups when fish are caught.
    /// </summary>
    public class FishingUI : MonoBehaviour
    {
        public static FishingUI Instance { get; private set; }

        private bool _showPopup;
        private string _titleText;
        private string _statsText;
        private string _goldText;
        private float _popupTimer;
        private const float PopupDuration = 3.5f;

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
            if (FishingManager.Instance != null)
            {
                FishingManager.Instance.OnFishCaught += ShowCatchPopup;
            }
        }

        private void OnDestroy()
        {
            if (FishingManager.Instance != null)
            {
                FishingManager.Instance.OnFishCaught -= ShowCatchPopup;
            }
            if (Instance == this) Instance = null;
        }

        private void ShowCatchPopup(Fish species, float sizeCm, float weightKg, int goldEarned)
        {
            _titleText = $"<size=20><b>🎣 CAUGHT A {species.displayName.ToUpper()}!</b></size>";
            _statsText = $"<size=15>Length: <b>{sizeCm:F1} cm</b> | Weight: <b>{weightKg:F2} kg</b></size>";
            _goldText = $"<size=18><b>💰 +${goldEarned} Gold</b></size>";
            _popupTimer = PopupDuration;
            _showPopup = true;
        }

        private void Update()
        {
            if (_showPopup)
            {
                _popupTimer -= Time.deltaTime;
                if (_popupTimer <= 0f)
                {
                    _showPopup = false;
                }
            }
        }

        private void OnGUI()
        {
            if (!_showPopup) return;

            int width = 380;
            int height = 100;
            Rect rect = new Rect((Screen.width - width) / 2f, 50, width, height);

            GUI.Box(rect, "");
            GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 20));
            
            GUILayout.Label(_titleText, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });
            GUILayout.Label(_statsText, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });
            GUILayout.Label(_goldText, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });

            GUILayout.EndArea();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureFishingUI()
        {
            if (Object.FindAnyObjectByType<FishingUI>() == null)
            {
                var uiObj = new GameObject("FishingUI");
                uiObj.AddComponent<FishingUI>();
            }
        }
    }
}
