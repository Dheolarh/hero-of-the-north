using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this script to UI Buttons inside your Palette horizontal scroll lists.
/// Enables dragging a prefab out of the UI scroll view and spawning it into the 2D world.
/// </summary>
public class PaletteDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Asset Properties")]
    [Tooltip("The unique type name identifier (must match an entry in GridPainter's Palette registry).")]
    public string assetTypeName;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Don't allow dragging if playtest mode is active
        if (LevelCreatorUI.Instance != null && LevelCreatorUI.Instance.IsPlaytesting) return;

        if (GridPainter.Instance != null)
        {
            GridPainter.Instance.StartDragPlacement(assetTypeName);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (LevelCreatorUI.Instance != null && LevelCreatorUI.Instance.IsPlaytesting) return;

        if (GridPainter.Instance != null && cam != null)
        {
            // Convert screen cursor coordinate to world coordinate
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(eventData.position);
            mouseWorldPos.z = 0f;

            GridPainter.Instance.UpdateDragPlacement(mouseWorldPos);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (LevelCreatorUI.Instance != null && LevelCreatorUI.Instance.IsPlaytesting) return;

        if (GridPainter.Instance != null && cam != null)
        {
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(eventData.position);
            mouseWorldPos.z = 0f;

            GridPainter.Instance.EndDragPlacement(mouseWorldPos);
        }
    }
}
