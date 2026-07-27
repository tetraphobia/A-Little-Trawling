using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the fish with arrow keys using the new Input System package,
/// keeps it inside the play area rectangle, and reacts to debris hits.
/// Attach to the "Fish" GameObject alongside a Rigidbody2D and a Collider2D.
/// Requires the com.unity.inputsystem package (already needed if your project's
/// Active Input Handling is set to "Input System Package (New)" or "Both").
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FishController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Play Area")]
    public PlayAreaBounds playArea;
    [Tooltip("Keeps the fish sprite fully inside the rectangle instead of letting its center touch the edge.")]
    public float edgePadding = 0.3f;

    [Header("Line / Health")]
    [Tooltip("How many debris hits the fish can take before the line snaps.")]
    public int maxHits = 3;
    public float knockbackForce = 4f;
    public float knockbackDuration = 0.2f;

    public int HitsTaken { get; private set; }

    private Rigidbody2D _rb;
    private float _knockbackTimer;
    private Vector2 _knockbackVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic; // we drive movement manually
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void Update()
    {
        if (FishingGameManager.Instance != null && FishingGameManager.Instance.State != GameStateFishing.Playing)
            return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 input = ReadArrowKeys();
        input = Vector2.ClampMagnitude(input, 1f);

        Vector2 velocity;
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.deltaTime;
            velocity = _knockbackVelocity;
        }
        else
        {
            velocity = input * moveSpeed;
        }

        Vector2 nextPos = _rb.position + velocity * Time.deltaTime;
        nextPos = ClampToBounds(nextPos);
        _rb.MovePosition(nextPos);
    }

    private Vector2 ReadArrowKeys()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero; // no keyboard detected

        float x = 0f;
        float y = 0f;

        if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) x -= 1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) x += 1f;
        if (kb.upArrowKey.isPressed || kb.wKey.isPressed) y += 1f;
        if (kb.downArrowKey.isPressed || kb.sKey.isPressed) y -= 1f;

        return new Vector2(x, y);
    }

    private Vector2 ClampToBounds(Vector2 pos)
    {
        if (playArea == null) return pos;

        Bounds b = playArea.Bounds;
        float minX = b.min.x + edgePadding;
        float maxX = b.max.x - edgePadding;
        float minY = b.min.y + edgePadding;
        float maxY = b.max.y - edgePadding;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        return pos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Debris")) return;
        TakeHit(other.transform.position);
    }

    private void TakeHit(Vector2 sourcePos)
    {
        HitsTaken++;

        Vector2 away = ((Vector2)transform.position - sourcePos);
        if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle; // avoid a zero vector
        away.Normalize();

        _knockbackVelocity = away * knockbackForce;
        _knockbackTimer = knockbackDuration;

        if (FishingGameManager.Instance != null)
        {
            FishingGameManager.Instance.RegisterHit();
            if (HitsTaken >= maxHits)
                FishingGameManager.Instance.Lose("Too many snags on debris.");
        }
    }
}
