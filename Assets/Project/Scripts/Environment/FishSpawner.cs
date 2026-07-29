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
            for (int i = 0; i < maxSchools; i++)
            {
                TrySpawnSchool();
            }
        }

        private void Update()
        {
            _activeSchools.RemoveAll(s => s == null);

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

                if (IsPositionOnLandOrDock(candidatePos))
                {
                    continue;
                }

                var schoolObj = new GameObject($"FishSchool_{_activeSchools.Count + 1}");
                schoolObj.transform.position = candidatePos;
                var school = schoolObj.AddComponent<FishSchool>();
                _activeSchools.Add(school);
                return true;
            }

            return false;
        }

        private bool IsPositionOnLandOrDock(Vector3 pos)
        {
            Vector3 origin = pos + Vector3.up * 50f;
            if (Physics.SphereCast(origin, landSafetyRadius, Vector3.down, out RaycastHit hit, 100f))
            {
                if (hit.collider == null || hit.collider.isTrigger) return false;

                if (hit.collider.name.Contains("Dock") || hit.collider.GetComponentInParent<Dock>() != null)
                {
                    return true;
                }

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
