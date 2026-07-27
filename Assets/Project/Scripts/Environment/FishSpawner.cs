using System.Collections.Generic;
using UnityEngine;
using LittleTrawling.Entities;

namespace LittleTrawling.Environment
{
    /// <summary>
    /// Spawns and manages fish schools in open ocean water.
    /// </summary>
    public class FishSpawner : MonoBehaviour
    {
        public static FishSpawner Instance { get; private set; }

        [Header("Spawner Configuration")]
        [Tooltip("Target number of active fish schools in the ocean.")]
        [SerializeField] private int maxSchools = 6;

        [Tooltip("Spawn area radius from ocean origin.")]
        [SerializeField] private float spawnRadius = 80.0f;

        [Tooltip("Safety distance from land colliders.")]
        [SerializeField] private float landSafetyRadius = 8.0f;

        private readonly List<FishSchool> _activeSchools = new List<FishSchool>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitialSpawn();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void InitialSpawn()
        {
            Debug.Log($"[FishSpawner] InitialSpawn starting... Target maxSchools={maxSchools}, spawnRadius={spawnRadius}m");
            int spawnedCount = 0;
            for (int i = 0; i < maxSchools; i++)
            {
                if (TrySpawnSchool()) spawnedCount++;
            }
            Debug.Log($"[FishSpawner] InitialSpawn complete! Successfully spawned {spawnedCount}/{maxSchools} fish schools.");
        }

        private void Update()
        {
            // Clean up missing/destroyed schools
            _activeSchools.RemoveAll(s => s == null);

            // Replenish schools if below target count
            if (_activeSchools.Count < maxSchools)
            {
                TrySpawnSchool();
            }
        }

        public bool TrySpawnSchool()
        {
            Vector3 center = transform.position;
            var dock = Object.FindAnyObjectByType<Dock>();
            if (dock != null) center = dock.transform.position;

            for (int attempt = 0; attempt < 25; attempt++)
            {
                Vector2 circle = Random.insideUnitCircle * spawnRadius;
                Vector3 candidatePos = new Vector3(center.x + circle.x, 0f, center.z + circle.y);

                // Reject if candidate position is too close to land or dock
                if (IsPositionOnLandOrDock(candidatePos))
                {
                    continue;
                }

                var schoolObj = new GameObject($"FishSchool_{_activeSchools.Count + 1}");
                schoolObj.transform.position = candidatePos;
                var school = schoolObj.AddComponent<FishSchool>();
                _activeSchools.Add(school);
                Debug.Log($"[FishSpawner] Successfully spawned school #{_activeSchools.Count} at {candidatePos}");
                return true;
            }

            Debug.LogWarning($"[FishSpawner] Failed to find valid spawn location after 25 attempts! Active schools count: {_activeSchools.Count}");
            return false;
        }

        private bool IsPositionOnLandOrDock(Vector3 pos)
        {
            Vector3 origin = pos + Vector3.up * 50f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f))
            {
                if (hit.collider == null || hit.collider.isTrigger) return false;

                // Reject if near dock
                if (hit.collider.name.Contains("Dock") || hit.collider.GetComponentInParent<Dock>() != null)
                {
                    return true;
                }

                // Reject if terrain/mesh elevation is above water level (y > 0.3m = Island / Shore)
                if (hit.point.y > 0.3f)
                {
                    return true;
                }
            }
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureFishSpawner()
        {
            if (Object.FindAnyObjectByType<FishSpawner>() == null)
            {
                var spawnerObj = new GameObject("FishSpawner");
                spawnerObj.AddComponent<FishSpawner>();
            }
        }
    }
}
