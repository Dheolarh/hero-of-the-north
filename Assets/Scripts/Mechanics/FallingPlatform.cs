using System.Collections;
using UnityEngine;

/// <summary>
/// Makes a platform fall under physics when the player steps on it.
///
/// HOW TO SET UP IN UNITY:
///  1. On the platform GameObject add:
///       - Collider2D  (BoxCollider2D / PolygonCollider2D) — NOT a trigger
///       - Rigidbody2D — set Body Type to "Kinematic" initially
///       - This script (FallingPlatform)
///  2. Tag the platform's collider object with "PlatformGround" so the
///     player's IsGrounded() check still works while standing on it.
///  3. Tune the Inspector fields to taste.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FallingPlatform : MonoBehaviour
{
    [Header("Fall Settings")]
    [Tooltip("Seconds the player must stand on the platform before it falls.")]
    [Range(0f, 3f)]
    public float fallDelay = 0.8f;

    [Tooltip("Gravity scale applied to the Rigidbody2D when the platform falls.\n" +
             "Higher = falls faster.")]
    public float fallGravityScale = 3f;

    [Header("Wobble Warning")]
    [Tooltip("Shake the platform before it falls to warn the player.")]
    public bool wobbleBeforeFall = true;

    [Tooltip("How far the platform wobbles left/right (world units).")]
    public float wobbleAmount = 0.06f;

    [Tooltip("How fast the wobble oscillates (Hz).")]
    public float wobbleSpeed = 30f;

    [Header("Reset")]
    [Tooltip("Automatically reset the platform to its original position after falling.")]
    public bool autoReset = true;

    [Tooltip("Seconds after falling before the platform resets.\n" +
             "Only used when Auto Reset is enabled.")]
    public float resetDelay = 3f;

    // ── Private state ─────────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool playerIsOn   = false;
    private bool hasFallen    = false;
    private Coroutine fallRoutine;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Start fully kinematic — no physics until we want it to fall
        rb.bodyType    = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    // ── Collision detection ───────────────────────────────────────────────────

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasFallen) return;

        // Only react to the player landing on TOP of the platform
        if (collision.collider.CompareTag("Player") && IsPlayerAbove(collision))
        {
            playerIsOn  = true;
            fallRoutine ??= StartCoroutine(FallSequence());
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
            playerIsOn = false;
        // Note: we do NOT cancel the fall coroutine — once started the
        // platform will still fall even if the player jumps off.
    }

    // ── Fall sequence ─────────────────────────────────────────────────────────

    private IEnumerator FallSequence()
    {
        // ── Phase 1: optional wobble warning ─────────────────────────────────
        if (wobbleBeforeFall && fallDelay > 0f)
        {
            float elapsed  = 0f;
            Vector3 origin = transform.position;

            while (elapsed < fallDelay)
            {
                float xOffset = Mathf.Sin(elapsed * wobbleSpeed) * wobbleAmount;
                transform.position = origin + new Vector3(xOffset, 0f, 0f);

                elapsed      += Time.deltaTime;
                yield return null;
            }

            // Snap back to original X before falling
            transform.position = origin;
        }
        else
        {
            yield return new WaitForSeconds(fallDelay);
        }

        // ── Phase 2: fall ─────────────────────────────────────────────────────
        hasFallen = true;
        rb.bodyType    = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;

        // ── Phase 3: optional auto-reset ─────────────────────────────────────
        if (autoReset)
        {
            yield return new WaitForSeconds(resetDelay);
            ResetPlatform();
        }
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    private void ResetPlatform()
    {
        // Stop physics
        rb.bodyType    = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity        = Vector2.zero;
        rb.angularVelocity = 0f;

        // Restore position and rotation
        transform.position = startPosition;
        transform.rotation = startRotation;

        // Reset state
        hasFallen   = false;
        playerIsOn  = false;
        fallRoutine = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true only when the player contact point is above the platform centre —
    /// prevents a wall-side or below-platform collision from triggering the fall.
    /// </summary>
    private bool IsPlayerAbove(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Contact normal pointing upward (platform surface normal faces up)
            if (contact.normal.y < -0.5f)
                return true;
        }
        return false;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Show the reset/spawn position as a green wireframe
        Gizmos.color = Color.green;
        if (Application.isPlaying)
            Gizmos.DrawWireCube(startPosition, transform.localScale);
        else
            Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
