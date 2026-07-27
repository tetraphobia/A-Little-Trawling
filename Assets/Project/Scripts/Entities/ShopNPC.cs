using UnityEngine;
using LittleTrawling.Core;

namespace LittleTrawling.Entities
{
    /// <summary>
    /// Put this on the Shopkeeper NPC object.
    /// Provides an interaction zone for opening the Upgrade Shop.
    /// </summary>
    public class ShopNPC : MonoBehaviour
    {
        [Tooltip("Tag on the player avatar.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Max distance to interact if trigger detection is missed.")]
        [SerializeField] private float maxInteractDistance = 3.0f;

        private PlayerController _player;
        private bool _playerInRange;

        private void Start()
        {
            if (InputReader.Instance != null)
                InputReader.Instance.InteractPressed += OnInteract;
        }

        private void OnDestroy()
        {
            if (InputReader.Instance != null)
                InputReader.Instance.InteractPressed -= OnInteract;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            _playerInRange = true;
            _player = other.GetComponentInParent<PlayerController>() ?? other.GetComponent<PlayerController>();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            _playerInRange = false;
        }

        private void OnInteract()
        {
            var gm = GameManager.Instance;
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

                if (!canInteract && _player != null)
                {
                    float dist = Vector3.Distance(_player.transform.position, transform.position);
                    if (dist <= maxInteractDistance)
                        canInteract = true;
                }

                if (canInteract)
                {
                    gm.SetState(GameState.Shopping);
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureShopNPC()
        {
            if (UnityEngine.Object.FindAnyObjectByType<ShopNPC>() == null)
            {
                var dock = UnityEngine.Object.FindAnyObjectByType<LittleTrawling.Environment.Dock>();
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
