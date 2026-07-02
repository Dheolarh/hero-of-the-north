using UnityEngine;

/// <summary>
/// A versatile 2D teleporter script.
/// Teleports a target object (e.g. Player) from Point A (source trigger position or offset)
/// to Point B (defined as a local offset relative to this GameObject).
/// Works with both Physics rigidbodies and normal Transforms.
/// </summary>
public class Teleporter2D : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("If true, teleportation is triggered automatically when a target enters the 2D trigger collider.")]
    [SerializeField] private bool triggerOnTriggerEnter = true;
    [Tooltip("Tag of objects allowed to trigger teleportation.")]
    [SerializeField] private string targetTag = "Player";

    [Header("Point A (Source) Settings")]
    [Tooltip("If true, uses the object's entry into the trigger collider as Point A. If false, checks distance to a custom Point A offset.")]
    [SerializeField] private bool useTriggerPositionAsPointA = true;
    [Tooltip("Local offset representing Point A if not using the default trigger position.")]
    [SerializeField] private Vector2 localPointA = Vector2.zero;
    [Tooltip("Distance threshold required to trigger if useTriggerPositionAsPointA is false.")]
    [SerializeField] private float activationRadius = 0.8f;

    [Header("Point B (Destination) Settings")]
    [Tooltip("Point B defined as a local offset relative to this GameObject.")]
    [SerializeField] private Vector2 localPointB = new Vector2(5f, 0f);
    [Tooltip("If true, resets Rigidbody2D velocity upon teleporting to prevent physics overshoot.")]
    [SerializeField] private bool resetRigidbodyVelocity = true;

    [Header("Visual Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;

    /// <summary>
    /// Returns the world position of Point A.
    /// </summary>
    public Vector3 WorldPointA => useTriggerPositionAsPointA ? transform.position : transform.TransformPoint(localPointA);

    /// <summary>
    /// Returns the world position of Point B.
    /// </summary>
    public Vector3 WorldPointB => transform.TransformPoint(localPointB);

    private void Update()
    {
        // If trigger collider is not used, check distance in Update
        if (!triggerOnTriggerEnter && !useTriggerPositionAsPointA)
        {
            var colliders = Physics2D.OverlapCircleAll(WorldPointA, activationRadius);
            foreach (var col in colliders)
            {
                if (col.CompareTag(targetTag))
                {
                    Teleport(col.gameObject);
                    break;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnTriggerEnter)
        {
            if (other.CompareTag(targetTag) || other.transform.parent?.CompareTag(targetTag) == true)
            {
                // Resolve to the parent game object carrying physics if tagged correctly
                GameObject target = other.gameObject;
                if (other.transform.parent != null && other.transform.parent.CompareTag(targetTag))
                {
                    target = other.transform.parent.gameObject;
                }

                if (useTriggerPositionAsPointA)
                {
                    Teleport(target);
                }
                else
                {
                    // Verify target is close enough to custom Point A before teleporting
                    float dist = Vector2.Distance(target.transform.position, WorldPointA);
                    if (dist <= activationRadius)
                    {
                        Teleport(target);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Performs the teleportation of the target object to Point B.
    /// Can also be invoked via public triggers or UI events.
    /// </summary>
    public void Teleport(GameObject target)
    {
        if (target == null) return;

        Vector3 dest = WorldPointB;
        Debug.Log($"[Teleporter2D] Teleporting '{target.name}' to Point B: {dest}");

        var rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Teleport physics body safely
            rb.position = dest;
            if (resetRigidbodyVelocity)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        else
        {
            // Teleport normal transform
            target.transform.position = dest;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;

        Vector3 ptA = WorldPointA;
        Vector3 ptB = WorldPointB;

        // Draw Point A marker
        Gizmos.DrawWireSphere(ptA, useTriggerPositionAsPointA ? 0.3f : activationRadius);
        
        // Draw path line from Point A to Point B
        Gizmos.DrawLine(ptA, ptB);

        // Draw Point B direction arrow/end-cap
        Gizmos.DrawWireSphere(ptB, 0.4f);
        Gizmos.DrawLine(ptB, ptB + Vector3.up * 0.3f);
        Gizmos.DrawLine(ptB, ptB + Vector3.down * 0.3f);
        Gizmos.DrawLine(ptB, ptB + Vector3.left * 0.3f);
        Gizmos.DrawLine(ptB, ptB + Vector3.right * 0.3f);
    }
}
