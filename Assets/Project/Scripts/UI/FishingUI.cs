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
            _titleText = $"<size=20><b>Caught a {species.displayName.ToUpper()}!</b></size>";
            _statsText = $"<size=15>Length: <b>{sizeCm:F1} cm</b> | Weight: <b>{weightKg:F2} kg</b></size>";
            _goldText = $"<size=18><b>Earned ${goldEarned} Gold</b></size>";
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

        private Texture2D _fillTexture;

        private Texture2D GetFillTexture()
        {
            if (_fillTexture == null)
            {
                _fillTexture = new Texture2D(1, 1);
                _fillTexture.SetPixel(0, 0, new Color(0.2f, 0.85f, 0.3f, 1.0f));
                _fillTexture.Apply();
            }
            return _fillTexture;
        }

        private void OnGUI()
        {
            DrawFishingHUD();

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

        private void DrawFishingHUD()
        {
            var fm = FishingManager.Instance;
            if (fm == null) return;

            switch (fm.CurrentState)
            {
                case FishingState.Charging:
                    {
                        float ratio = fm.ChargeRatio;
                        int width = 300;
                        int height = 34;
                        Rect outer = new Rect((Screen.width - width) / 2f, Screen.height - 120, width, height);

                        GUI.Box(outer, "");
                        Rect inner = new Rect(outer.x + 4, outer.y + 4, (outer.width - 8) * ratio, outer.height - 8);
                        GUI.DrawTexture(inner, GetFillTexture());

                        GUI.Label(outer, $"<size=15><b>Casting... {Mathf.RoundToInt(ratio * 100)}%</b></size>", new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            richText = true
                        });
                    }
                    break;

                case FishingState.WaitingForBite:
                    {
                        int width = 340;
                        int height = 35;
                        Rect rect = new Rect((Screen.width - width) / 2f, Screen.height - 110, width, height);

                        GUI.Box(rect, "");
                        GUI.Label(rect, "<size=14>Waiting for a bite... (Press <b>[F]</b> to recall)</size>", new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            richText = true
                        });
                    }
                    break;

                case FishingState.BiteActive:
                    {
                        int width = 360;
                        int height = 55;
                        Rect rect = new Rect((Screen.width - width) / 2f, Screen.height - 180, width, height);

                        GUI.Box(rect, "");
                        GUI.Label(rect, "<size=21><color=yellow><b>⚡ BITE! PRESS [F] NOW! ⚡</b></color></size>", new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            richText = true
                        });
                    }
                    break;
            }
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
