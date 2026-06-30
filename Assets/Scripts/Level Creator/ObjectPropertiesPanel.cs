using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Side Panel UI controller for modifying properties (scale, rotation, trigger wiring, speed, delay)
/// of the currently selected PlacedEditorObject in the editor.
/// </summary>
public class ObjectPropertiesPanel : MonoBehaviour
{
    public static ObjectPropertiesPanel Instance { get; private set; }

    [Header("UI Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Transform Sliders")]
    [SerializeField] private Slider widthSlider;
    [SerializeField] private Slider heightSlider;
    [SerializeField] private Slider rotationSlider;

    [Header("Mechanics / Trap Settings")]
    [SerializeField] private GameObject mechanicsGroup; // group to hide/show for non-trap assets
    [SerializeField] private TMP_Dropdown directionDropdown;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private Slider delaySlider;

    [Header("Interactive Wiring")]
    [SerializeField] private GameObject triggerWiringGroup; // group to hide/show for trigger assets
    [SerializeField] private Button linkButton;
    [SerializeField] private TMP_Text linkStatusText;

    [Header("Action Buttons")]
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button closeButton;

    // The currently selected object we are editing
    private PlacedEditorObject targetObject;
    private bool isUpdatingUI = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Start hidden
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Start()
    {
        // Wire up UI listeners
        if (widthSlider != null) widthSlider.onValueChanged.AddListener(OnWidthChanged);
        if (heightSlider != null) heightSlider.onValueChanged.AddListener(OnHeightChanged);
        if (rotationSlider != null) rotationSlider.onValueChanged.AddListener(OnRotationChanged);

        if (speedSlider != null) speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        if (delaySlider != null) delaySlider.onValueChanged.AddListener(OnDelayChanged);
        if (directionDropdown != null) directionDropdown.onValueChanged.AddListener(OnDirectionChanged);

        if (linkButton != null) linkButton.onClick.AddListener(OnLinkButtonClicked);
        if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
    }

    /// <summary>
    /// Open the properties UI and populate it with values from the selected object.
    /// </summary>
    public void ShowProperties(PlacedEditorObject obj)
    {
        targetObject = obj;
        if (targetObject == null)
        {
            HideProperties();
            return;
        }

        isUpdatingUI = true;

        if (panelRoot != null) panelRoot.SetActive(true);

        // 1. Set Transform Slider values
        if (widthSlider != null)
        {
            widthSlider.minValue = 0.2f;
            widthSlider.maxValue = 15f;
            widthSlider.value = targetObject.transform.localScale.x;
        }
        if (heightSlider != null)
        {
            heightSlider.minValue = 0.2f;
            heightSlider.maxValue = 15f;
            heightSlider.value = targetObject.transform.localScale.y;
        }
        if (rotationSlider != null)
        {
            rotationSlider.minValue = 0f;
            rotationSlider.maxValue = 360f;
            rotationSlider.value = targetObject.transform.eulerAngles.z;
        }

        // 2. Hide/show mechanics properties depending on the asset type
        bool isTrapOrObstacle = IsTrapOrObstacleType(targetObject.assetTypeName);
        if (mechanicsGroup != null) mechanicsGroup.SetActive(isTrapOrObstacle);

        if (isTrapOrObstacle)
        {
            if (speedSlider != null)
            {
                speedSlider.minValue = 0f;
                speedSlider.maxValue = 15f;
                speedSlider.value = targetObject.speed;
            }
            if (delaySlider != null)
            {
                delaySlider.minValue = 0.1f;
                delaySlider.maxValue = 5f;
                delaySlider.value = targetObject.delay;
            }

            // Sync direction dropdown selection
            if (directionDropdown != null)
            {
                int index = directionDropdown.options.FindIndex(o => o.text.Equals(targetObject.moveDir, System.StringComparison.OrdinalIgnoreCase));
                directionDropdown.value = Mathf.Max(0, index);
            }
        }

        // 3. Hide/show trigger wiring setup
        bool isTrigger = targetObject.assetTypeName == "TriggerZone" || targetObject.assetTypeName == "Goal";
        if (triggerWiringGroup != null) triggerWiringGroup.SetActive(isTrigger);

        UpdateLinkStatusText();

        isUpdatingUI = false;
    }

    public void HideProperties()
    {
        targetObject = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void OpenPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ── Slider Handlers ───────────────────────────────────────────────────────

    private void OnWidthChanged(float val)
    {
        if (isUpdatingUI || targetObject == null) return;
        Vector3 localScale = targetObject.transform.localScale;
        targetObject.transform.localScale = new Vector3(val, localScale.y, localScale.z);
    }

    private void OnHeightChanged(float val)
    {
        if (isUpdatingUI || targetObject == null) return;
        Vector3 localScale = targetObject.transform.localScale;
        targetObject.transform.localScale = new Vector3(localScale.x, val, localScale.z);
    }

    private void OnRotationChanged(float val)
    {
        if (isUpdatingUI || targetObject == null) return;
        targetObject.transform.rotation = Quaternion.Euler(0f, 0f, val);

        // Update visual wires connected to this so they follow the rotation pivot
        if (targetObject.wireLine != null)
        {
            targetObject.wireLine.SetPosition(0, targetObject.transform.position);
        }
    }

    private void OnSpeedChanged(float val)
    {
        if (isUpdatingUI || targetObject == null) return;
        targetObject.speed = val;
    }

    private void OnDelayChanged(float val)
    {
        if (isUpdatingUI || targetObject == null) return;
        targetObject.delay = val;
    }

    private void OnDirectionChanged(int index)
    {
        if (isUpdatingUI || targetObject == null || directionDropdown == null) return;
        targetObject.moveDir = directionDropdown.options[index].text;
    }

    // ── Interaction Actions ──────────────────────────────────────────────────

    private void OnLinkButtonClicked()
    {
        if (targetObject == null) return;

        if (targetObject.hasTarget)
        {
            // If already linked, clicking the button clears it
            if (GridPainter.Instance != null) GridPainter.Instance.RemoveLink(targetObject);
            UpdateLinkStatusText();
        }
        else
        {
            // Enter wiring link mode
            if (GridPainter.Instance != null) GridPainter.Instance.RequestTriggerWiringLink();
            if (linkStatusText != null) linkStatusText.text = "Click target object... (Esc to cancel)";
        }
    }

    private void OnDeleteButtonClicked()
    {
        if (GridPainter.Instance != null)
        {
            GridPainter.Instance.DeleteSelectedObject();
        }
    }

    private void UpdateLinkStatusText()
    {
        if (linkStatusText == null || targetObject == null) return;

        if (targetObject.hasTarget && targetObject.targetObject != null)
        {
            linkStatusText.text = $"Wired to: {targetObject.targetObject.assetTypeName} at ({targetObject.targetObject.transform.position.x:F1}, {targetObject.targetObject.transform.position.y:F1})";
            if (linkButton != null)
            {
                var txt = linkButton.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = "Disconnect";
            }
        }
        else
        {
            linkStatusText.text = "Not wired to any target.";
            if (linkButton != null)
            {
                var txt = linkButton.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = "Link Target";
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private bool IsTrapOrObstacleType(string name)
    {
        return name == "MovingPlatform" || name == "ProjectileSpawner" || name == "TriggerZone" || name == "CameraShake";
    }
}
