using UnityEngine;
using LittleTrawling.Core;

namespace LittleTrawling.Entities
{
    /// <summary>
    /// Put this on the Shopkeeper NPC object.
    /// </summary>
    public class ShopNPC : MonoBehaviour
    {
        [Tooltip("Tag on the player avatar.")]
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Max distance to interact with the shopkeeper if trigger detection is missed.")]
        [SerializeField] private float maxInteractDistance = 2.5f;

        private PlayerController _player;
        private bool _playerInRange;

        private void Start()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed += OnInteract;
                Debug.Log($"[ShopNPC] Registered InteractPressed on InputReader.Instance ({InputReader.Instance.name})");
            }
            else
            {
                Debug.LogWarning("[ShopNPC] Start() - InputReader.Instance is NULL!");
            }
        }

        private void OnDestroy()
        {
            if (InputReader.Instance != null)
                InputReader.Instance.InteractPressed -= OnInteract;
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[ShopNPC] OnTriggerEnter with object '{other.name}' (Tag: '{other.tag}')");
            if (!other.CompareTag(playerTag)) return;
            _playerInRange = true;
            _player = other.GetComponentInParent<PlayerController>() ?? other.GetComponent<PlayerController>();
            Debug.Log($"[ShopNPC] Player entered trigger range! Player: {_player?.name}");
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log($"[ShopNPC] OnTriggerExit with object '{other.name}' (Tag: '{other.tag}')");
            if (!other.CompareTag(playerTag)) return;
            _playerInRange = false;
            Debug.Log("[ShopNPC] Player exited trigger range.");
        }

        private void OnInteract()
        {
            var gm = GameManager.Instance;
            Debug.Log($"[ShopNPC] OnInteract event received! GameState: {(gm != null ? gm.CurrentState.ToString() : "NULL")}");

            if (gm == null) return;

            // Open shop when walking near the NPC
            if (gm.IsState(GameState.Walking))
            {
                if (_player == null)
                {
                    var playerObj = GameObject.FindGameObjectWithTag(playerTag);
                    if (playerObj != null)
                        _player = playerObj.GetComponentInParent<PlayerController>() ?? playerObj.GetComponent<PlayerController>();
                }

                bool canInteract = _playerInRange;
                float dist = -1f;

                // Fallback distance check to ensure interaction works reliably
                if (_player != null)
                {
                    dist = Vector3.Distance(_player.transform.position, transform.position);
                    if (dist <= maxInteractDistance)
                        canInteract = true;
                }

                Debug.Log($"[ShopNPC] Evaluate interact: playerInRange={_playerInRange}, dist={dist:F2}m (max={maxInteractDistance}m), canInteract={canInteract}");

                if (canInteract)
                {
                    Debug.Log("[ShopNPC] Success! Switching state to GameState.Shopping");
                    gm.SetState(GameState.Shopping);
                }
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
                npcObj.name = "Shopkeeper NPC";
                npcObj.transform.position = spawnPos;

                var col = npcObj.GetComponent<CapsuleCollider>();
                if (col != null) col.isTrigger = true;

                npcObj.AddComponent<ShopNPC>();

                var mr = npcObj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.material.color = new Color(0.2f, 0.7f, 1.0f);
                }
            }
        }
    }
}
