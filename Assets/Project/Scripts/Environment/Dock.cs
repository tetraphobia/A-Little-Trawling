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

        [Tooltip("Maximum distance from the berth to consider the boat inside docking range as a fallback when no collider is present.")]
        [SerializeField] private float berthDockingRadius = 2.5f;

        private readonly HashSet<Collider> _occupyingColliders = new HashSet<Collider>();

        public Transform Berth => berth != null ? berth : transform;

        private void Start()
        {
            Transform b = Berth;
            Debug.Log($"[Dock] '{name}' initialized. Dock WorldPos={transform.position}, Berth Name='{b.name}', Berth WorldPos={b.position}, Berth LocalPos={b.localPosition}, Berth Rot={b.eulerAngles}");
        }

        public Collider Hitbox
        {
            get
            {
                if (dockingHitbox == null)
                {
                    Collider[] colliders = GetComponentsInChildren<Collider>();
                    foreach (var c in colliders)
                    {
                        if (c != null && c.isTrigger)
                        {
                            dockingHitbox = c;
                            break;
                        }
                    }
                    if (dockingHitbox == null)
                        dockingHitbox = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
                }
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

                Collider[] boatColliders = boat.GetComponentsInChildren<Collider>();
                foreach (var bCol in boatColliders)
                {
                    if (bCol != null && bCol.enabled && col.bounds.Intersects(bCol.bounds))
                    {
                        return true;
                    }
                }

                return false;
            }

            // 3. Fallback: Check proximity to Berth transform (only when no Hitbox collider exists)
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
