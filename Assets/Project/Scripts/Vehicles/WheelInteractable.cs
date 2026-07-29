using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Entities;
using LittleTrawling.Environment;

namespace LittleTrawling.Vehicles
{
    /// <summary>
    /// Put this on the ShipWheel object to allow the player to enter/exit boat piloting mode.
    /// </summary>
    public class WheelInteractable : MonoBehaviour
    {
        [Tooltip("Empty transform where the player stands while steering.")]
        [SerializeField] private Transform pilotAnchor;

        [Tooltip("Tag on the player avatar.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Max distance to interact with the wheel if trigger detection is missed.")]
        [SerializeField] private float maxInteractDistance = 2.0f;

        private PlayerController _player;
        private BoatController _boatController;
        private bool _playerInRange;

        private void Awake()
        {
            _boatController = GetComponentInParent<BoatController>() ?? GetComponent<BoatController>();
        }

        private void Start()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed += OnInteract;
            }
        }

        private void OnDestroy()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.InteractPressed -= OnInteract;
            }
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

        private PlayerController GetPlayer()
        {
            if (_player != null) return _player;

            var playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                _player = playerObj.GetComponentInParent<PlayerController>() ?? playerObj.GetComponent<PlayerController>();
            }
            return _player;
        }

        private void OnInteract()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (_boatController == null)
            {
                _boatController = GetComponentInParent<BoatController>() ?? GetComponent<BoatController>();
            }

            if (gm.IsState(GameState.Piloting))
            {
                TryStopPiloting(gm);
            }
            else if (gm.IsState(GameState.Walking))
            {
                TryStartPiloting(gm);
            }
        }

        private void TryStopPiloting(GameManager gm)
        {
            if (_boatController != null)
            {
                Dock targetDock = _boatController.CurrentDockZone;
                if (targetDock == null || !targetDock.IsBoatInside(_boatController))
                {
                    targetDock = FindDockContainingBoat();
                }

                if (targetDock != null && targetDock.IsBoatInside(_boatController))
                {
                    _boatController.DockTo(targetDock);
                }
            }

            PlayerController player = GetPlayer();
            if (player != null && pilotAnchor != null)
            {
                player.SnapTo(pilotAnchor);
            }

            gm.SetState(GameState.Walking);
        }

        private void TryStartPiloting(GameManager gm)
        {
            PlayerController player = GetPlayer();
            if (player == null) return;

            bool canInteract = _playerInRange;
            if (!canInteract)
            {
                Vector3 anchorPos = pilotAnchor != null ? pilotAnchor.position : transform.position;
                float dist = Vector3.Distance(player.transform.position, anchorPos);
                canInteract = dist <= maxInteractDistance;
            }

            if (canInteract)
            {
                if (_boatController != null && _boatController.IsDocked)
                {
                    _boatController.Undock();
                }

                if (pilotAnchor != null)
                {
                    player.SnapTo(pilotAnchor);
                }
                gm.SetState(GameState.Piloting);
            }
        }

        private Dock FindDockContainingBoat()
        {
            if (_boatController == null) return null;
            var docks = Object.FindObjectsByType<Dock>(FindObjectsSortMode.None);
            foreach (var d in docks)
            {
                if (d != null && d.IsBoatInside(_boatController))
                {
                    return d;
                }
            }
            return null;
        }
    }
}