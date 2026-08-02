using System.Collections;
using UnityEngine;
using TMPro;
using LittleTrawling.Core;

namespace LittleTrawling.UI
{
    /// <summary>
    /// Cinematic launch intro sequence:
    /// 1. 1s silent start (no text)
    /// 2. Fade in/out "Jacob T. and Liam M. presents..."
    /// 3. Fade in/out "A Little Trawling"
    /// </summary>
    public class GameTitleIntroUI : MonoBehaviour
    {
        public static GameTitleIntroUI Instance { get; private set; }

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _titleText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildUI();
        }

        private void Start()
        {
            StartCoroutine(TitleIntroRoutine());
        }

        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            _canvas = UITheme.CreateScreenCanvas("GameTitleIntroUI_Canvas", 200);
            _canvas.transform.SetParent(transform, false);

            _canvasGroup = _canvas.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            _titleText = UITheme.CreateLabel("TitleText", _canvas.transform,
                "",
                110f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            _titleText.textWrappingMode = TextWrappingModes.Normal;
            RectTransform textRt = _titleText.rectTransform;
            UITheme.CenterWithSize(textRt, 1500f, 320f);
        }

        private IEnumerator TitleIntroRoutine()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            // 1. Wait 1s at start with no text
            yield return new WaitForSeconds(1.0f);

            // 2. "Jacob T. and Liam M. presents..."
            if (_titleText != null)
            {
                _titleText.fontSize = 42f;
                _titleText.text = "Jacob T. and Liam M. presents...";
            }

            yield return FadeCanvasGroup(0f, 1f, 0.8f);
            yield return new WaitForSeconds(1.6f);
            yield return FadeCanvasGroup(1f, 0f, 0.8f);

            // 3. Short pause
            yield return new WaitForSeconds(0.5f);

            // 4. "A Little Trawling"
            if (_titleText != null)
            {
                _titleText.fontSize = 110f;
                _titleText.text = "A Little Trawling";
            }

            yield return FadeCanvasGroup(0f, 1f, 0.8f);
            yield return new WaitForSeconds(1.8f);
            yield return FadeCanvasGroup(1f, 0f, 1.2f);

            // Cleanup
            Destroy(gameObject);
        }

        private IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                }
                yield return null;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = endAlpha;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureGameTitleIntroUI()
        {
            if (UnityEngine.Object.FindAnyObjectByType<GameTitleIntroUI>() == null)
            {
                var obj = new GameObject("GameTitleIntroUI");
                obj.AddComponent<GameTitleIntroUI>();
            }
        }
    }
}
