using UnityEngine;

/// <summary>
/// A single piece of debris. Usually added automatically by DebrisSpawner,
/// but you can also drop this on your own prefab.
/// Requires the "Debris" tag to exist in Project Settings > Tags and Layers.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class Debris : MonoBehaviour
{
    [Header("Drift")]
    public bool drifts = true;
    public float driftSpeed = 0.6f;

    [HideInInspector] public PlayAreaBounds playArea;

    private Vector2 _direction;

    private void Start()
    {
        gameObject.tag = "Debris";
        GetComponent<Collider2D>().isTrigger = true;
        _direction = Random.insideUnitCircle.normalized;
    }

    private void Update()
    {
        if (!drifts || playArea == null) return;

        Vector2 pos = (Vector2)transform.position + _direction * driftSpeed * Time.deltaTime;
        Bounds b = playArea.Bounds;

        // Simple bounce off the rectangle edges.
        if (pos.x < b.min.x || pos.x > b.max.x) _direction.x *= -1f;
        if (pos.y < b.min.y || pos.y > b.max.y) _direction.y *= -1f;

        pos.x = Mathf.Clamp(pos.x, b.min.x, b.max.x);
        pos.y = Mathf.Clamp(pos.y, b.min.y, b.max.y);

        transform.position = pos;
    }
}
