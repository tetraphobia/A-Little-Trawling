using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Interaction;
using LittleTrawling.Systems;

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
            return "Press <color=yellow><b>[E]</b></color> to talk to Shopkeeper";
        }

        public void Interact()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsState(GameState.Walking)) return;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue("Shopkeeper", greetingLines, () =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SetState(GameState.Shopping);
                    }
                });
            }
            else
            {
                gm.SetState(GameState.Shopping);
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
