using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns random debris obstacles inside the play area at scene start.
/// Attach to an empty "DebrisSpawner" GameObject.
/// If no prefab is assigned, a simple placeholder circle is generated at runtime,
/// so this works with zero art assets.
/// </summary>
public class DebrisSpawner : MonoBehaviour
{
    [Header("References")]
    public PlayAreaBounds playArea;
    public Transform rod;
    public Transform fishStart;
    [Tooltip("Optional. If left empty, a simple placeholder circle is generated at runtime.")]
    public GameObject debrisPrefab;

    [Header("Spawn Settings")]
    public int debrisCount = 6;
    public float debrisRadius = 0.4f;
    [Tooltip("Minimum gap kept clear around the rod and the fish's starting point.")]
    public float safeRadius = 1.5f;
    [Tooltip("Minimum spacing between debris pieces so they don't stack on top of each other.")]
    public float minSpacing = 1f;
    public int maxPlacementAttempts = 30;

    private static Sprite _generatedSprite;

    private void Start()
    {
        SpawnAll();
    }

    public void SpawnAll()
    {
        var placed = new List<Vector2>();
        Bounds b = playArea.Bounds;

        for (int i = 0; i < debrisCount; i++)
        {
            Vector2? pos = FindValidPosition(b, placed);
            if (pos == null) continue; // couldn't find a free spot after several tries, skip it

            placed.Add(pos.Value);
            SpawnOne(pos.Value);
        }
    }

    private Vector2? FindValidPosition(Bounds b, List<Vector2> placed)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            float x = Random.Range(b.min.x + debrisRadius, b.max.x - debrisRadius);
            float y = Random.Range(b.min.y + debrisRadius, b.max.y - debrisRadius);
            Vector2 candidate = new Vector2(x, y);

            if (rod != null && Vector2.Distance(candidate, rod.position) < safeRadius) continue;
            if (fishStart != null && Vector2.Distance(candidate, fishStart.position) < safeRadius) continue;

            bool tooClose = false;
            foreach (var p in placed)
            {
                if (Vector2.Distance(candidate, p) < minSpacing) { tooClose = true; break; }
            }
            if (tooClose) continue;

            return candidate;
        }
        return null;
    }

    private void SpawnOne(Vector2 pos)
    {
        GameObject go = debrisPrefab != null
            ? Instantiate(debrisPrefab, pos, Quaternion.identity)
            : CreatePlaceholderDebris(pos);

        go.transform.position = pos;
        go.transform.SetParent(transform);

        var col = go.GetComponent<Collider2D>();
        if (col == null)
        {
            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = debrisRadius;
            col = circle;
        }
        col.isTrigger = true;

        var debris = go.GetComponent<Debris>();
        if (debris == null) debris = go.AddComponent<Debris>();
        debris.playArea = playArea;

        go.tag = "Debris";
    }

    private GameObject CreatePlaceholderDebris(Vector2 pos)
    {
        var go = new GameObject("Debris");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetGeneratedSprite();
        sr.color = new Color(0.45f, 0.3f, 0.15f); // driftwood brown
        go.transform.localScale = Vector3.one * (debrisRadius * 2f);

        return go;
    }

    // Draws a simple filled circle texture at runtime so the spawner works with no art assets.
    private static Sprite GetGeneratedSprite()
    {
        if (_generatedSprite != null) return _generatedSprite;

        int size = 64;
        var tex = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= size / 2f ? Color.white : Color.clear);
            }
        }
        tex.Apply();

        _generatedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _generatedSprite;
    }
}
