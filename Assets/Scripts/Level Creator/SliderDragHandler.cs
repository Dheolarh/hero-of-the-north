using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class SliderDragHandler : MonoBehaviour, IPointerDownHandler
{
    public Action<Slider> OnDragStart;

    private Slider parentSlider;

    private void Awake()
    {
        parentSlider = GetComponentInParent<Slider>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[SliderDragHandler] OnPointerDown on: {gameObject.name}");
        OnDragStart?.Invoke(parentSlider);
    }
}

public class CameraBorrowerSlider : MonoBehaviour
{
    public Action<Vector2> OnPositionSaved;

    public float snapIncrement;

    private Slider xSlider;
    private Slider ySlider;
    private GameObject[] targetObjects;

    private bool isDragging = false;
    private Vector3[] initialObjectPositions;
    private Vector3 initialCameraPosition;
    private CanvasGroup parentCanvasGroup;
    private Slider activeSlider = null;
    private Vector3 targetAnchorPos;
    private bool hasStoredInitialPositions = false;
    private int anchorIndex = -1;

    public void Initialize(Slider xSlider, Slider ySlider, GameObject targetObject, Vector2 savedPosition, CanvasGroup parentGroup = null)
    {
        Initialize(xSlider, ySlider, targetObject != null ? new GameObject[] { targetObject } : null, savedPosition, parentGroup);
    }

    public void Initialize(Slider xSlider, Slider ySlider, GameObject[] targetObjects, Vector2 savedPosition, CanvasGroup parentGroup = null)
    {
        this.xSlider = xSlider;
        this.ySlider = ySlider;
        this.targetObjects = targetObjects;
        this.parentCanvasGroup = parentGroup;

        // Find the first non-null target object as the anchor
        anchorIndex = -1;
        if (targetObjects != null)
        {
            for (int i = 0; i < targetObjects.Length; i++)
            {
                if (targetObjects[i] != null)
                {
                    anchorIndex = i;
                    break;
                }
            }
        }

        // Ensure this panel itself fades with the parent editor group
        CanvasGroup childCg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        childCg.ignoreParentGroups = false;
        childCg.alpha = 1f;

        if (xSlider != null && ySlider != null)
        {
            // Level boundaries matching ClampCameraPosition
            xSlider.minValue = -7f;
            xSlider.maxValue = 50f;
            ySlider.minValue = -25f;
            ySlider.maxValue = 25f;

            if (!isDragging)
            {
                Vector2 startPos = savedPosition;
                if (startPos == Vector2.zero && anchorIndex >= 0)
                {
                    startPos = targetObjects[anchorIndex].transform.position;
                }
                xSlider.value = Mathf.Clamp(startPos.x, xSlider.minValue, xSlider.maxValue);
                ySlider.value = Mathf.Clamp(startPos.y, ySlider.minValue, ySlider.maxValue);
                
                if (anchorIndex >= 0)
                {
                    targetAnchorPos = targetObjects[anchorIndex].transform.position;
                }
            }

            xSlider.onValueChanged.RemoveAllListeners();
            xSlider.onValueChanged.AddListener((val) =>
            {
                if (isDragging && anchorIndex >= 0)
                {
                    float snap = GetSnapIncrement();
                    float snappedVal = Mathf.Round(val / snap) * snap;
                    targetAnchorPos.x = snappedVal;
                }
            });

            ySlider.onValueChanged.RemoveAllListeners();
            ySlider.onValueChanged.AddListener((val) =>
            {
                if (isDragging && anchorIndex >= 0)
                {
                    float snap = GetSnapIncrement();
                    float snappedVal = Mathf.Round(val / snap) * snap;
                    targetAnchorPos.y = snappedVal;
                }
            });

            if (anchorIndex >= 0)
            {
                var dragX = xSlider.gameObject.GetComponent<SliderDragHandler>() ?? xSlider.gameObject.AddComponent<SliderDragHandler>();
                dragX.OnDragStart = HandleDragStart;

                if (xSlider.handleRect != null)
                {
                    var handleDragX = xSlider.handleRect.gameObject.GetComponent<SliderDragHandler>() ?? xSlider.handleRect.gameObject.AddComponent<SliderDragHandler>();
                    handleDragX.OnDragStart = HandleDragStart;
                }

                var dragY = ySlider.gameObject.GetComponent<SliderDragHandler>() ?? ySlider.gameObject.AddComponent<SliderDragHandler>();
                dragY.OnDragStart = HandleDragStart;

                if (ySlider.handleRect != null)
                {
                    var handleDragY = ySlider.handleRect.gameObject.GetComponent<SliderDragHandler>() ?? ySlider.handleRect.gameObject.AddComponent<SliderDragHandler>();
                    handleDragY.OnDragStart = HandleDragStart;
                }
            }
        }
    }

