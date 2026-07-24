using System.Collections.Generic;
using UnityEngine;
using LittleTrawling.Vehicles;

namespace LittleTrawling.Environment
{
    /// <summary>
    /// Put this on a Dock object to define a docking area.
    /// Assign the Berth transform where boats should align when docked.
    /// </summary>
    public class Dock : MonoBehaviour
    {
        [Tooltip("Target transform where the boat will align when docked.")]
        [SerializeField] private Transform berth;

        [Tooltip("Optional collider defining the docking zone hitbox. If unassigned, automatically finds a collider on this object or its children.")]
        [SerializeField] private Collider dockingHitbox;

        [Tooltip("Maximum distance from the berth to consider the boat inside docking range as a fallback.")]
        [SerializeField] private float berthDockingRadius = 6.0f;

        private readonly HashSet<Collider> _occupyingColliders = new HashSet<Collider>();

        public Transform Berth => berth != null ? berth : transform;

        public Collider Hitbox
        {
            get
            {
                if (dockingHitbox == null)
                    dockingHitbox = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
                return dockingHitbox;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var boat = other.GetComponentInParent<BoatController>() ?? other.GetComponent<BoatController>();
            if (boat != null)
            {
                _occupyingColliders.Add(other);
                boat.CurrentDockZone = this;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            var boat = other.GetComponentInParent<BoatController>() ?? other.GetComponent<BoatController>();
            if (boat != null)
            {
                _occupyingColliders.Add(other);
                if (boat.CurrentDockZone != this)
                {
                    boat.CurrentDockZone = this;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var boat = other.GetComponentInParent<BoatController>() ?? other.GetComponent<BoatController>();
            if (boat != null)
            {
                _occupyingColliders.Remove(other);
                if (_occupyingColliders.Count == 0 && boat.CurrentDockZone == this)
                {
                    boat.CurrentDockZone = null;
                }
            }
        }

        public bool IsBoatInside(BoatController boat)
        {
            if (boat == null) return false;

            // 1. Check active trigger occupation
            _occupyingColliders.RemoveWhere(c => c == null || !c.enabled);
            if (_occupyingColliders.Count > 0) return true;

            // 2. Check Hitbox collider bounds (searches self and children)
            Collider col = Hitbox;
            if (col != null && col.enabled)
            {
                Vector3 boatPos = boat.transform.position;
                if (col.bounds.Contains(boatPos)) return true;

                Vector3 closest = col.bounds.ClosestPoint(boatPos);
                if (Vector3.Distance(boatPos, closest) <= 1.5f) return true;

                Collider[] boatColliders = boat.GetComponentsInChildren<Collider>();
                foreach (var bCol in boatColliders)
                {
                    if (bCol != null && bCol.enabled && col.bounds.Intersects(bCol.bounds))
                    {
                        return true;
                    }
                }
            }

            // 3. Fallback: Check proximity to Berth transform
            Transform targetBerth = Berth;
            if (targetBerth != null)
            {
                float dist = Vector3.Distance(boat.transform.position, targetBerth.position);
                if (dist <= berthDockingRadius) return true;
            }

            return false;
        }
    }
}
