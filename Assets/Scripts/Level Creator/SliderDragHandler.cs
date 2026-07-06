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
                Camera.main.transform.position = new Vector3(targetObject.transform.position.x, targetObject.transform.position.y, Camera.main.transform.position.z);
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

        // Keep ONLY the active slider fully visible
        if (activeSlider != null)
        {
            CanvasGroup sliderCg = activeSlider.gameObject.GetComponent<CanvasGroup>() ?? activeSlider.gameObject.AddComponent<CanvasGroup>();
            sliderCg.ignoreParentGroups = true;
            sliderCg.alpha = 1f;
        }
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

        // Restore active slider to inherit parent opacity again
        if (activeSlider != null)
        {
            CanvasGroup sliderCg = activeSlider.gameObject.GetComponent<CanvasGroup>();
            if (sliderCg != null)
            {
                sliderCg.ignoreParentGroups = false;
                sliderCg.alpha = 1f;
            }
            activeSlider = null;
        }

        // Reinitialize to clamp values
        Initialize(xSlider, ySlider, targetObject, finalPos, parentCanvasGroup);
    }
}
