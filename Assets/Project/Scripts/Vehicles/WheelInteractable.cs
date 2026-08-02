using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Entities;
using LittleTrawling.Environment;
using LittleTrawling.Interaction;

namespace LittleTrawling.Vehicles
{
    /// <summary>
    /// Put this on the ShipWheel object to allow the player to enter/exit boat piloting mode.
    /// Implements IInteractable for the new InteractionSystem.
    /// </summary>
    public class WheelInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Empty transform where the player stands while steering.")]
        [SerializeField] private Transform pilotAnchor;

        [Tooltip("Tag on the player avatar.")]
        [SerializeField] private string playerTag = "Player";

        private PlayerController _player;
        private BoatController _boatController;

        private void Awake()
        {
            _boatController = GetComponentInParent<BoatController>() ?? GetComponent<BoatController>();
            
            // Ensure InteractionTrigger exists on object or children
            if (GetComponent<InteractionTrigger>() == null && GetComponentInChildren<InteractionTrigger>() == null)
            {
                gameObject.AddComponent<InteractionTrigger>();
            }
        }

        public string GetInteractionPrompt()
        {
            var gm = GameManager.Instance;
            if (BoatController.Instance != null && BoatController.Instance.IsPlayerPiloting)
            {
                return "<color=#EE5D5D><b>[E]</b></color> Exit boat";
            }
            return "<color=#EE5D5D><b>[E]</b></color> Enter boat";
        }

        public void Interact()
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

        private PlayerController GetPlayer()
        {
            if (_player != null) return _player;
            _player = PlayerController.Instance;
            if (_player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag(playerTag);
                if (playerObj != null)
                {
                    _player = playerObj.GetComponentInParent<PlayerController>() ?? playerObj.GetComponent<PlayerController>();
                }
            }
            return _player;
        }

        private void TryStopPiloting(GameManager gm)
        {
            if (_boatController != null)
            {
                _boatController.PlayExitSound();

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

            if (_boatController != null)
            {
                if (_boatController.IsDocked)
                {
                    _boatController.Undock();
                }
                _boatController.PlayEnterSound();
            }

            if (pilotAnchor != null)
            {
                player.SnapTo(pilotAnchor);
            }
            gm.SetState(GameState.Piloting);
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