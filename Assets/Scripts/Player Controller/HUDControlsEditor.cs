using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages the state of the HUD Controls Edit Mode, including saving and cancelling.
/// </summary>
public class HUDControlsEditor : MonoBehaviour
{
    public static HUDControlsEditor Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The panel containing the Save and Cancel buttons.")]
    public GameObject editModeUIPanel;
    
    [Tooltip("The Image component of the panel containing your movement buttons. Used to tint it slightly during edit mode.")]
    public Image controlsPanelImage;

    [Header("Colors")]
    public Color normalPanelColor = new Color(0, 0, 0, 0f);
    public Color editModePanelColor = new Color(0, 0, 0, 0.4f);

    public bool IsEditMode { get; private set; } = false;

    private List<DraggableUIButton> draggableButtons = new List<DraggableUIButton>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Find all draggable buttons dynamically in children (or globally if attached high up)
        var foundButtons = FindObjectsByType<DraggableUIButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        draggableButtons.AddRange(foundButtons);

        // Load their saved positions immediately
        foreach (var btn in draggableButtons)
        {
            btn.LoadSavedPosition();
        }

        // Ensure Edit UI is hidden initially
        if (editModeUIPanel != null)
        {
            editModeUIPanel.SetActive(false);
        }

        if (controlsPanelImage != null)
        {
            controlsPanelImage.color = normalPanelColor;
        }

        // Resolve cross-scene reference issues by binding events programmatically at runtime
        BindEditControlsButton();
        BindSaveCancelButtons();
    }

    private void BindEditControlsButton()
    {
        if (UIManager.Instance == null || UIManager.Instance.pauseMenu == null)
        {
            Debug.LogWarning("[HUDControlsEditor] UIManager or PauseMenu not found. Cannot bind Edit Controls button.");
            return;
        }

        // Search for a button named "EditControls" or "Edit Controls" inside the PauseMenu (including inactive)
        Button[] buttons = UIManager.Instance.pauseMenu.GetComponentsInChildren<Button>(true);
        Button editBtn = null;
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == "EditControls" || btn.gameObject.name == "Edit Controls")
            {
                editBtn = btn;
                break;
            }
        }

        if (editBtn != null)
        {
            editBtn.onClick.RemoveListener(EnterEditMode);
            editBtn.onClick.AddListener(EnterEditMode);
            Debug.Log("[HUDControlsEditor] Programmatically bound Edit Controls button click event.");
        }
        else
        {
            Debug.LogWarning("[HUDControlsEditor] Could not find a button named 'EditControls' or 'Edit Controls' inside the Pause Menu.");
        }
    }

    private void BindSaveCancelButtons()
    {
        if (editModeUIPanel == null) return;

        // Search for buttons named "Save"/"SaveButton" or "Cancel"/"CancelButton" inside the EditModeUI panel
        Button[] buttons = editModeUIPanel.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == "Save" || btn.gameObject.name == "SaveButton")
            {
                btn.onClick.RemoveListener(SaveEditMode);
                btn.onClick.AddListener(SaveEditMode);
                Debug.Log("[HUDControlsEditor] Programmatically bound Save button click event.");
            }
            else if (btn.gameObject.name == "Cancel" || btn.gameObject.name == "CancelButton")
            {
                btn.onClick.RemoveListener(CancelEditMode);
                btn.onClick.AddListener(CancelEditMode);
                Debug.Log("[HUDControlsEditor] Programmatically bound Cancel button click event.");
            }
        }
    }

    /// <summary>
    /// Called when the player clicks the "Edit Controls" button in the Pause Menu.
    /// </summary>
    public void EnterEditMode()
    {
        IsEditMode = true;

        // Hide Pause Menu
        if (UIManager.Instance != null && UIManager.Instance.pauseMenu != null)
        {
            UIManager.Instance.pauseMenu.SetActive(false);
        }

        // Show Edit Mode UI
        if (editModeUIPanel != null)
        {
            editModeUIPanel.SetActive(true);
        }

        // Tint background
        if (controlsPanelImage != null)
        {
            controlsPanelImage.color = editModePanelColor;
        }

        // Cache original positions in case of cancel
        foreach (var btn in draggableButtons)
        {
            btn.CacheOriginalPosition();
        }
    }

    /// <summary>
    /// Called when the player clicks "Save" in the Edit Mode UI.
    /// </summary>
    public void SaveEditMode()
    {
        IsEditMode = false;

        // Save all current positions
        foreach (var btn in draggableButtons)
        {
            btn.SaveCurrentPosition();
        }

        ExitEditModeVisuals();
    }

    /// <summary>
    /// Called when the player clicks "Cancel" in the Edit Mode UI.
    /// </summary>
    public void CancelEditMode()
    {
        IsEditMode = false;

        // Revert all positions
        foreach (var btn in draggableButtons)
        {
            btn.RevertToOriginalPosition();
        }

        ExitEditModeVisuals();
    }

    private void ExitEditModeVisuals()
    {
        // Hide Edit Mode UI
        if (editModeUIPanel != null)
        {
            editModeUIPanel.SetActive(false);
        }

        // Remove tint
        if (controlsPanelImage != null)
        {
            controlsPanelImage.color = normalPanelColor;
        }

        // Show Pause Menu again
        if (UIManager.Instance != null && UIManager.Instance.pauseMenu != null)
        {
            UIManager.Instance.pauseMenu.SetActive(true);
        }
    }
}
