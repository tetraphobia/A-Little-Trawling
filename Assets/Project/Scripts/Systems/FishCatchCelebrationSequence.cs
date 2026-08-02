using System;
using System.Collections;
using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Data;
using LittleTrawling.UI;

namespace LittleTrawling.Systems
{
    /// <summary>
    /// Animal Crossing style catch celebration sequence:
    /// 1. Smoothly rotates/zooms camera around to face player character from the front.
    /// 2. Spawns and presents caught fish sprite floating right in front of the character with a scale bounce & bobbing.
    /// 3. Displays dialogue box with catch details.
    /// 4. Restores camera to original position & cleans up when dialogue closes.
    /// </summary>
    public class FishCatchCelebrationSequence : MonoBehaviour
    {
        public static FishCatchCelebrationSequence Instance { get; private set; }

        [Header("Audio SFX")]
        [Tooltip("Fanfare played when catching a fish species for the first time.")]
        [SerializeField] private AudioClip firstCatchJingle;
        [Tooltip("Fanfare played when landing a LUNKER! (3x size & value).")]
        [SerializeField] private AudioClip lunkerFanfare;
        [Tooltip("Fanfare played when landing a MEGA LUNKER! (6x size & value).")]
        [SerializeField] private AudioClip megaLunkerFanfare;

        private GameObject _activePresentedFish;

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

        public void PlayCelebration(Fish species, LunkerStatus lunkerStatus, string speakerTitle, string[] dialogueLines, Action onComplete = null)
        {
            StartCoroutine(CelebrationRoutine(species, lunkerStatus, speakerTitle, dialogueLines, onComplete));
        }

        private IEnumerator CelebrationRoutine(Fish species, LunkerStatus lunkerStatus, string speakerTitle, string[] dialogueLines, Action onComplete)
        {
            var gm = GameManager.Instance;

            AudioClip fanfare = lunkerStatus switch
            {
                LunkerStatus.MegaLunker => megaLunkerFanfare,
                LunkerStatus.Lunker => lunkerFanfare,
                _ => firstCatchJingle
            };

            if (fanfare != null)
            {
                AudioSource.PlayClipAtPoint(fanfare, Camera.main != null ? Camera.main.transform.position : transform.position, 1.0f);
            }
            if (gm != null) gm.SetState(GameState.Dialogue);

            Transform playerT = null;
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerT = playerObj.transform;

            if (playerT == null)
            {
                var playerController = UnityEngine.Object.FindAnyObjectByType<LittleTrawling.Entities.PlayerController>();
                if (playerController != null) playerT = playerController.transform;
            }

            if (playerT != null && ThirdPersonCameraController.Instance != null)
            {
                float targetYaw = playerT.eulerAngles.y + 180f;
                ThirdPersonCameraController.Instance.SetCelebrationOverride(true, targetYaw, 5f, 3.8f, new Vector3(0f, 1.2f, 0f));
            }

            if (playerT != null)
            {
                Vector3 fishPos = playerT.position + Vector3.up * 1.55f + playerT.forward * 0.35f;
                _activePresentedFish = new GameObject($"CelebrationFish_{species.displayName}");
                _activePresentedFish.transform.position = fishPos;

                SpriteRenderer sr = _activePresentedFish.AddComponent<SpriteRenderer>();
                if (species != null && species.sprite != null)
                {
                    sr.sprite = species.sprite;
                }

                float baseScale = (lunkerStatus == LunkerStatus.MegaLunker) ? 0.65f : (lunkerStatus == LunkerStatus.Lunker) ? 0.5f : 0.4f;
                _activePresentedFish.transform.localScale = Vector3.zero;

                StartCoroutine(AnimateFishPresentation(_activePresentedFish, baseScale));
            }

            yield return new WaitForSeconds(0.35f);

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue(speakerTitle, dialogueLines, () =>
                {
                    EndCelebration();
                    onComplete?.Invoke();
                });
            }
            else
            {
                yield return new WaitForSeconds(2.5f);
                EndCelebration();
                onComplete?.Invoke();
            }
        }

        private IEnumerator AnimateFishPresentation(GameObject fishObj, float targetScale)
        {
            float elapsed = 0f;
            float popDuration = 0.3f;
            Vector3 initialPos = fishObj.transform.position;

            while (elapsed < popDuration)
            {
                if (fishObj == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                float scaleT = Mathf.Sin(t * Mathf.PI * 0.6f) * 1.15f;
                fishObj.transform.localScale = Vector3.one * (targetScale * Mathf.Clamp01(scaleT));
                yield return null;
            }

            float timer = 0f;
            while (fishObj != null)
            {
                timer += Time.deltaTime;
                float bobY = Mathf.Sin(timer * 4f) * 0.04f;
                fishObj.transform.position = initialPos + Vector3.up * bobY;

                if (Camera.main != null)
                {
                    fishObj.transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up);
                }

                yield return null;
            }
        }

        private void EndCelebration()
        {
            if (_activePresentedFish != null)
            {
                Destroy(_activePresentedFish);
                _activePresentedFish = null;
            }

            if (ThirdPersonCameraController.Instance != null)
            {
                ThirdPersonCameraController.Instance.SetCelebrationOverride(false);
            }

            var gm = GameManager.Instance;
            if (gm != null && gm.IsState(GameState.Dialogue))
            {
                gm.SetState(GameState.Walking);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureCelebrationSequence()
        {
            if (UnityEngine.Object.FindAnyObjectByType<FishCatchCelebrationSequence>() == null)
            {
                var obj = new GameObject("FishCatchCelebrationSequence");
                obj.AddComponent<FishCatchCelebrationSequence>();
            }
        }
    }
}
