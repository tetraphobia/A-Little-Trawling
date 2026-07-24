using UnityEngine;
using LittleTrawling.Vehicles;

namespace LittleTrawling.Environment
{
    /// <summary>
    /// Put this on a Dock object with a trigger collider to define a docking area.
    /// Assign the Berth transform where boats should align when docked.
    /// </summary>
    public class Dock : MonoBehaviour
    {
        [Tooltip("Target transform where the boat will align when docked.")]
        [SerializeField] private Transform berth;

        public Transform Berth => berth != null ? berth : transform;

        private void OnTriggerEnter(Collider other)
        {
            var boat = other.GetComponentInParent<BoatController>() ?? other.GetComponent<BoatController>();
            if (boat != null)
            {
                boat.CurrentDockZone = this;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            var boat = other.GetComponentInParent<BoatController>() ?? other.GetComponent<BoatController>();
            if (boat != null && boat.CurrentDockZone != this)
            {
                boat.CurrentDockZone = this;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var boat = other.GetComponentInParent<BoatController>() ?? other.GetComponent<BoatController>();
            if (boat != null && boat.CurrentDockZone == this)
            {
                boat.CurrentDockZone = null;
            }
        }
    }
}
