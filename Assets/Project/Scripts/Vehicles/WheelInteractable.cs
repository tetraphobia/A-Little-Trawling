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
            _player = other.GetComponentInParent<PlayerController>();
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
            if (gm.IsState(GameState.Walking) && _playerInRange && _player != null)
            {
                if (pilotAnchor != null) _player.SnapTo(pilotAnchor);
                gm.SetState(GameState.Piloting);
            }
        }
    }
}