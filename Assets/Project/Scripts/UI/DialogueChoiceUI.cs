using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    /// Displays Animal Crossing style dialogue choice options.
    /// </summary>
    public class DialogueChoiceUI : MonoBehaviour
    {
        public static DialogueChoiceUI Instance { get; private set; }

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

            Image border = UITheme.CreatePanel("ChoiceContainerBorder", _modalRoot.transform,
                UITheme.PanelSprite, UITheme.Gold);
            RectTransform borderRt = border.rectTransform;
            borderRt.anchorMin = new Vector2(1, 0);
            borderRt.anchorMax = new Vector2(1, 0);
            borderRt.pivot = new Vector2(1, 0);
            borderRt.sizeDelta = new Vector2(340f, 130f);
            borderRt.anchoredPosition = new Vector2(-60f, 240f);

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
                    UITheme.BackgroundMint, UITheme.TextBrown, UITheme.BodyFontSize, 300f, 48f);

                btn.onClick.AddListener(() =>
                {
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
