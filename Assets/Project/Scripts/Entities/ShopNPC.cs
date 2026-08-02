using System.Collections.Generic;
using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Interaction;
using LittleTrawling.Systems;
using LittleTrawling.UI;

namespace LittleTrawling.Entities
{
    /// <summary>
    /// Put this on the Shopkeeper NPC object.
    /// Implements IInteractable for the new InteractionSystem.
    /// </summary>
    public class ShopNPC : MonoBehaviour, IInteractable
    {
        [Tooltip("Greeting dialogue lines spoken by the shopkeeper when approached.")]
        [TextArea(2, 4)]
        [SerializeField] private string[] greetingLines = new string[]
        {
            "Ahoy, fisherbird! Welcome to my dockside shop.",
            "Take a look at my latest boat engines and fishing rods!"
        };

        private void Awake()
        {
            // Ensure InteractionTrigger exists
            if (GetComponent<InteractionTrigger>() == null)
            {
                gameObject.AddComponent<InteractionTrigger>();
            }
        }

        public string GetInteractionPrompt()
        {
            return "<color=#EE5D5D><b>[E]</b></color> Talk to Shopkeeper";
        }

        public void Interact()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsState(GameState.Walking)) return;

            string[] introLine = new string[] { "Ahoy, fisherbird! Welcome to my dockside shop." };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue("Shopkeeper", introLine, () =>
                {
                    PresentShopkeeperChoices();
                });
            }
            else
            {
                PresentShopkeeperChoices();
            }
        }

        private void PresentShopkeeperChoices()
        {
            if (DialogueChoiceUI.Instance == null)
            {
                if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Shopping);
                return;
            }

            var choices = new List<DialogueChoice>
            {
                new DialogueChoice("What have you got?", () =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SetState(GameState.Shopping);
                    }
                }),
                new DialogueChoice("Why am I here?", () =>
                {
                    PlayTutorialDialogue();
                })
            };

            DialogueChoiceUI.Instance.ShowChoices(choices);
        }

        private void PlayTutorialDialogue()
        {
            string[] tutorialLines = new string[]
            {
                "Why, you're here to fish of course! I've got twelve ducklings at home that are counting on me.",
                "Hold the <color=#EE5D5D><b>[F]</b></color> key near the water to charge and aim your cast. The longer you hold, the farther it goes.",
                "Wait until you see the bobber bob quickly, then press <color=#EE5D5D><b>[F]</b></color> again to reel in your catch!",
                "Use the boat to navigate around the waters, some species of fish can only be caught in the deeper parts of the lake.",
                "The price of a fish varies depending on its rarity and size. Larger, rarer fish are worth more.",
                "Press <color=#EE5D5D><b>[I]</b></color> to see what fish you've caught so far, and what price I'll pay for them!"
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue("Shopkeeper", tutorialLines, () =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SetState(GameState.Walking);
                    }
                });
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureShopNPC()
        {
            if (Object.FindAnyObjectByType<ShopNPC>() == null)
            {
                var dock = Object.FindAnyObjectByType<LittleTrawling.Environment.Dock>();
                Vector3 spawnPos = dock != null ? dock.transform.position + Vector3.up * 0.8f + dock.transform.right * 1.8f : new Vector3(2f, 1f, 2f);
                var npcObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                npcObj.name = "ShopkeeperNPC";
                npcObj.transform.position = spawnPos;

                var sphere = npcObj.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 2.5f;

                npcObj.AddComponent<ShopNPC>();
            }
        }
    }
}
