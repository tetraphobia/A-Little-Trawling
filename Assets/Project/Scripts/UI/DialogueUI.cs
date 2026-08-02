using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Systems;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Renders Animal Crossing style bottom-of-screen dialogue box and speaker name badge.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        private static Texture2D _bgTexture;
        private static Texture2D _borderTexture;
        private static Texture2D _badgeTexture;
        private static GUIStyle _badgeTextStyle;
        private static GUIStyle _bodyTextStyle;
        private static GUIStyle _promptTextStyle;

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

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsState(GameState.Dialogue)) return;

            var mgr = DialogueManager.Instance;
            if (mgr == null || !mgr.IsActive) return;

            var session = mgr.CurrentSession;
            if (session == null) return;

            InitResources(session.speakerColor);
            GUI.depth = -9999;

            int width = Mathf.Min(740, Screen.width - 40);
            int height = 135;
            int x = (Screen.width - width) / 2;
            int y = Screen.height - 165;

            Rect outerRect = new Rect(x - 3, y - 3, width + 6, height + 6);
            Rect innerRect = new Rect(x, y, width, height);

            // Draw gold outer border
            GUI.DrawTexture(outerRect, _borderTexture);

            // Draw dark slate backdrop card
            GUI.DrawTexture(innerRect, _bgTexture);

            // Draw Speaker Name Tag Badge
            string speakerName = session.speakerName;
            int badgeWidth = Mathf.Max(140, Mathf.RoundToInt(speakerName.Length * 14f) + 30);
            Rect badgeOuter = new Rect(x + 18, y - 26, badgeWidth + 4, 32);
            Rect badgeInner = new Rect(x + 20, y - 24, badgeWidth, 28);

            GUI.DrawTexture(badgeOuter, _borderTexture);
            GUI.DrawTexture(badgeInner, _badgeTexture);
            GUI.Label(badgeInner, $"<b>{speakerName}</b>", _badgeTextStyle);

            // Draw Typewriter Substring Content
            Rect textRect = new Rect(x + 25, y + 20, width - 50, height - 40);
            string text = mgr.DisplayedText;
            GUI.Label(textRect, $"<size=18><b>{text}</b></size>", _bodyTextStyle);

            // Draw Continuation Indicator when line finishes typing
            if (mgr.IsLineFullyTyped)
            {
                float alpha = 0.5f + Mathf.PingPong(Time.time * 3f, 0.5f);
                Color savedColor = GUI.contentColor;
                GUI.contentColor = new Color(0.95f, 0.85f, 0.2f, alpha);

                Rect promptRect = new Rect(x + width - 150, y + height - 32, 135, 25);
                GUI.Label(promptRect, "▼ Press [E]", _promptTextStyle);

                GUI.contentColor = savedColor;
            }
        }

        private static Texture2D MakeSolidTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(w, h);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private static void InitResources(Color badgeColor)
        {
            if (_bgTexture == null)
            {
                _bgTexture = MakeSolidTex(1, 1, new Color(0.06f, 0.08f, 0.12f, 0.95f));
            }
            if (_borderTexture == null)
            {
                _borderTexture = MakeSolidTex(1, 1, new Color(0.95f, 0.75f, 0.20f, 1.0f));
            }

            // Dynamically generate badge texture matching speaker color
            if (_badgeTexture == null)
            {
                _badgeTexture = MakeSolidTex(1, 1, badgeColor);
            }

            if (_badgeTextStyle == null)
            {
                _badgeTextStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
                _badgeTextStyle.normal.textColor = Color.white;
            }

            if (_bodyTextStyle == null)
            {
                _bodyTextStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                    richText = true
                };
                _bodyTextStyle.normal.textColor = Color.white;
            }

            if (_promptTextStyle == null)
            {
                _promptTextStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
                _promptTextStyle.normal.textColor = Color.yellow;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureDialogueUI()
        {
            if (UnityEngine.Object.FindAnyObjectByType<DialogueUI>() == null)
            {
                var obj = new GameObject("DialogueUI");
                obj.AddComponent<DialogueUI>();
            }
        }
    }
}
