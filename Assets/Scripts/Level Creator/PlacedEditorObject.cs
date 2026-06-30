using UnityEngine;

/// <summary>
/// Attached to every object placed in the editor scene.
/// Holds configuration properties (scale, rotation, trigger target link)
/// which are dynamically edited in UI and serialized to JSON.
/// </summary>
public class PlacedEditorObject : MonoBehaviour
{
    [Header("Asset Info")]
    public string assetTypeName;

    [Header("Properties (Adjusted via UI Properties Panel)")]
    public string moveDir = "Down"; // For spawner/pingpong directions: Up, Down, Left, Right
    public float speed = 3f;
    public float delay = 1f;

    [Header("Trigger Wiring Links")]
    public bool hasTarget = false;
    public PlacedEditorObject targetObject;

    // Reference to visual wire connection in Edit Mode
    [HideInInspector] public LineRenderer wireLine;

    // Helper to get relative transformation data for saving
    public CustomTileData ToTileData()
    {
        return new CustomTileData
        {
            type = assetTypeName,
            position = new Vector2S(transform.position),
            scale = new Vector2S(transform.localScale),
            rotation = transform.eulerAngles.z
        };
    }

    public CustomTrapData ToTrapData()
    {
        CustomTrapData data = new CustomTrapData
        {
            type = assetTypeName,
            spawnPos = new Vector2S(transform.position),
            scale = new Vector2S(transform.localScale),
            rotation = transform.eulerAngles.z,
            moveDir = moveDir,
            speed = speed,
            delay = delay,
            hasTarget = hasTarget
        };

        if (hasTarget && targetObject != null)
        {
            data.targetPos = new Vector2S(targetObject.transform.position);
        }

        return data;
    }
}