    private void HandleDragStart(Slider slider)
    {
        activeSlider = slider;
        OnSliderDragStart();
    }

    private void Update()
    {
        if (isDragging)
        {
            // Bulletproof: If mouse button is released, stop dragging immediately
            if (!Input.GetMouseButton(0))
            {
                OnSliderDragEnd();
                return;
            }

            if (anchorIndex >= 0 && targetObjects != null && targetObjects[anchorIndex] != null && Camera.main != null)
            {
                GameObject anchorObj = targetObjects[anchorIndex];
                Vector3 oldAnchorPos = anchorObj.transform.position;
                float speed = 8f; // Speed factor (smoothly glides the target)

                // 1. Smoothly place the anchor object toward the target coordinate slider value
                Vector3 newAnchorPos = Vector3.Lerp(oldAnchorPos, new Vector3(targetAnchorPos.x, targetAnchorPos.y, oldAnchorPos.z), Time.deltaTime * speed);
                anchorObj.transform.position = newAnchorPos;

                // 2. Glide all other target objects maintaining their relative distances
                if (initialObjectPositions != null)
                {
                    Vector3 anchorDelta = newAnchorPos - initialObjectPositions[anchorIndex];
                    for (int i = 0; i < targetObjects.Length; i++)
                    {
                        if (i == anchorIndex || targetObjects[i] == null || i >= initialObjectPositions.Length) continue;

                        Vector3 currentObjPos = targetObjects[i].transform.position;
                        Vector3 desiredObjPos = new Vector3(
                            initialObjectPositions[i].x + anchorDelta.x,
                            initialObjectPositions[i].y + anchorDelta.y,
                            currentObjPos.z);

                        // Lerp each companion object at the same speed as the anchor
                        targetObjects[i].transform.position = Vector3.Lerp(currentObjPos, desiredObjPos, Time.deltaTime * speed);
                    }
                }

                // 3. Smoothly follow with camera, applying clamping bounds
                Camera cam = Camera.main;
                float halfHeight = cam.orthographicSize;
                float halfWidth = halfHeight * cam.aspect;

                float minXBound = -7f;
                float maxXBound = 50f;
                float minYBound = -25f;
                float maxYBound = 25f;

                // Focus on the smoothed anchor position
                Vector3 desiredCamPos = new Vector3(anchorObj.transform.position.x, anchorObj.transform.position.y, cam.transform.position.z);

                // Clamp camera target position to level bounds
                if ((maxXBound - minXBound) > (2f * halfWidth))
                {
                    desiredCamPos.x = Mathf.Clamp(desiredCamPos.x, minXBound + halfWidth, maxXBound - halfWidth);
                }
                else
                {
                    desiredCamPos.x = (minXBound + maxXBound) * 0.5f;
                }

                if ((maxYBound - minYBound) > (2f * halfHeight))
                {
                    desiredCamPos.y = Mathf.Clamp(desiredCamPos.y, minYBound + halfHeight, maxYBound - halfHeight);
                }
                else
                {
                    desiredCamPos.y = (minYBound + maxYBound) * 0.5f;
                }

                // Smoothly interpolate the camera position
                cam.transform.position = Vector3.Lerp(cam.transform.position, desiredCamPos, Time.deltaTime * speed);
            }
        }
    }

