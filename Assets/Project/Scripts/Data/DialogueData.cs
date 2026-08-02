using System;
using UnityEngine;

namespace LittleTrawling.Data
{
    /// <summary>
    /// Data container for a sequence of dialogue lines spoken by a character.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueData", menuName = "LittleTrawling/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [Tooltip("Name of the speaker displayed in the dialogue badge.")]
        public string speakerName = "Shopkeeper";

        [Tooltip("Color of the speaker name tag badge.")]
        public Color speakerColor = new Color(0.95f, 0.75f, 0.20f, 1.0f);

        [Tooltip("Sequence of dialogue lines spoken in order.")]
        [TextArea(3, 6)]
        public string[] lines = new string[]
        {
            "Hello there, fisherbird! Welcome to the shop.",
            "Take a look around at our selection of engines and fishing rods!"
        };

        [Tooltip("Typing speed in characters per second.")]
        public float charactersPerSecond = 35f;
    }

    /// <summary>
    /// Active runtime dialogue session structure.
    /// </summary>
    public class RuntimeDialogueSession
    {
        public string speakerName;
        public Color speakerColor;
        public string[] lines;
        public float charactersPerSecond;
        public Action onComplete;

        public RuntimeDialogueSession(string speakerName, string[] lines, Action onComplete = null, float charsPerSec = 35f, Color? speakerColor = null)
        {
            this.speakerName = string.IsNullOrEmpty(speakerName) ? "Speaker" : speakerName;
            this.lines = lines != null && lines.Length > 0 ? lines : new string[] { "..." };
            this.onComplete = onComplete;
            this.charactersPerSecond = charsPerSec > 0f ? charsPerSec : 35f;
            this.speakerColor = speakerColor ?? new Color(0.95f, 0.75f, 0.20f, 1.0f);
        }

        public RuntimeDialogueSession(DialogueData data, Action onComplete = null)
        {
            if (data != null)
            {
                this.speakerName = data.speakerName;
                this.speakerColor = data.speakerColor;
                this.lines = data.lines;
                this.charactersPerSecond = data.charactersPerSecond;
            }
            else
            {
                this.speakerName = "Speaker";
                this.speakerColor = new Color(0.95f, 0.75f, 0.20f, 1.0f);
                this.lines = new string[] { "..." };
                this.charactersPerSecond = 35f;
            }
            this.onComplete = onComplete;
        }
    }
}
