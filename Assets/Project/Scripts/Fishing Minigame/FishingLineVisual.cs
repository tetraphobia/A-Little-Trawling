using UnityEngine;

/// <summary>
/// Draws a line from the rod to the fish every frame using a LineRenderer.
/// Attach to the Rod GameObject (or any dedicated GameObject) with a LineRenderer component.
/// Remember to assign a material (e.g. "Sprites-Default") on the LineRenderer so it renders.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class FishingLineVisual : MonoBehaviour
{
    public Transform rod;
    public Transform fish;

    private LineRenderer _line;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.widthMultiplier = 0.05f;
    }

    private void LateUpdate()
    {
        if (rod == null || fish == null) return;
        _line.SetPosition(0, rod.position);
        _line.SetPosition(1, fish.position);
    }
}
