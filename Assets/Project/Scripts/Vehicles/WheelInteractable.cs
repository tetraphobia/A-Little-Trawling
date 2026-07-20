using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Entities;

namespace LittleTrawling.Vehicles
{
    /// <summary>
    /// Put this on the ShipWheel object and assign the pilot anchor (where the player stands while steering).
    /// </summary>
    public class WheelInteractable : MonoBehaviour
    {
        [Tooltip("Empty transform where the player stands while steering.")]
        [SerializeField] private Transform pilotAnchor;
        [Tooltip("Tag on the player avatar.")]
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Max distance to interact with the wheel if trigger detection is missed.")]
        [SerializeField] private float maxInteractDistance = 2.5f;

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

            // Stop steering
            if (gm.IsState(GameState.Piloting))
            {
                gm.SetState(GameState.Walking);
                return;
            }

            // Start steering
            if (gm.IsState(GameState.Walking))
            {
                if (_player == null)
                {
                    var playerObj = GameObject.FindGameObjectWithTag(playerTag);
                    if (playerObj != null)
                        _player = playerObj.GetComponentInParent<PlayerController>() ?? playerObj.GetComponent<PlayerController>();
                }

                bool canInteract = _playerInRange;

                // Fallback distance check to ensure interaction works reliably
                if (!canInteract && _player != null)
                {
                    Vector3 anchorPos = pilotAnchor != null ? pilotAnchor.position : transform.position;
                    float dist = Vector3.Distance(_player.transform.position, anchorPos);
                    if (dist <= maxInteractDistance)
                        canInteract = true;
                }

                if (canInteract && _player != null)
                {
                    if (pilotAnchor != null) _player.SnapTo(pilotAnchor);
                    gm.SetState(GameState.Piloting);
                }
            }
        }
    }
}