    private void OnSliderDragStart()
    {
        if (anchorIndex < 0 || targetObjects == null || targetObjects[anchorIndex] == null) return;
        isDragging = true;

        if (!hasStoredInitialPositions)
        {
            initialObjectPositions = new Vector3[targetObjects.Length];
            for (int i = 0; i < targetObjects.Length; i++)
            {
                if (targetObjects[i] != null)
                {
                    initialObjectPositions[i] = targetObjects[i].transform.position;
                }
            }

            if (Camera.main != null)
            {
                initialCameraPosition = Camera.main.transform.position;
            }
            hasStoredInitialPositions = true;
        }

        targetAnchorPos = targetObjects[anchorIndex].transform.position;
        
        if (parentCanvasGroup != null)
        {
            parentCanvasGroup.alpha = 0.05f; // Dim overall editor panel
        }

        // Hide all extra elements except the active slider (opacity-based to preserve layout)
        SetAllExceptActiveSlider(false);
    }

    private float GetSnapIncrement()
    {
        if (LevelCreatorUI.Instance != null)
        {
            return LevelCreatorUI.Instance.dragSnapIncrement;
        }
        return snapIncrement > 0f ? snapIncrement : 0.05f;
    }

    private void OnSliderDragEnd()
    {
        if (anchorIndex < 0 || targetObjects == null || targetObjects[anchorIndex] == null) return;
        isDragging = false;

        float snap = GetSnapIncrement();
        float finalX = Mathf.Round(targetObjects[anchorIndex].transform.position.x / snap) * snap;
        float finalY = Mathf.Round(targetObjects[anchorIndex].transform.position.y / snap) * snap;
        Vector2 finalPos = new Vector2(finalX, finalY);

        // Snap anchor precisely to grid
        Vector3 anchorOldPos = targetObjects[anchorIndex].transform.position;
        targetObjects[anchorIndex].transform.position = new Vector3(finalX, finalY, anchorOldPos.z);

        // Snap all companion objects precisely too, preserving their relative offset
        if (initialObjectPositions != null)
        {
            Vector3 anchorDelta = new Vector3(finalX, finalY, 0f) - new Vector3(initialObjectPositions[anchorIndex].x, initialObjectPositions[anchorIndex].y, 0f);
            for (int i = 0; i < targetObjects.Length; i++)
            {
                if (i == anchorIndex || targetObjects[i] == null || i >= initialObjectPositions.Length) continue;
                Vector3 companionPos = targetObjects[i].transform.position;
                targetObjects[i].transform.position = new Vector3(
                    initialObjectPositions[i].x + anchorDelta.x,
                    initialObjectPositions[i].y + anchorDelta.y,
                    companionPos.z);
            }
        }

        // Notify saved position
        OnPositionSaved?.Invoke(finalPos);

        if (parentCanvasGroup != null)
        {
            parentCanvasGroup.alpha = 1f;
        }

        // Restore all extra elements back to normal visibility
        SetAllExceptActiveSlider(true);

        activeSlider = null;

        // Reinitialize to clamp values
        Initialize(xSlider, ySlider, targetObjects, finalPos, parentCanvasGroup);
    }


    public void ResetToInitialState(bool restoreObjectPositions = true)
    {
        Debug.Log($"[CameraBorrowerSlider] ResetToInitialState called. hasStoredInitialPositions={hasStoredInitialPositions}, restoreObjectPositions={restoreObjectPositions}, targetObjectsCount={(targetObjects != null ? targetObjects.Length : 0)}");
        if (hasStoredInitialPositions)
        {
            if (restoreObjectPositions && targetObjects != null && initialObjectPositions != null)
            {
                for (int i = 0; i < targetObjects.Length; i++)
                {
                    if (targetObjects[i] != null && i < initialObjectPositions.Length)
                    {
                        Debug.Log($"[CameraBorrowerSlider] Restoring position of {targetObjects[i].name} to {initialObjectPositions[i]}");
                        targetObjects[i].transform.position = initialObjectPositions[i];
                    }
                }
            }

            hasStoredInitialPositions = false;
        }

        // Always snap camera back to player start in the editor when closing/resetting
        if (GridPainter.Instance != null)
        {
            Debug.Log("[CameraBorrowerSlider] Snapping camera back to player start.");
            GridPainter.Instance.SnapCameraToPlayerStart();
        }
        else if (Camera.main != null && initialCameraPosition != Vector3.zero)
        {
            Debug.Log($"[CameraBorrowerSlider] Restoring camera to {initialCameraPosition}");
            Camera.main.transform.position = initialCameraPosition;
        }
    }

