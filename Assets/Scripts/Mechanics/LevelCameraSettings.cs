using UnityEngine;

[ExecuteAlways]
public class LevelCameraSettings : MonoBehaviour
{
    [Header("Follow Settings")]
    public bool followX = true;
    public bool followY = true;
    
    [Header("Offset")]
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Smoothing")]
    public bool useSmoothing = true;
    public float smoothSpeed = 5f;

    [Header("Fixed Y Height (if Follow Y is false)")]
    [Tooltip("If Follow Y is unchecked, the camera's Y position will be locked to this height.")]
    public float fixedYHeight = 0f;

    [Header("Scene View Gizmo Preview")]
    public bool drawPreview = true;
    public Color previewColor = Color.cyan;
    [Range(1f, 20f)] public float orthoSize = 5f;
    public float aspectRatio = 16f / 9f;

    private void Update()
    {
        // Keep fixedYHeight in sync with the transform's Y position for easy visual positioning
        if (!followY)
        {
            fixedYHeight = transform.position.y;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawPreview) return;

        // Try to find a player in the children of this prefab to draw the camera preview relative to them
        PlayerController player = GetComponentInChildren<PlayerController>();
        if (player == null) return;

        Gizmos.color = previewColor;

        Vector3 playerPos = player.transform.position;
        float camX = followX ? (playerPos.x + offset.x) : transform.position.x + offset.x;
        float camY = followY ? (playerPos.y + offset.y) : fixedYHeight;
        float camZ = playerPos.z + offset.z;

        Vector3 cameraPos = new Vector3(camX, camY, camZ);

        // Draw camera anchor point
        Gizmos.DrawSphere(cameraPos, 0.25f);
        Gizmos.DrawLine(playerPos, cameraPos);

        // Draw camera frame boundary (Orthographic)
        float height = orthoSize * 2f;
        float width = height * aspectRatio;
        Gizmos.DrawWireCube(cameraPos, new Vector3(width, height, 0.1f));

        #if UNITY_EDITOR
        UnityEditor.Handles.color = previewColor;
        UnityEditor.Handles.Label(cameraPos + Vector3.up * (orthoSize + 0.2f), "Camera Preview Frame");
        #endif
    }
}
