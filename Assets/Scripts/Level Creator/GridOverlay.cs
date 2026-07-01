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

        // 3. Draw Selected Object Outline (Red border overlay)
        var selectedObj = GridPainter.Instance != null ? GridPainter.Instance.GetSelectedObject() : null;
        if (selectedObj != null)
        {
            var spriteRenderer = selectedObj.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Bounds bounds = spriteRenderer.bounds;
                float pad = 0.05f;
                Vector3 min = bounds.min - new Vector3(pad, pad, 0f);
                Vector3 max = bounds.max + new Vector3(pad, pad, 0f);

                GL.Begin(GL.LINES);
                GL.Color(Color.red);

                // Bottom line
                GL.Vertex3(min.x, min.y, 0f);
                GL.Vertex3(max.x, min.y, 0f);

                // Right line
                GL.Vertex3(max.x, min.y, 0f);
                GL.Vertex3(max.x, max.y, 0f);

                // Top line
                GL.Vertex3(max.x, max.y, 0f);
                GL.Vertex3(min.x, max.y, 0f);

                // Left line
                GL.Vertex3(min.x, max.y, 0f);
                GL.Vertex3(min.x, min.y, 0f);

                GL.End();
            }
        }
    }
}
