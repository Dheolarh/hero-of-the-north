using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectTransformPanelController : MonoBehaviour
{
    public static ObjectTransformPanelController Instance { get; private set; }

    [Header("UI References - Object Transform")]
    [SerializeField] private TMP_Text selectedObjectNameText;
    [SerializeField] private Slider scaleXSlider;
    [SerializeField] private Slider scaleYSlider;
    [SerializeField] private Slider rotationSlider;
    [SerializeField] private GameObject objectTransformGroup;

    [Header("UI References - Player Settings")]
    [SerializeField] private Slider moveSpeedSlider;
    [SerializeField] private Slider jumpHeightSlider;
    [SerializeField] private TMP_Text maxJumpsValueText;
    [SerializeField] private Button maxJumpsMinusButton;
    [SerializeField] private Button maxJumpsPlusButton;
    [SerializeField] private Toggle fallDamageToggle;

    private PlacedEditorObject lastSelectedObject = null;
    private CanvasGroup canvasGroup;
    private bool isDragging = false;
    private Slider activeSlider = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        AutoLocateReferences();
    }

    private void Start()
    {
        // ── Object Transform Listeners ──────────────────────────────────────────
        if (scaleXSlider != null)
        {
            scaleXSlider.minValue = 0.1f;
            scaleXSlider.maxValue = 5f;
            scaleXSlider.onValueChanged.RemoveAllListeners();
            scaleXSlider.onValueChanged.AddListener(OnScaleXChanged);
            SetupSliderDragListeners(scaleXSlider);
        }

        if (scaleYSlider != null)
        {
            scaleYSlider.minValue = 0.1f;
            scaleYSlider.maxValue = 5f;
            scaleYSlider.onValueChanged.RemoveAllListeners();
            scaleYSlider.onValueChanged.AddListener(OnScaleYChanged);
            SetupSliderDragListeners(scaleYSlider);
        }

        if (rotationSlider != null)
        {
            rotationSlider.minValue = 0f;
            rotationSlider.maxValue = 360f;
            rotationSlider.onValueChanged.RemoveAllListeners();
            rotationSlider.onValueChanged.AddListener(OnRotationChanged);
            SetupSliderDragListeners(rotationSlider);
        }

        // ── Player Settings Listeners ─────────────────────────────────────────
        if (moveSpeedSlider != null)
        {
            moveSpeedSlider.minValue = 3f;
            moveSpeedSlider.maxValue = 10f;
            moveSpeedSlider.value = LevelCreatorUI.Instance != null ? LevelCreatorUI.Instance.playerMoveSpeed : 5f;
            moveSpeedSlider.onValueChanged.RemoveAllListeners();
            moveSpeedSlider.onValueChanged.AddListener(OnMoveSpeedChanged);
            SetupSliderDragListeners(moveSpeedSlider);
        }

        if (jumpHeightSlider != null)
        {
            jumpHeightSlider.minValue = 5f;
            jumpHeightSlider.maxValue = 15f;
            jumpHeightSlider.value = LevelCreatorUI.Instance != null ? LevelCreatorUI.Instance.playerJumpForce : 7f;
            jumpHeightSlider.onValueChanged.RemoveAllListeners();
            jumpHeightSlider.onValueChanged.AddListener(OnJumpForceChanged);
            SetupSliderDragListeners(jumpHeightSlider);
        }

        if (maxJumpsMinusButton != null)
        {
            maxJumpsMinusButton.onClick.RemoveAllListeners();
            maxJumpsMinusButton.onClick.AddListener(DecrementMaxJumps);
        }

        if (maxJumpsPlusButton != null)
        {
            maxJumpsPlusButton.onClick.RemoveAllListeners();
            maxJumpsPlusButton.onClick.AddListener(IncrementMaxJumps);
        }

        if (fallDamageToggle != null)
        {
            fallDamageToggle.isOn = LevelCreatorUI.Instance != null && LevelCreatorUI.Instance.playerEnableFallDamage;
            fallDamageToggle.onValueChanged.RemoveAllListeners();
            fallDamageToggle.onValueChanged.AddListener(OnFallDamageToggled);
        }

        UpdatePlayerSettingsUI();
        OnSelectionChanged(null);
    }

    private void Update()
    {
        if (isDragging)
        {
            // If the user releases the mouse button, stop fading the panel
            if (!Input.GetMouseButton(0))
            {
                HandleSliderDragEnd();
            }
        }

        if (GridPainter.Instance == null) return;

        PlacedEditorObject currentSelection = GridPainter.Instance.GetSelectedObject();

        if (currentSelection != lastSelectedObject)
        {
            lastSelectedObject = currentSelection;
            OnSelectionChanged(currentSelection);
        }
    }

    // ── Update Panels based on selection ──────────────────────────────────
    private void OnSelectionChanged(PlacedEditorObject selected)
    {
        if (selected != null)
        {
            if (objectTransformGroup != null) objectTransformGroup.SetActive(true);

            if (selectedObjectNameText != null)
            {
                selectedObjectNameText.text = "Selected: " + (string.IsNullOrEmpty(selected.customToolDisplayName) ? selected.assetTypeName : selected.customToolDisplayName);
            }

            // Sync Sliders with the selected object's transform properties
            if (scaleXSlider != null)
            {
                scaleXSlider.onValueChanged.RemoveAllListeners();
                scaleXSlider.value = selected.transform.localScale.x;
                scaleXSlider.onValueChanged.AddListener(OnScaleXChanged);
            }

            if (scaleYSlider != null)
            {
                scaleYSlider.onValueChanged.RemoveAllListeners();
                scaleYSlider.value = selected.transform.localScale.y;
                scaleYSlider.onValueChanged.AddListener(OnScaleYChanged);
            }

            if (rotationSlider != null)
            {
                rotationSlider.onValueChanged.RemoveAllListeners();
                rotationSlider.value = selected.transform.localEulerAngles.z;
                rotationSlider.onValueChanged.AddListener(OnRotationChanged);
            }
        }
        else
        {
            if (objectTransformGroup != null) objectTransformGroup.SetActive(false);
            if (selectedObjectNameText != null) selectedObjectNameText.text = "No Object Selected";
        }
    }

    // ── Object Transform Callback Handlers ────────────────────────────────
    private void OnScaleXChanged(float val)
    {
        if (lastSelectedObject != null)
        {
            Vector3 localScale = lastSelectedObject.transform.localScale;
            localScale.x = val;
            lastSelectedObject.transform.localScale = localScale;
        }
    }

    private void OnScaleYChanged(float val)
    {
        if (lastSelectedObject != null)
        {
            Vector3 localScale = lastSelectedObject.transform.localScale;
            localScale.y = val;
            lastSelectedObject.transform.localScale = localScale;
        }
    }

    private void OnRotationChanged(float val)
    {
        if (lastSelectedObject != null)
        {
            lastSelectedObject.transform.localEulerAngles = new Vector3(0f, 0f, val);
        }
    }

    // ── Player Settings Callback Handlers ────────────────────────────────
    private void OnMoveSpeedChanged(float val)
    {
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.playerMoveSpeed = val;
        }
    }

    private void OnJumpForceChanged(float val)
    {
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.playerJumpForce = val;
        }
    }

    private void DecrementMaxJumps()
    {
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.playerMaxJumps = Mathf.Max(0, LevelCreatorUI.Instance.playerMaxJumps - 1);
            UpdatePlayerSettingsUI();
        }
    }

    private void IncrementMaxJumps()
    {
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.playerMaxJumps = Mathf.Min(5, LevelCreatorUI.Instance.playerMaxJumps + 1);
            UpdatePlayerSettingsUI();
        }
    }

    private void OnFallDamageToggled(bool isON)
    {
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.playerEnableFallDamage = isON;
        }
    }

    public void UpdatePlayerSettingsUI()
    {
        if (LevelCreatorUI.Instance == null) return;

        if (moveSpeedSlider != null)
        {
            moveSpeedSlider.onValueChanged.RemoveAllListeners();
            moveSpeedSlider.value = LevelCreatorUI.Instance.playerMoveSpeed;
            moveSpeedSlider.onValueChanged.AddListener(OnMoveSpeedChanged);
        }

        if (jumpHeightSlider != null)
        {
            jumpHeightSlider.onValueChanged.RemoveAllListeners();
            jumpHeightSlider.value = LevelCreatorUI.Instance.playerJumpForce;
            jumpHeightSlider.onValueChanged.AddListener(OnJumpForceChanged);
        }

        if (maxJumpsValueText != null)
        {
            maxJumpsValueText.text = LevelCreatorUI.Instance.playerMaxJumps.ToString();
        }

        if (fallDamageToggle != null)
        {
            fallDamageToggle.onValueChanged.RemoveAllListeners();
            fallDamageToggle.isOn = LevelCreatorUI.Instance.playerEnableFallDamage;
            fallDamageToggle.onValueChanged.AddListener(OnFallDamageToggled);
        }
    }

    // ── Slider Drag Handling for Fading Panel ────────────────────────────
    private void SetupSliderDragListeners(Slider slider)
    {
        if (slider == null) return;

        var drag = slider.gameObject.GetComponent<SliderDragHandler>() ?? slider.gameObject.AddComponent<SliderDragHandler>();
        drag.OnDragStart = HandleSliderDragStart;

        if (slider.handleRect != null)
        {
            var handleDrag = slider.handleRect.gameObject.GetComponent<SliderDragHandler>() ?? slider.handleRect.gameObject.AddComponent<SliderDragHandler>();
            handleDrag.OnDragStart = HandleSliderDragStart;
        }
    }

    private void HandleSliderDragStart(Slider slider)
    {
        activeSlider = slider;
        isDragging = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.15f; // Fade panel
        }

        if (activeSlider != null)
        {
            var cg = activeSlider.gameObject.GetComponent<CanvasGroup>() ?? activeSlider.gameObject.AddComponent<CanvasGroup>();
            cg.ignoreParentGroups = true;
            cg.alpha = 1f; // Keep active slider visible
        }
    }

    private void HandleSliderDragEnd()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f; // Restore panel opacity
        }

        if (activeSlider != null)
        {
            var cg = activeSlider.gameObject.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.ignoreParentGroups = false;
            }
        }

        activeSlider = null;
        isDragging = false;
    }

    private void AutoLocateReferences()
    {
        if (selectedObjectNameText == null)
        {
            selectedObjectNameText = transform.Find("Header/Title")?.GetComponent<TMP_Text>() ?? 
                                     transform.Find("Title")?.GetComponent<TMP_Text>() ??
                                     GetComponentInChildren<TMP_Text>();
        }

        if (objectTransformGroup == null)
        {
            var t = transform.Find("Object Transform") ?? transform.Find("ObjectTransform");
            if (t != null) objectTransformGroup = t.gameObject;
        }

        Slider[] allSliders = GetComponentsInChildren<Slider>(true);
        foreach (var s in allSliders)
        {
            string name = s.gameObject.name.ToLower();
            Transform parent = s.transform.parent;
            string parentName = parent != null ? parent.name.ToLower() : "";

            if (name.Contains("scale x") || (parentName.Contains("scale") && name.Contains("x")))
            {
                if (scaleXSlider == null) scaleXSlider = s;
            }
            else if (name.Contains("scale y") || (parentName.Contains("scale") && name.Contains("y")))
            {
                if (scaleYSlider == null) scaleYSlider = s;
            }
            else if (name.Contains("rotate") || name.Contains("rotation"))
            {
                if (rotationSlider == null) rotationSlider = s;
            }
            else if (name.Contains("speed") || name.Contains("move"))
            {
                if (moveSpeedSlider == null) moveSpeedSlider = s;
            }
            else if (name.Contains("jump height") || name.Contains("jumpforce") || name.Contains("jump height slider") || (parentName.Contains("jump height") && s.name.Contains("Slider")))
            {
                if (jumpHeightSlider == null) jumpHeightSlider = s;
            }
        }

        if (maxJumpsMinusButton == null || maxJumpsPlusButton == null || maxJumpsValueText == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string name = b.gameObject.name.ToLower();
                if (name.Contains("minus") || b.GetComponentInChildren<TMP_Text>()?.text == "-")
                {
                    if (maxJumpsMinusButton == null) maxJumpsMinusButton = b;
                }
                else if (name.Contains("plus") || b.GetComponentInChildren<TMP_Text>()?.text == "+")
                {
                    if (maxJumpsPlusButton == null) maxJumpsPlusButton = b;
                }
            }

            if (maxJumpsMinusButton != null)
            {
                Transform parent = maxJumpsMinusButton.transform.parent;
                if (parent != null)
                {
                    TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
                    foreach (var t in texts)
                    {
                        if (t.text != "-" && t.text != "+")
                        {
                            maxJumpsValueText = t;
                            break;
                        }
                    }
                }
            }
        }

        if (fallDamageToggle == null)
        {
            fallDamageToggle = GetComponentInChildren<Toggle>(true);
        }
    }
}
