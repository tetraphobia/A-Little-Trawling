using System;
using System.Collections;
using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;

namespace LittleTrawling.Systems
{
    /// <summary>
    /// Central manager for active dialogue sequences.
    /// Handles character-by-character typewriter text animation, audio blips, and input skipping.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        public event Action OnDialogueStarted;
        public event Action<int, string> OnLineStarted;
        public event Action<int> OnLineFinished;
        public event Action OnDialogueEnded;

        public bool IsActive => _currentSession != null;
        public RuntimeDialogueSession CurrentSession => _currentSession;
        public int CurrentLineIndex => _currentLineIndex;
        public string DisplayedText => _displayedText;
        public bool IsLineFullyTyped => _isLineFullyTyped;

        private RuntimeDialogueSession _currentSession;
        private int _currentLineIndex;
        private string _displayedText = "";
        private bool _isLineFullyTyped;
        private Coroutine _typewriterCoroutine;

        private AudioSource _audioSource;
        private AudioClip[] _blipClips;

        private GameState _previousState = GameState.Walking;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitAudio();
        }

        private void Start()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed += OnInteractPressed;
            }
        }

        private void OnDestroy()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed -= OnInteractPressed;
            }
            if (Instance == this) Instance = null;
        }

        public void ShowDialogue(string speakerName, string[] lines, Action onComplete = null, Color? speakerColor = null)
        {
            var session = new RuntimeDialogueSession(speakerName, lines, onComplete, 35f, speakerColor);
            StartDialogueSession(session);
        }

        public void ShowDialogue(DialogueData data, Action onComplete = null)
        {
            if (data == null) return;
            var session = new RuntimeDialogueSession(data, onComplete);
            StartDialogueSession(session);
        }

        private void StartDialogueSession(RuntimeDialogueSession session)
        {
            if (session == null || session.lines == null || session.lines.Length == 0) return;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                _previousState = gm.CurrentState;
                gm.SetState(GameState.Dialogue);
            }

            _currentSession = session;
            _currentLineIndex = 0;
            
            OnDialogueStarted?.Invoke();
            StartLine(_currentLineIndex);
        }

        private void OnInteractPressed()
        {
            if (_currentSession == null) return;

            var gm = GameManager.Instance;
            if (gm != null && !gm.IsState(GameState.Dialogue)) return;

            if (!_isLineFullyTyped)
            {
                // Instant skip to full line text
                CompleteCurrentLineInstantly();
            }
            else
            {
                // Advance to next line or complete dialogue
                AdvanceToNextLine();
            }
        }

        private void StartLine(int index)
        {
            if (_currentSession == null || index < 0 || index >= _currentSession.lines.Length)
            {
                EndDialogue();
                return;
            }

            _currentLineIndex = index;
            _displayedText = "";
            _isLineFullyTyped = false;

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }

            string fullLine = _currentSession.lines[index];
            OnLineStarted?.Invoke(index, fullLine);
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(fullLine, _currentSession.charactersPerSecond));
        }

        private IEnumerator TypewriterRoutine(string fullLine, float charsPerSec)
        {
            float delay = 1.0f / Mathf.Max(1f, charsPerSec);
            int length = fullLine.Length;

            for (int i = 1; i <= length; i++)
            {
                _displayedText = fullLine.Substring(0, i);
                PlayCharacterBlip(fullLine[i - 1]);
                yield return new WaitForSeconds(delay);
            }

            _isLineFullyTyped = true;
            OnLineFinished?.Invoke(_currentLineIndex);
            _typewriterCoroutine = null;
        }

        private void CompleteCurrentLineInstantly()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            if (_currentSession != null && _currentLineIndex < _currentSession.lines.Length)
            {
                _displayedText = _currentSession.lines[_currentLineIndex];
            }

            _isLineFullyTyped = true;
            OnLineFinished?.Invoke(_currentLineIndex);
        }

        private void AdvanceToNextLine()
        {
            int nextIndex = _currentLineIndex + 1;
            if (nextIndex < _currentSession.lines.Length)
            {
                StartLine(nextIndex);
            }
            else
            {
                EndDialogue();
            }
        }

        public void EndDialogue()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            var session = _currentSession;
            _currentSession = null;
            _displayedText = "";
            _isLineFullyTyped = false;

            OnDialogueEnded?.Invoke();

            if (session != null && session.onComplete != null)
            {
                session.onComplete.Invoke();
            }
            else
            {
                var gm = GameManager.Instance;
                if (gm != null && gm.IsState(GameState.Dialogue))
                {
                    gm.SetState(_previousState);
                }
            }
        }

        #region Procedural Animal Crossing Audio Blips

        private void InitAudio()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // 2D Sound
            _audioSource.volume = 0.35f;

            // Generate 3 short pitch variations of Animal Crossing speech blip
            _blipClips = new AudioClip[3];
            _blipClips[0] = GenerateBlipClip(520f);
            _blipClips[1] = GenerateBlipClip(640f);
            _blipClips[2] = GenerateBlipClip(780f);
        }

        private void PlayCharacterBlip(char c)
        {
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c)) return;
            if (_audioSource == null || _blipClips == null || _blipClips.Length == 0) return;

            int clipIndex = UnityEngine.Random.Range(0, _blipClips.Length);
            _audioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            _audioSource.PlayOneShot(_blipClips[clipIndex]);
        }

        private static AudioClip GenerateBlipClip(float frequency)
        {
            int sampleRate = 44100;
            float duration = 0.035f; // 35ms cute blip
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);

            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1.0f - (t / duration); // Exponential decay fadeout
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);
                samples[i] = wave * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create($"Blip_{frequency}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureDialogueManager()
        {
            if (UnityEngine.Object.FindAnyObjectByType<DialogueManager>() == null)
            {
                var obj = new GameObject("DialogueManager");
                obj.AddComponent<DialogueManager>();
            }
        }
    }
}
