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

        private static GUIStyle _bannerStyle;

        private static GUIStyle GetBannerStyle()
        {
            if (_bannerStyle == null)
            {
                _bannerStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
            }
            return _bannerStyle;
        }

        private void OnGUI()
        {
            if (FishingManager.Instance == null) return;

            FishingState state = FishingManager.Instance.CurrentState;
            if (state == FishingState.Idle) return;

            GUIStyle style = GetBannerStyle();

            switch (state)
            {
                case FishingState.Charging:
                    {
                        float ratio = FishingManager.Instance.ChargeRatio;
                        int width = 300;
                        int height = 34;
                        Rect outer = new Rect((Screen.width - width) / 2f, Screen.height - 120, width, height);

                        GUI.Box(outer, "");
                        Rect inner = new Rect(outer.x + 4, outer.y + 4, (outer.width - 8) * ratio, outer.height - 8);
                        GUI.DrawTexture(inner, GetFillTexture());

                        GUI.Label(outer, $"<size=15><b>Casting... {Mathf.RoundToInt(ratio * 100)}%</b></size>", style);
                    }
                    break;

                case FishingState.WaitingForBite:
                    {
                        int width = 340;
                        int height = 35;
                        Rect rect = new Rect((Screen.width - width) / 2f, Screen.height - 110, width, height);

                        GUI.Box(rect, "");
                        GUI.Label(rect, "<size=14>Waiting for a bite... (Press <b>[F]</b> to recall)</size>", style);
                    }
                    break;

                case FishingState.BiteActive:
                    {
                        int width = 360;
                        int height = 55;
                        Rect rect = new Rect((Screen.width - width) / 2f, Screen.height - 180, width, height);

                        GUI.Box(rect, "");
                        GUI.Label(rect, "<size=21><color=yellow><b>BITE! PRESS [F] NOW!</b></color></size>", style);
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
