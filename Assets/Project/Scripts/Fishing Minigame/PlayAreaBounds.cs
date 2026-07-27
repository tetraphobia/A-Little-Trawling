using UnityEngine;

/// <summary>
/// Defines the rectangular play area for the fishing minigame.
/// Attach to an empty GameObject with a BoxCollider2D.
/// Size and position the BoxCollider2D in the Scene view to match your rectangle
/// (e.g. add a background sprite as a child, sized to match, purely for visuals).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PlayAreaBounds : MonoBehaviour
{
    private BoxCollider2D _collider;

    public Bounds Bounds => _collider.bounds;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true; // never used for physical collision, just geometry
    }

    // Lets you see the rectangle in the Scene view even without a background sprite.
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 0.6f, 1f, 0.15f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