    private void SetAllExceptActiveSlider(bool active)
    {
        float targetAlpha = active ? 1f : 0f;

        foreach (Transform child in transform)
        {
            if (child.name == "Header")
            {
                SetCanvasGroupAlpha(child.gameObject, targetAlpha);
            }
            else
            {
                // This is a sub-panel group (like Teleport or SingleMotion)
                // Search inside it to see if it holds the active slider
                bool containsActive = false;
                if (activeSlider != null)
                {
                    Slider[] sliders = child.GetComponentsInChildren<Slider>(true);
                    foreach (var s in sliders)
                    {
                        if (s == activeSlider)
                        {
                            containsActive = true;
                            break;
                        }
                    }
                }

                if (!containsActive)
                {
                    // Hide entire group if active slider is not here
                    SetCanvasGroupAlpha(child.gameObject, targetAlpha);
                }
                else
                {
                    // Keep this group visible, but hide non-active elements inside it
                    foreach (Transform subChild in child)
                    {
                        bool isSubChildActive = false;
                        if (activeSlider != null)
                        {
                            Slider s = subChild.GetComponentInChildren<Slider>(true);
                            if (s != null && s == activeSlider)
                            {
                                isSubChildActive = true;
                            }
                        }

                        if (!isSubChildActive)
                        {
                            SetCanvasGroupAlpha(subChild.gameObject, targetAlpha);
                        }
                        else
                        {
                            // Keep active slider's parent container visible and ignore parent groups
                            var cg = subChild.gameObject.GetComponent<CanvasGroup>() ?? subChild.gameObject.AddComponent<CanvasGroup>();
                            cg.ignoreParentGroups = !active;
                            cg.alpha = 1f;

                            // Hide secondary labels inside the active slider container to clear space
                            Transform label = subChild.Find("Label");
                            if (label != null)
                            {
                                SetCanvasGroupAlpha(label.gameObject, targetAlpha);
                            }
                        }
                    }
                }
            }
        }
    }

    private void SetCanvasGroupAlpha(GameObject go, float alpha)
    {
        var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        cg.alpha = alpha;
        cg.blocksRaycasts = (alpha > 0.01f);
        cg.interactable = (alpha > 0.01f);
        cg.ignoreParentGroups = false;
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    public void CleanUp()
    {
        if (xSlider != null)
        {
            xSlider.onValueChanged.RemoveAllListeners();
            var dragX = xSlider.gameObject.GetComponent<SliderDragHandler>();
            if (dragX != null) Destroy(dragX);
            if (xSlider.handleRect != null)
            {
                var handleDragX = xSlider.handleRect.gameObject.GetComponent<SliderDragHandler>();
                if (handleDragX != null) Destroy(handleDragX);
            }
        }
        if (ySlider != null)
        {
            ySlider.onValueChanged.RemoveAllListeners();
            var dragY = ySlider.gameObject.GetComponent<SliderDragHandler>();
            if (dragY != null) Destroy(dragY);
            if (ySlider.handleRect != null)
            {
                var handleDragY = ySlider.handleRect.gameObject.GetComponent<SliderDragHandler>();
                if (handleDragY != null) Destroy(handleDragY);
            }
        }
        isDragging = false;
        hasStoredInitialPositions = false;
    }
}
