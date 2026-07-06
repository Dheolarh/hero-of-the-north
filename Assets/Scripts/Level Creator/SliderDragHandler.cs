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

    private Slider xSlider;
    private Slider ySlider;
    private GameObject targetObject;

    private bool isDragging = false;
    private Vector3 initialObjectPosition;
    private Vector3 initialCameraPosition;
    private CanvasGroup parentCanvasGroup;
    private Slider activeSlider = null;

    public void Initialize(Slider xSlider, Slider ySlider, GameObject targetObject, Vector2 savedPosition, CanvasGroup parentGroup = null)
    {
        this.xSlider = xSlider;
        this.ySlider = ySlider;
        this.targetObject = targetObject;
        this.parentCanvasGroup = parentGroup;

        // Ensure this Teleport panel itself fades with the parent editor group
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
                if (startPos == Vector2.zero && targetObject != null)
                {
                    startPos = targetObject.transform.position;
                }
                xSlider.value = Mathf.Clamp(startPos.x, xSlider.minValue, xSlider.maxValue);
                ySlider.value = Mathf.Clamp(startPos.y, ySlider.minValue, ySlider.maxValue);
            }

            xSlider.onValueChanged.RemoveAllListeners();
            xSlider.onValueChanged.AddListener((val) =>
            {
                if (isDragging && targetObject != null)
                {
                    targetObject.transform.position = new Vector3(val, ySlider.value, targetObject.transform.position.z);
                }
            });

            ySlider.onValueChanged.RemoveAllListeners();
            ySlider.onValueChanged.AddListener((val) =>
            {
                if (isDragging && targetObject != null)
                {
                    targetObject.transform.position = new Vector3(xSlider.value, val, targetObject.transform.position.z);
                }
            });

            if (targetObject != null)
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

            if (targetObject != null && Camera.main != null)
            {
                Camera cam = Camera.main;
                float halfHeight = cam.orthographicSize;
                float halfWidth = halfHeight * cam.aspect;

                float minXBound = -7f;
                float maxXBound = 50f;
                float minYBound = -25f;
                float maxYBound = 25f;

                Vector3 targetCamPos = new Vector3(targetObject.transform.position.x, targetObject.transform.position.y, cam.transform.position.z);

                // Clamp camera position to level bounds
                if ((maxXBound - minXBound) > (2f * halfWidth))
                {
                    targetCamPos.x = Mathf.Clamp(targetCamPos.x, minXBound + halfWidth, maxXBound - halfWidth);
                }
                else
                {
                    targetCamPos.x = (minXBound + maxXBound) * 0.5f;
                }

                if ((maxYBound - minYBound) > (2f * halfHeight))
                {
                    targetCamPos.y = Mathf.Clamp(targetCamPos.y, minYBound + halfHeight, maxYBound - halfHeight);
                }
                else
                {
                    targetCamPos.y = (minYBound + maxYBound) * 0.5f;
                }

                cam.transform.position = targetCamPos;
            }
        }
    }

    private void OnSliderDragStart()
    {
        if (targetObject == null) return;
        isDragging = true;
        initialObjectPosition = targetObject.transform.position;
        if (Camera.main != null)
        {
            initialCameraPosition = Camera.main.transform.position;
        }

        if (parentCanvasGroup != null)
        {
            parentCanvasGroup.alpha = 0.05f; // Dim overall editor panel
        }

        // Hide all extra elements except the active slider (opacity-based to preserve layout)
        SetAllExceptActiveSlider(false);
    }

    private void OnSliderDragEnd()
    {
        if (targetObject == null) return;
        isDragging = false;

        Vector2 finalPos = targetObject.transform.position;

        // Notify saved position
        OnPositionSaved?.Invoke(finalPos);

        // Snap target object back to its initial position
        targetObject.transform.position = initialObjectPosition;

        // Return camera to player
        if (Camera.main != null)
        {
            Camera.main.transform.position = initialCameraPosition;
        }

        if (parentCanvasGroup != null)
        {
            parentCanvasGroup.alpha = 1f;
        }

        // Restore all extra elements back to normal visibility
        SetAllExceptActiveSlider(true);

        activeSlider = null;

        // Reinitialize to clamp values
        Initialize(xSlider, ySlider, targetObject, finalPos, parentCanvasGroup);
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
            else if (child.name == "Teleport") // This is the child panel
            {
                foreach (Transform subChild in child)
                {
                    if (subChild.name == "Object to teleport - scroll" || 
                        subChild.name == "Coordinate Label")
                    {
                        SetCanvasGroupAlpha(subChild.gameObject, targetAlpha);
                    }
                    else if (subChild.name == "X Coordinate" || subChild.name == "Y coordinate")
                    {
                        Slider sliderInChild = subChild.GetComponentInChildren<Slider>();
                        if (sliderInChild != null)
                        {
                            if (sliderInChild == activeSlider)
                            {
                                // Active coordinate panel: Hide its text label but keep the panel and slider visible
                                Transform label = subChild.Find("Label");
                                if (label != null)
                                {
                                    SetCanvasGroupAlpha(label.gameObject, targetAlpha);
                                }

                                // Make sure this slider's panel itself ignores parent group fading and stays visible
                                var cg = subChild.gameObject.GetComponent<CanvasGroup>() ?? subChild.gameObject.AddComponent<CanvasGroup>();
                                cg.ignoreParentGroups = !active;
                                cg.alpha = 1f;
                            }
                            else
                            {
                                // Inactive slider panel: Hide it completely
                                SetCanvasGroupAlpha(subChild.gameObject, targetAlpha);
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
}
