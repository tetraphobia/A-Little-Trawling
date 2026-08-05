using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LittleTrawling.Audio;
using LittleTrawling.Core;

namespace LittleTrawling.UI
{
    public struct DialogueChoice
    {
        public string text;
        public Action onSelect;

        public DialogueChoice(string text, Action onSelect)
        {
            this.text = text;
            this.onSelect = onSelect;
        }
    }

    /// <summary>
    /// Displays Animal Crossing style dialogue choice options floating outside, directly top-right of the main dialogue window.
    /// </summary>
    public class DialogueChoiceUI : MonoBehaviour
    {
        public static DialogueChoiceUI Instance { get; private set; }

        public bool IsShowingChoices => _modalRoot != null && _modalRoot.activeSelf;

        [Header("Audio SFX")]
        [Tooltip("Sound played when selecting a dialogue option.")]
        [SerializeField] private AudioClip choiceSelectSound;
        [Tooltip("Sound played when hovering a dialogue option button.")]
        [SerializeField] private AudioClip choiceHoverSound;

        private Canvas _canvas;
        private GameObject _modalRoot;
        private Transform _buttonContainer;

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

        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            _canvas = UITheme.CreateScreenCanvas("DialogueChoiceUI_Canvas", 105);
            _canvas.transform.SetParent(transform, false);

            _modalRoot = new GameObject("ChoiceModalRoot");
            _modalRoot.transform.SetParent(_canvas.transform, false);
            RectTransform modalRt = _modalRoot.AddComponent<RectTransform>();
            UITheme.StretchFill(modalRt);

            // Floating container directly top-right outside of the main dialogue window
            Image border = UITheme.CreatePanel("ChoiceContainerBorder", _modalRoot.transform,
                UITheme.PanelSprite, UITheme.Gold);
            RectTransform borderRt = border.rectTransform;
            borderRt.anchorMin = new Vector2(0.5f, 0);
            borderRt.anchorMax = new Vector2(0.5f, 0);
            borderRt.pivot = new Vector2(1, 0); // Bottom-right corner aligned with top-right of dialogue box
            borderRt.sizeDelta = new Vector2(300f, 120f);
            borderRt.anchoredPosition = new Vector2(420f, 236f);

            Image panelBg = UITheme.CreatePanel("ChoiceContainerBg", border.transform,
                UITheme.PanelSprite, UITheme.CardWhite);
            UITheme.StretchFill(panelBg.rectTransform, 3f, 3f, 3f, 3f);

            VerticalLayoutGroup vlg = panelBg.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            _buttonContainer = panelBg.transform;
            _modalRoot.SetActive(false);
        }

        public void ShowChoices(List<DialogueChoice> choices)
        {
            if (choices == null || choices.Count == 0) return;

            for (int i = _buttonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_buttonContainer.GetChild(i).gameObject);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            foreach (var choice in choices)
            {
                var capturedChoice = choice;
                Button btn = UITheme.CreateButton("ChoiceBtn", _buttonContainer, choice.text,
                    UITheme.BackgroundMint, UITheme.TextBrown, UITheme.BodyFontSize - 2f, 280f, 46f);

                btn.onClick.AddListener(() =>
                {
                    AudioClip clip = choiceSelectSound != null ? choiceSelectSound : ProceduralAudioSynthesizer.GetChoiceSelectSound();
                    Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
                    float uiVol = VolumeManager.Instance != null ? VolumeManager.Instance.UiSoundVolume : 0.5f;
                    if (VolumeManager.Instance != null)
                    {
                        VolumeManager.Instance.PlayClipAtPoint(clip, pos, uiVol, AudioCategory.UI);
                    }
                    else
                    {
                        AudioSource.PlayClipAtPoint(clip, pos, uiVol);
                    }
                    HideChoices();
                    capturedChoice.onSelect?.Invoke();
                });
            }

            _modalRoot.SetActive(true);
        }

        public void HideChoices()
        {
            if (_modalRoot != null) _modalRoot.SetActive(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureDialogueChoiceUI()
        {
            if (UnityEngine.Object.FindAnyObjectByType<DialogueChoiceUI>() == null)
            {
                var obj = new GameObject("DialogueChoiceUI");
                obj.AddComponent<DialogueChoiceUI>();
            }
        }
    }
}
