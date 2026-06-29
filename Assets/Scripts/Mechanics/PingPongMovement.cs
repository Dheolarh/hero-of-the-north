using UnityEngine;

public enum PingPongDirection
{
    Horizontal,
    Vertical
}

public class PingPongMovement : MonoBehaviour
{
    [Header("Direction Settings")]
    [Tooltip("Choose whether the object moves horizontally (Left/Right) or vertically (Up/Down).")]
    public PingPongDirection movementDirection = PingPongDirection.Horizontal;

    [Header("Movement Limits (Relative to Start)")]
    [Tooltip("How far left (Horizontal) or down (Vertical) the object can move from its starting position (must be positive).")]
    public float maxLeftOffset = 5f;
    [Tooltip("How far right (Horizontal) or up (Vertical) the object can move from its starting position (must be positive).")]
    public float maxRightOffset = 5f;

    [Header("Speed Settings")]
    [Tooltip("Speed of the movement.")]
    public float speed = 3f;

    [Header("Activation Settings")]
    [Tooltip("Should the object start moving immediately? If false, it waits for a trigger activation.")]
    public bool startAutomatically = true;

    [Header("Sticky Platform Settings")]
    [Tooltip("If checked, the player will stick to and move with the platform when standing on it.")]
    public bool playerMovesWithPlatform = true;

    private Vector3 startPosition;
    private bool isActivated = false;
    private int direction = 1; // 1 = right/up, -1 = left/down
    private float currentOffset = 0f;
    private Transform originalPlayerParent;

    void Start()
    {
        // Cache the start position
        startPosition = transform.position;
        isActivated = startAutomatically;
    }

    void Update()
    {
        if (!isActivated) return;

        // Advance the offset based on speed and direction
        currentOffset += direction * speed * Time.deltaTime;

        // Check bounds and reverse direction
        if (direction > 0 && currentOffset >= maxRightOffset)
        {
            currentOffset = maxRightOffset;
            direction = -1; // Switch to negative direction (left/down)
        }
        else if (direction < 0 && currentOffset <= -maxLeftOffset)
        {
            currentOffset = -maxLeftOffset;
            direction = 1; // Switch to positive direction (right/up)
        }

        // Apply the position relative to start
        if (movementDirection == PingPongDirection.Horizontal)
        {
            transform.position = new Vector3(startPosition.x + currentOffset, transform.position.y, transform.position.z);
        }
        else
        {
            transform.position = new Vector3(transform.position.x, startPosition.y + currentOffset, transform.position.z);
        }
    }

    public void Activate()
    {
        isActivated = true;
    }

    public void Deactivate()
    {
        isActivated = false;
    }

    // ── Sticky Platform Logic ─────────────────────────────────────────────

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (playerMovesWithPlatform && collision.gameObject.CompareTag("Player"))
        {
            // Verify player is on top of the platform (player Y position is above platform Y position)
            if (collision.transform.position.y > transform.position.y)
            {
                // Remember original parent if not already parented to this platform
                if (collision.transform.parent != transform)
                {
                    originalPlayerParent = collision.transform.parent;
                }
                collision.transform.SetParent(transform);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (playerMovesWithPlatform && collision.gameObject.CompareTag("Player"))
        {
            // Only unparent if the player is currently parented to this platform
            if (collision.transform.parent == transform)
            {
                collision.transform.SetParent(originalPlayerParent);
            }
        }
    }

    private void OnDisable()
    {
        // Safety check: if the platform is disabled/destroyed, release the player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.transform.parent == transform)
        {
            player.transform.SetParent(originalPlayerParent);
        }
    }
}
