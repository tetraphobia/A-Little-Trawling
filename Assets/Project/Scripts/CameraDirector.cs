using Unity.Cinemachine;
using UnityEngine;

namespace LittleTrawling.Core
{
    /// <summary>
    /// Switches the active Cinemachine camera based on game state.
    /// </summary>
    public class CameraDirector : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera walkingCam;   // follow player avatar while walking
        [SerializeField] private CinemachineCamera pilotingCam;   // follow boat while piloting

        private const int Active = 20;
        private const int Idle = 10;

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.StateChanged += OnStateChanged;
                OnStateChanged(gm.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            bool piloting = state == GameState.Piloting;
            if (pilotingCam != null) pilotingCam.Priority = piloting ? Active : Idle;
            if (walkingCam != null) walkingCam.Priority = piloting ? Idle : Active;
        }
    }
}