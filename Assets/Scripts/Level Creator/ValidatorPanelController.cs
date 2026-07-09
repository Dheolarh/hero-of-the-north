using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Validator Panel UI for all confirmation/notification scenarios
/// in the Level Creator: delete, publish, save success, load, and single-instance warnings.
/// </summary>
public class ValidatorPanelController : MonoBehaviour
{
    public static ValidatorPanelController Instance { get; private set; }

    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject levelsListGroup;     // Levels List scroll view parent
    [SerializeField] private Transform levelsContent;         // Content transform — level buttons spawn here
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject actionButtonsGroup;   // Parent of Cancel + Proceed
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button proceedButton;

    [Header("Level List Styling")]
    [SerializeField] private Color normalLevelColor   = new Color(0.20f, 0.24f, 0.34f, 1f);
    [SerializeField] private Color selectedLevelColor = new Color(0.27f, 0.55f, 0.95f, 1f);

    [Header("Level Button Prefab")]
    [Tooltip("Prefab used for each level entry in the load list. Must have a Button component.")]
    [SerializeField] private GameObject levelButtonPrefab;
    [Tooltip("Name of the child GameObject inside the prefab that holds the TMP_Text for the level name.")]
    [SerializeField] private string levelNameTextField = "Text";

    private Action onProceedAction;
    private Action onCancelAction;
    private string selectedLevelKey;
    private readonly List<GameObject> spawnedLevelButtons = new List<GameObject>();

    // ── Unity lifecycle ─────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        HidePanel();
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>Shows delete confirmation. Proceed = confirm, Cancel = go back.</summary>
    public void ShowDeleteConfirm(Action onConfirm, Action onCancel = null)
    {
        PrepareShow("Are you sure you want to delete this scene?", showMessage: true, showActions: true, showClose: false);
        SetActions(onConfirm, onCancel ?? HidePanel);
    }

    /// <summary>Shows publish confirmation after playtest success. Cancel returns to editor.</summary>
    public void ShowPublishConfirm(Action onConfirm, Action onCancel = null)
    {
        PrepareShow("Do you want to publish this level now?", showMessage: true, showActions: true, showClose: false);
        SetActions(onConfirm, onCancel ?? HidePanel);
    }

    /// <summary>Shows "Scene saved!" — only Close button is active.</summary>
    public void ShowSaveSuccess()
    {
        PrepareShow("Scene saved!", showMessage: true, showActions: false, showClose: true);
    }

    /// <summary>Shows the level list for loading. Selecting a level + Proceed loads it.</summary>
    public void ShowLoadPanel(Action<string> onLoadKey)
    {
        PrepareShow(message: null, showMessage: false, showActions: true, showClose: false);
        if (levelsListGroup != null) levelsListGroup.SetActive(true);

        selectedLevelKey = null;
        PopulateLevelList();

        onProceedAction = () =>
        {
            if (!string.IsNullOrEmpty(selectedLevelKey))
            {
                onLoadKey?.Invoke(selectedLevelKey);
                HidePanel();
            }
        };
        onCancelAction = HidePanel;
    }

    /// <summary>Shows a single-instance warning — only Close button active.</summary>
    public void ShowSingleInstanceWarning(string objectName)
    {
        PrepareShow($"Only one {objectName} is allowed in the Scene.", showMessage: true, showActions: false, showClose: true);
    }

    public void Close() => HidePanel();

    // ── Private helpers ─────────────────────────────────────────────────────

