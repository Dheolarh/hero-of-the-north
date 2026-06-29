using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ProjectileSpawner : MonoBehaviour
{
    // ── Direction ────────────────────────────────────────────────────────────
    public enum SpawnDirection { Down, Up, Left, Right }

    [Header("Object Pool")]
    [Tooltip("The prefab to spawn. A PooledProjectile component is auto-added if missing.")]
    public GameObject prefab;

    [Tooltip("Number of objects to pre-create. Grows automatically if exhausted.")]
    public int poolSize = 15;

    // ── Movement ─────────────────────────────────────────────────────────────
    [Header("Movement")]
    [Tooltip("Which direction spawned objects travel.")]
    public SpawnDirection moveDirection = SpawnDirection.Down;

    [Tooltip("Speed of each spawned object (units/second).")]
    public float moveSpeed = 5f;

    // ── Spawn range ──────────────────────────────────────────────────────────
    [Header("Spawn Range")]
    [Tooltip("Minimum position along the SPAWN axis\n" +
             "(X for Up/Down movement — Y for Left/Right movement).")]
    public float spawnRangeMin = -8f;

    [Tooltip("Maximum position along the SPAWN axis.")]
    public float spawnRangeMax = 8f;

    [Tooltip("Fixed position ON the movement axis where objects are created\n" +
             "e.g. the top Y coordinate for rain falling downward.")]
    public float spawnEdgePosition = 10f;

    // ── Despawn ──────────────────────────────────────────────────────────────
    [Header("Despawn")]
    [Tooltip("Objects are returned to the pool when they cross this position\n" +
             "along the movement axis (e.g. the bottom Y for downward objects).")]
    public float despawnEdgePosition = -12f;

    // ── Gap enforcement ──────────────────────────────────────────────────────
    [Header("Gap / Spacing")]
    [Tooltip("Minimum distance (in world units) between one object's spawn\n" +
             "position and the next. This guarantees a safe lane for the player.")]
    [Range(0.5f, 30f)]
    public float minGapBetweenObjects = 3f;

    [Tooltip("How many seconds between each spawn.")]
    [Range(0.1f, 10f)]
    public float spawnInterval = 1f;

    // ── Activation ───────────────────────────────────────────────────────────
    [Header("Activation")]
    [Tooltip("Begin spawning as soon as the scene starts.")]
    public bool startAutomatically = true;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private bool isSpawning;
    private List<GameObject> pool = new List<GameObject>();
    private float lastSpawnPos = float.NaN;   // last position in the spawn axis

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        InitPool();
        if (startAutomatically)
            Activate();
    }

    // ── Pool management ───────────────────────────────────────────────────────

    private void InitPool()
    {
        for (int i = 0; i < poolSize; i++)
            pool.Add(CreatePooledObject());
    }

    private GameObject CreatePooledObject()
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        if (obj.GetComponent<PooledProjectile>() == null)
            obj.AddComponent<PooledProjectile>();
        return obj;
    }

    private GameObject GetFromPool()
    {
        foreach (var obj in pool)
            if (!obj.activeInHierarchy) return obj;

        // Auto-grow
        GameObject extra = CreatePooledObject();
        pool.Add(extra);
        Debug.Log($"[ProjectileSpawner] Pool grew to {pool.Count}");
        return extra;
    }

    /// <summary>Called by PooledProjectile when it exits the despawn boundary.</summary>
    public void ReturnToPool(GameObject obj) => obj.SetActive(false);

    // ── Spawn loop ────────────────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        while (isSpawning)
        {
            SpawnOne();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOne()
    {
        GameObject obj = GetFromPool();
        if (obj == null) return;

        float spawnPos = PickSpawnPosition();
        lastSpawnPos = spawnPos;

        obj.transform.position = GetSpawnWorldPosition(spawnPos);
        obj.SetActive(true);

        PooledProjectile proj = obj.GetComponent<PooledProjectile>();
        proj?.Initialize(GetMoveVector(), moveSpeed, IsYAxis(), despawnEdgePosition, this);
    }

    // ── Spawn position with gap enforcement ───────────────────────────────────

    private float PickSpawnPosition()
    {
        // First spawn — fully random
        if (float.IsNaN(lastSpawnPos))
            return Random.Range(spawnRangeMin, spawnRangeMax);

        float rangeWidth = spawnRangeMax - spawnRangeMin;

        // Try up to 15 random positions that respect the min gap
        for (int attempt = 0; attempt < 15; attempt++)
        {
            float candidate = Random.Range(spawnRangeMin, spawnRangeMax);
            if (Mathf.Abs(candidate - lastSpawnPos) >= minGapBetweenObjects)
                return candidate;
        }

        // Fallback: flip to the opposite side of the range from the last spawn
        float midpoint = (spawnRangeMin + spawnRangeMax) * 0.5f;
        if (lastSpawnPos > midpoint)
        {
            // Place somewhere in the lower half
            return Random.Range(spawnRangeMin, Mathf.Min(midpoint, spawnRangeMax - minGapBetweenObjects));
        }
        else
        {
            // Place somewhere in the upper half
            return Random.Range(Mathf.Max(midpoint, spawnRangeMin + minGapBetweenObjects), spawnRangeMax);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector3 GetSpawnWorldPosition(float spawnPos)
    {
        return IsYAxis()
            ? new Vector3(spawnPos, spawnEdgePosition, 0f)
            : new Vector3(spawnEdgePosition, spawnPos, 0f);
    }

    private Vector2 GetMoveVector()
    {
        switch (moveDirection)
        {
            case SpawnDirection.Down:  return Vector2.down;
            case SpawnDirection.Up:    return Vector2.up;
            case SpawnDirection.Left:  return Vector2.left;
            case SpawnDirection.Right: return Vector2.right;
            default:                   return Vector2.down;
        }
    }

    /// <summary>True when objects move along the Y axis (Up/Down).</summary>
    private bool IsYAxis() =>
        moveDirection == SpawnDirection.Down || moveDirection == SpawnDirection.Up;

    // ── Public API (also used by trigger system) ──────────────────────────────

    public void Activate()
    {
        if (isSpawning) return;
        isSpawning = true;
        StartCoroutine(SpawnLoop());
    }

    public void Deactivate()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    // ── Scene Gizmos ──────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        bool yAxis = (moveDirection == SpawnDirection.Down || moveDirection == SpawnDirection.Up);

        // Spawn line (blue)
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.7f);
        Vector3 spawnA = yAxis
            ? new Vector3(spawnRangeMin,  spawnEdgePosition,   0f)
            : new Vector3(spawnEdgePosition,  spawnRangeMin,   0f);
        Vector3 spawnB = yAxis
            ? new Vector3(spawnRangeMax,  spawnEdgePosition,   0f)
            : new Vector3(spawnEdgePosition,  spawnRangeMax,   0f);
        Gizmos.DrawLine(spawnA, spawnB);
        Gizmos.DrawSphere(spawnA, 0.25f);
        Gizmos.DrawSphere(spawnB, 0.25f);

        // Despawn line (red)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Vector3 despawnA = yAxis
            ? new Vector3(spawnRangeMin, despawnEdgePosition, 0f)
            : new Vector3(despawnEdgePosition, spawnRangeMin, 0f);
        Vector3 despawnB = yAxis
            ? new Vector3(spawnRangeMax, despawnEdgePosition, 0f)
            : new Vector3(despawnEdgePosition, spawnRangeMax, 0f);
        Gizmos.DrawLine(despawnA, despawnB);

        // Min-gap indicator (yellow bar at mid spawn edge)
        Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
        Vector3 mid = (spawnA + spawnB) * 0.5f;
        Vector3 gapSize = yAxis
            ? new Vector3(minGapBetweenObjects, 0.25f, 0f)
            : new Vector3(0.25f, minGapBetweenObjects, 0f);
        Gizmos.DrawWireCube(mid, gapSize);
    }
}
