using System;
using UnityEngine;
[RequireComponent(typeof(Camera))]
public class GridOverlay : MonoBehaviour
{
    [Header("Grid Styling")]
    public bool showGrid = true;
    public float gridSpacingX = 1f;
    public float gridSpacingY = 1f;
    public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

    private Material lineMaterial;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void CreateLineMaterial()
    {
        if (lineMaterial == null)
        {
            // Use Unity's internal basic color shader
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            // Set up transparent alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }

    /// <summary>
    /// OnPostRender is automatically called by Unity after the camera finishes rendering the scene.
    /// Draws the grid lines directly on the GPU using GL commands.
    /// </summary>
    void OnPostRender()
    {
        if (!showGrid) return;

        // Hide grid lines when playtesting
        if (LevelCreatorUI.Instance != null && LevelCreatorUI.Instance.IsPlaytesting) return;

        CreateLineMaterial();
        lineMaterial.SetPass(0);

        GL.Begin(GL.LINES);
        GL.Color(gridColor);

        // Get current camera bounds in world coordinates
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        float minX = cam.transform.position.x - width;
        float maxX = cam.transform.position.x + width;
        float minY = cam.transform.position.y - height;
        float maxY = cam.transform.position.y + height;

        // 1. Draw Vertical Lines
        float startX = Mathf.Floor(minX / gridSpacingX) * gridSpacingX;
        for (float x = startX; x <= maxX; x += gridSpacingX)
        {
            GL.Vertex3(x, minY, 0f);
            GL.Vertex3(x, maxY, 0f);
        }

        // 2. Draw Horizontal Lines
        float startY = Mathf.Floor(minY / gridSpacingY) * gridSpacingY;
        for (float y = startY; y <= maxY; y += gridSpacingY)
        {
            GL.Vertex3(minX, y, 0f);
            GL.Vertex3(maxX, y, 0f);
        }

        GL.End();
    }
}