    private void PrepareShow(string message, bool showMessage, bool showActions, bool showClose)
    {
        // Wire buttons
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            // Close button behaves like Cancel in every mode
            // (for save-success, onCancelAction is null so it just hides)
            closeButton.onClick.AddListener(OnCancelClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
        if (proceedButton != null)
        {
            proceedButton.onClick.RemoveAllListeners();
            proceedButton.onClick.AddListener(OnProceedClicked);
        }

        // Show/hide elements
        if (messageText != null)
        {
            messageText.gameObject.SetActive(showMessage);
            if (showMessage && message != null) messageText.text = message;
        }
        if (levelsListGroup != null)  levelsListGroup.SetActive(false); // ShowLoadPanel turns this on
        if (actionButtonsGroup != null) actionButtonsGroup.SetActive(showActions);

        // Close button is ALWAYS visible in every mode
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private void SetActions(Action onProceed, Action onCancel)
    {
        onProceedAction = onProceed;
        onCancelAction  = onCancel;
    }

    private void OnProceedClicked()
    {
        var action = onProceedAction;
        HidePanel();
        action?.Invoke();
    }

    private void OnCancelClicked()
    {
        var action = onCancelAction;
        HidePanel();
        action?.Invoke();
    }

    private void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        ClearLevelButtons();
        onProceedAction  = null;
        onCancelAction   = null;
        selectedLevelKey = null;
    }

    // ── Level list ──────────────────────────────────────────────────────────

    private void PopulateLevelList()
    {
        ClearLevelButtons();
        if (levelsContent == null) return;

        var keys = GetAllSavedLevelKeys();
        if (keys.Count == 0)
        {
            if (messageText != null) { messageText.gameObject.SetActive(true); messageText.text = "No saved levels found."; }
            if (actionButtonsGroup != null) actionButtonsGroup.SetActive(false);
            return;
        }

        foreach (string key in keys)
        {
            string capturedKey = key;
            string levelName   = GetLevelName(key);
            GameObject btnObj  = CreateLevelButton(levelName);
            btnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedLevelKey = capturedKey;
                HighlightSelected(btnObj);
            });
        }
    }

    private GameObject CreateLevelButton(string label)
    {
        GameObject btnObj;

        if (levelButtonPrefab != null)
        {
            // Use the designer-assigned prefab
            btnObj = Instantiate(levelButtonPrefab, levelsContent);
            btnObj.name = $"LvlBtn_{label}";

            // Find the named text child and set the level name
            Transform textChild = btnObj.transform.Find(levelNameTextField);
            if (textChild == null)
            {
                // Fall back to first TMP_Text found anywhere in the prefab
                textChild = btnObj.GetComponentInChildren<TextMeshProUGUI>(true)?.transform;
            }
            if (textChild != null)
            {
                var txt = textChild.GetComponent<TextMeshProUGUI>();
                if (txt != null) txt.text = label;
            }

            // Reset normal colour to the configured tint
            var img = btnObj.GetComponent<Image>();
            if (img != null) img.color = normalLevelColor;
        }
        else
        {
            // Code-built fallback (no prefab assigned)
            btnObj = new GameObject($"LvlBtn_{label}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(levelsContent, false);

            var rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 52f);

            var img = btnObj.GetComponent<Image>();
            img.color = normalLevelColor;

            var labelObj = new GameObject(levelNameTextField, typeof(RectTransform), typeof(CanvasRenderer));
            labelObj.transform.SetParent(btnObj.transform, false);
            var txt = labelObj.AddComponent<TextMeshProUGUI>();
            txt.text      = label;
            txt.fontSize  = 18;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color     = Color.white;

            var rtLabel = labelObj.GetComponent<RectTransform>();
            rtLabel.anchorMin = Vector2.zero;
            rtLabel.anchorMax = Vector2.one;
            rtLabel.offsetMin = new Vector2(8f, 4f);
            rtLabel.offsetMax = new Vector2(-8f, -4f);
        }

        spawnedLevelButtons.Add(btnObj);
        return btnObj;
    }

    private void HighlightSelected(GameObject selected)
    {
        foreach (var btn in spawnedLevelButtons)
        {
            if (btn == null) continue;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = (btn == selected) ? selectedLevelColor : normalLevelColor;
        }
    }

    private void ClearLevelButtons()
    {
        foreach (var btn in spawnedLevelButtons)
            if (btn != null) Destroy(btn);
        spawnedLevelButtons.Clear();
    }

    // ── Static save-slot helpers (used by LevelCreatorUI) ──────────────────

    /// <summary>Returns all existing save slot keys in creation order.</summary>
    public static List<string> GetAllSavedLevelKeys()
    {
        int count = PlayerPrefs.GetInt("CustomLevel_Count", 0);
        var keys  = new List<string>();
        for (int i = 1; i <= count; i++)
        {
            string k = $"CustomLevel_{i}";
            if (PlayerPrefs.HasKey(k)) keys.Add(k);
        }
        return keys;
    }

    /// <summary>Returns the next available slot number (count + 1).</summary>
    public static int GetNextSlotNumber() => PlayerPrefs.GetInt("CustomLevel_Count", 0) + 1;

    /// <summary>Returns the level name stored inside a slot's JSON.</summary>
    public static string GetLevelName(string key)
    {
        string json = PlayerPrefs.GetString(key, "{}");
        try   { return JsonUtility.FromJson<CustomLevelData>(json)?.levelName ?? key; }
        catch { return key; }
    }
}
