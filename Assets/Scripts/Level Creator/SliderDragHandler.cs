using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class SliderDragHandler : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Action OnDragStart;
    public Action OnDragEnd;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[SliderDragHandler] OnPointerDown on GameObject: {gameObject.name}");
        OnDragStart?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[SliderDragHandler] OnBeginDrag on GameObject: {gameObject.name}");
        OnDragStart?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Must implement IDragHandler for drag events to work, but empty since Slider handles value updates internally.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[SliderDragHandler] OnEndDrag on GameObject: {gameObject.name}");
        OnDragEnd?.Invoke();
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

    public void Initialize(Slider xSlider, Slider ySlider, GameObject targetObject, Vector2 savedPosition, CanvasGroup parentGroup = null)
    {
        this.xSlider = xSlider;
        this.ySlider = ySlider;
        this.targetObject = targetObject;
        this.parentCanvasGroup = parentGroup;

        Camera cam = Camera.main;
        if (cam != null && xSlider != null && ySlider != null)
        {
            float orthoSize = cam.orthographicSize;
            float aspect = cam.aspect;
            float camX = cam.transform.position.x;
            float camY = cam.transform.position.y;

            xSlider.minValue = camX - orthoSize * aspect;
            xSlider.maxValue = camX + orthoSize * aspect;
            ySlider.minValue = camY - orthoSize;
            ySlider.maxValue = camY + orthoSize;
        }

        if (xSlider != null && ySlider != null)
        {
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
                dragX.OnDragStart = OnSliderDragStart;
                dragX.OnDragEnd = OnSliderDragEnd;

                if (xSlider.handleRect != null)
                {
                    var handleDragX = xSlider.handleRect.gameObject.GetComponent<SliderDragHandler>() ?? xSlider.handleRect.gameObject.AddComponent<SliderDragHandler>();
                    handleDragX.OnDragStart = OnSliderDragStart;
                    handleDragX.OnDragEnd = OnSliderDragEnd;
                }

                var dragY = ySlider.gameObject.GetComponent<SliderDragHandler>() ?? ySlider.gameObject.AddComponent<SliderDragHandler>();
                dragY.OnDragStart = OnSliderDragStart;
                dragY.OnDragEnd = OnSliderDragEnd;

                if (ySlider.handleRect != null)
                {
                    var handleDragY = ySlider.handleRect.gameObject.GetComponent<SliderDragHandler>() ?? ySlider.handleRect.gameObject.AddComponent<SliderDragHandler>();
                    handleDragY.OnDragStart = OnSliderDragStart;
                    handleDragY.OnDragEnd = OnSliderDragEnd;
                }
            }
        }
    }

    private void Update()
    {
        if (isDragging && targetObject != null && Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(targetObject.transform.position.x, targetObject.transform.position.y, Camera.main.transform.position.z);
        }
    }

    private void OnSliderDragStart()
    {
        Debug.Log($"[CameraBorrowerSlider] OnSliderDragStart! targetObject: {(targetObject != null ? targetObject.name : "null")}");
        if (targetObject == null) return;
        isDragging = true;
        initialObjectPosition = targetObject.transform.position;
        if (Camera.main != null)
        {
            initialCameraPosition = Camera.main.transform.position;
        }

        if (parentCanvasGroup != null)
        {
            parentCanvasGroup.alpha = 0.05f;
        }

        // Keep this control panel fully visible if CanvasGroup is present
        CanvasGroup childCg = GetComponent<CanvasGroup>();
        if (childCg != null)
        {
            childCg.ignoreParentGroups = true;
        }
    }

    private void OnSliderDragEnd()
    {
        Debug.Log($"[CameraBorrowerSlider] OnSliderDragEnd! targetObject: {(targetObject != null ? targetObject.name : "null")}");
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

        // Reinitialize to clamp values
        Initialize(xSlider, ySlider, targetObject, finalPos, parentCanvasGroup);
    }
}
