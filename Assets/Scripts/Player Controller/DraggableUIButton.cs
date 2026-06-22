using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Allows a UI button to be dragged around the screen, but only when Edit Mode is active.
/// Caches its original position so it can revert if the user cancels the edit.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggableUIButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Unique ID for saving to PlayerPrefs (e.g., "LeftButton", "RightButton")
    [Tooltip("A unique string ID used to save this button's position in PlayerPrefs.")]
    public string buttonID;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private Canvas parentCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Call this from the Editor script when entering edit mode to cache the pre-edit position.
    /// </summary>
    public void CacheOriginalPosition()
    {
        originalAnchoredPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// Call this from the Editor script when cancelling to revert to the cached position.
    /// </summary>
    public void RevertToOriginalPosition()
    {
        rectTransform.anchoredPosition = originalAnchoredPosition;
    }

    /// <summary>
    /// Loads the saved position from PlayerPrefs if it exists.
    /// </summary>
    public void LoadSavedPosition()
    {
        if (string.IsNullOrEmpty(buttonID))
        {
            Debug.LogWarning($"[DraggableUIButton] Button ID is empty on {gameObject.name}. Cannot load position.");
            return;
        }

        if (PlayerPrefs.HasKey(buttonID + "_X") && PlayerPrefs.HasKey(buttonID + "_Y"))
        {
            float x = PlayerPrefs.GetFloat(buttonID + "_X");
            float y = PlayerPrefs.GetFloat(buttonID + "_Y");
            rectTransform.anchoredPosition = new Vector2(x, y);
        }
    }

    /// <summary>
    /// Saves the current anchored position to PlayerPrefs.
    /// </summary>
    public void SaveCurrentPosition()
    {
        if (string.IsNullOrEmpty(buttonID)) return;

        PlayerPrefs.SetFloat(buttonID + "_X", rectTransform.anchoredPosition.x);
        PlayerPrefs.SetFloat(buttonID + "_Y", rectTransform.anchoredPosition.y);
        PlayerPrefs.Save();
    }

    // --- Drag Handling ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Only allow drag if we are in Edit Mode
        if (HUDControlsEditor.Instance == null || !HUDControlsEditor.Instance.IsEditMode)
            return;

        // Optional: reduce alpha or show dragging feedback
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (HUDControlsEditor.Instance == null || !HUDControlsEditor.Instance.IsEditMode)
            return;

        // Move the UI element relative to the canvas scale
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
        }
        else
        {
            rectTransform.anchoredPosition += eventData.delta;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (HUDControlsEditor.Instance == null || !HUDControlsEditor.Instance.IsEditMode)
            return;
            
        // Optional: snap to screen bounds here if desired
    }
}
