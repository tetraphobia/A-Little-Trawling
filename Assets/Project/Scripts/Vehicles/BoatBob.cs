using UnityEngine;
using LittleTrawling.Vehicles;

namespace LittleTrawling.Vehicles
{
    /// <summary>
    /// Simulated visual boat buoyancy bobbing and rocking motion when undocked.
    /// </summary>
    public class BoatBob : MonoBehaviour
    {
        [Header("Bob (up / down)")]
        [SerializeField] private float bobHeight = 0.25f;   // meters
        [SerializeField] private float bobSpeed = 1.6f;

        [Header("Rock (tilt, in degrees)")]
        [SerializeField] private float pitchAmount = 2.5f;  // nose up / down
        [SerializeField] private float pitchSpeed = 1.1f;
        [SerializeField] private float rollAmount = 3.5f;   // side to side
        [SerializeField] private float rollSpeed = 0.8f;

        [Header("Settle")]
        [Tooltip("How quickly the motion eases in/out when undocking / docking.")]
        [SerializeField] private float settleSpeed = 2f;

        private Vector3 _basePos;
        private Quaternion _baseRot;
        private BoatController _boatController;
        private float _amplitude;
        private bool _lastAfloatState;

        private void Awake()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
            _boatController = GetComponentInParent<BoatController>() ?? GetComponent<BoatController>();
            Debug.Log($"[BoatBob] Awake: _basePos={_basePos}, _boatController={(_boatController != null ? _boatController.name : "NULL")}");
        }

        private void LateUpdate()
        {
            // Determine afloat state based on BoatController docking state
            bool afloat = _boatController == null || !_boatController.IsDocked;

            if (afloat != _lastAfloatState)
            {
                _lastAfloatState = afloat;
                Debug.Log($"[BoatBob] Afloat state changed: afloat={afloat}");
            }

            _amplitude = Mathf.MoveTowards(_amplitude, afloat ? 1f : 0f, settleSpeed * Time.deltaTime);

            if (_amplitude <= 0.001f)
            {
                transform.localPosition = _basePos;
                transform.localRotation = _baseRot;
                return;
            }

            float t = Time.time;
            float bob   = Mathf.Sin(t * bobSpeed) * bobHeight * _amplitude;
            float pitch = Mathf.Sin(t * pitchSpeed) * pitchAmount * _amplitude;
            float roll  = Mathf.Sin(t * rollSpeed + 1.3f) * rollAmount * _amplitude;

            transform.localPosition = _basePos + Vector3.up * bob;
            transform.localRotation = _baseRot * Quaternion.Euler(pitch, 0f, roll);

            // Log periodic motion output every ~2 seconds for debugging
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"[BoatBob] Active: amplitude={_amplitude:F2}, bob={bob:F3}m, pitch={pitch:F1}°, roll={roll:F1}°");
            }
        }
    }
}