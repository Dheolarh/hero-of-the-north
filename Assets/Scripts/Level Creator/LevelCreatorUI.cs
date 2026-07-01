using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Master UI controller for the Level Creator screen. Manages toolbar buttons,
/// palette tabs, tool selection (brush vs eraser), playtesting states, and local saving.
/// </summary>
public class LevelCreatorUI : MonoBehaviour
{
    public static LevelCreatorUI Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("The root UI Canvas or GameObject for the Editor interface.")]
    [SerializeField] private GameObject editorUIRoot;
    [Tooltip("Panel shown when playtest validation is successful.")]
    [SerializeField] private GameObject validationSuccessPanel;

    [Header("Palette Category Sub-Panels")]
    [SerializeField] private GameObject terrainPalettePanel;
    [SerializeField] private GameObject hazardsPalettePanel;
    [SerializeField] private GameObject essentialsPalettePanel;
    [SerializeField] private GameObject cameraSettingsPalettePanel;

    [Header("Camera Customizer Sliders")]
    [SerializeField] private Slider camOffsetXSlider;
    [SerializeField] private Slider camOffsetYSlider;
    [SerializeField] private Slider camOrthoSizeSlider;

    [Header("Controls & Buttons")]
    [SerializeField] private Button playtestButton;
    [SerializeField] private Button publishButton;
    [SerializeField] private TMP_Text selectedToolText;
    [SerializeField] private TMP_InputField levelNameInputField;

    [System.Serializable]
    public struct BrushPrefabMapping
    {
        [Tooltip("The tool name/tag for the button (e.g. 'Floor', 'PlatformIce', 'Spike 1').")]
        public string toolName;
        [Tooltip("The prefab to spawn when this tool is active.")]
        public GameObject prefab;
    }

    [Header("Custom Brush Mappings")]
    [SerializeField] private List<BrushPrefabMapping> customBrushes = new List<BrushPrefabMapping>();

    public GameObject GetPrefabForType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        string norm = typeName.Replace(" ", "").ToLower();
        foreach (var mapping in customBrushes)
        {
            if (!string.IsNullOrEmpty(mapping.toolName) && mapping.toolName.Replace(" ", "").ToLower() == norm)
            {
                return mapping.prefab;
            }
        }
        return null;
    }

    // ── Tool Selection State ──────────────────────────────────────────────────
    
    /// <summary>The type identifier of the currently selected placement asset.</summary>
    public string SelectedAsset { get; private set; } = "Floor";

    /// <summary>If true, placement actions will erase elements instead of painting them.</summary>
    public bool IsEraserActive { get; private set; } = false;

    /// <summary>If true, the editor is hidden and player physics/controls are active.</summary>
    public bool IsPlaytesting { get; private set; } = false;

    /// <summary>Has the player completed their own level in playtest mode?</summary>
    public bool HasValidatedPlaytest { get; private set; } = false;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired when the brush selection or eraser mode changes. Passes (selectedAsset, isEraserActive).</summary>
    public event Action<string, bool> OnToolChanged;

    /// <summary>Fired when toggling between editing and playtesting. Passes (isPlaytesting).</summary>
    public event Action<bool> OnPlaytestToggled;

    /// <summary>Fired when the user requests clearing the entire grid canvas.</summary>
    public event Action OnClearGridRequest;

    /// <summary>Fired when the user requests loading a saved local level.</summary>
    public event Action<CustomLevelData> OnLoadLevelRequest;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize UI display
        UpdateToolText();
        SetPublishButtonState(false);
        if (validationSuccessPanel != null)
            validationSuccessPanel.SetActive(false);

        // Dynamically inject Exit Playtest button (non-destructive)
        CreateDynamicExitPlaytestButton();
    }

    // ── Palette Navigation ───────────────────────────────────────────────────

    public void ShowTerrainPalette() => SetActivePalette(true, false, false, false);
    public void ShowHazardsPalette() => SetActivePalette(false, true, false, false);
    public void ShowEssentialsPalette() => SetActivePalette(false, false, true, false);
    public void ShowCameraSettingsPalette() => SetActivePalette(false, false, false, true);

    private void SetActivePalette(bool terrain, bool hazards, bool essentials, bool camera)
    {
        if (terrainPalettePanel != null) terrainPalettePanel.SetActive(terrain);
        if (hazardsPalettePanel != null) hazardsPalettePanel.SetActive(hazards);
        if (essentialsPalettePanel != null) essentialsPalettePanel.SetActive(essentials);
        if (cameraSettingsPalettePanel != null) cameraSettingsPalettePanel.SetActive(camera);

        if (camera)
        {
            InitializeCameraSettingsSliders();
        }
    }

    // ── Tool Selection API ───────────────────────────────────────────────────
    public void SelectAsset(string assetType)
    {
        SelectedAsset = assetType;
        IsEraserActive = false;
        UpdateToolText();
        OnToolChanged?.Invoke(SelectedAsset, IsEraserActive);
    }

    /// <summary>
    /// Toggles the Eraser tool.
    /// </summary>
    public void ToggleEraser()
    {
        IsEraserActive = !IsEraserActive;
        UpdateToolText();
        OnToolChanged?.Invoke(SelectedAsset, IsEraserActive);
    }

    public void UpdateToolText()
    {
        if (selectedToolText == null) return;

        if (GridPainter.Instance != null && GridPainter.Instance.GetSelectedObject() != null)
        {
            var selected = GridPainter.Instance.GetSelectedObject();
            string displayName = !string.IsNullOrEmpty(selected.customToolDisplayName) 
                ? selected.customToolDisplayName 
                : selected.assetTypeName;

            selectedToolText.text = $"Active Tool: <color=blue>{displayName}</color>";
        }
        else if (IsEraserActive)
        {
            selectedToolText.text = "Active Tool: <color=red>Eraser</color>";
        }
        else
        {
            selectedToolText.text = $"Active Tool: <color=blue>{SelectedAsset}</color>";
        }
    }

    // ── Playtest Mode Toggle ─────────────────────────────────────────────────

    /// <summary>
    /// Swaps between Editor Edit mode and active gameplay Playtest mode.
    /// Called by the Playtest button in the Inspector.
    /// </summary>
    public void TogglePlaytest()
    {
        IsPlaytesting = !IsPlaytesting;

        if (IsPlaytesting)
        {
            // Start playtest
            if (editorUIRoot != null) editorUIRoot.SetActive(false);
            if (exitPlaytestButton != null) exitPlaytestButton.SetActive(true);
            if (playtestButton != null)
            {
                var text = playtestButton.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = "Stop Test";
            }
            Debug.Log("[LevelCreatorUI] Playtest started.");
        }
        else
        {
            // Stop playtest and return to editor
            if (editorUIRoot != null) editorUIRoot.SetActive(true);
            if (exitPlaytestButton != null) exitPlaytestButton.SetActive(false);
            if (playtestButton != null)
            {
                var text = playtestButton.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = "Playtest";
            }
            if (validationSuccessPanel != null)
                validationSuccessPanel.SetActive(false);

            Debug.Log("[LevelCreatorUI] Playtest stopped. Returned to Editor.");
        }

        OnPlaytestToggled?.Invoke(IsPlaytesting);
    }

    /// <summary>
    /// Call this when the player successfully beats the custom level in playtest mode.
    /// Unlocks the Publish option.
    /// </summary>
    public void ValidatePlaytestSuccess()
    {
        if (!IsPlaytesting) return;

        HasValidatedPlaytest = true;
        SetPublishButtonState(true);

        if (validationSuccessPanel != null)
            validationSuccessPanel.SetActive(true);

        Debug.Log("[LevelCreatorUI] Custom level playtest validation success! Publish button unlocked.");
    }

    private void SetPublishButtonState(bool interactable)
    {
        if (publishButton != null)
        {
            publishButton.interactable = interactable;
        }
    }

    // ── Local Saving & Loading (Drafts) ──────────────────────────────────────

    public void SaveLevelDraft()
    {
        string levelName = levelNameInputField != null ? levelNameInputField.text : "My Custom Level";
        if (string.IsNullOrWhiteSpace(levelName))
            levelName = "Untitled Draft";

        string creatorName = DevvitBridge.Instance != null ? DevvitBridge.Instance.username : "EditorPlayer";

        CustomLevelData levelData = null;
        if (GridPainter.Instance != null)
        {
            levelData = GridPainter.Instance.ExportLevelData(levelName, creatorName);
        }
        else
        {
            levelData = new CustomLevelData
            {
                levelName = levelName,
                creator = creatorName
            };
        }

        string json = JsonUtility.ToJson(levelData, true);
        PlayerPrefs.SetString("CustomLevel_Draft", json);
        PlayerPrefs.Save();

        Debug.Log($"[LevelCreatorUI] Draft saved successfully! Name: {levelName}\nJSON Size: {json.Length} chars");
    }

    /// <summary>
    /// Loads the level layout draft from PlayerPrefs if it exists.
    /// Triggered by the Load button.
    /// </summary>
    public void LoadLevelDraft()
    {
        if (!PlayerPrefs.HasKey("CustomLevel_Draft"))
        {
            Debug.LogWarning("[LevelCreatorUI] No saved draft found in PlayerPrefs.");
            return;
        }

        string json = PlayerPrefs.GetString("CustomLevel_Draft");
        try
        {
            CustomLevelData data = JsonUtility.FromJson<CustomLevelData>(json);
            if (data != null)
            {
                if (levelNameInputField != null)
                    levelNameInputField.text = data.levelName;

                OnLoadLevelRequest?.Invoke(data);
                // Reset validation status on load since design might have changed
                HasValidatedPlaytest = false;
                SetPublishButtonState(false);

                Debug.Log($"[LevelCreatorUI] Draft loaded successfully! Name: {data.levelName}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LevelCreatorUI] Failed to parse loaded draft JSON: {e.Message}");
        }
    }

    // ── Publish to Reddit / Devvit ───────────────────────────────────────────

    /// <summary>
    /// Publishes the validated level design to Devvit backend and Reddit.
    /// Triggered by the Publish button.
    /// </summary>
    public void PublishLevel()
    {
        if (!HasValidatedPlaytest)
        {
            Debug.LogWarning("[LevelCreatorUI] Cannot publish unvalidated level. Playtest and complete the level first.");
            return;
        }

        string levelName = levelNameInputField != null ? levelNameInputField.text : "My Shared Level";
        if (string.IsNullOrWhiteSpace(levelName))
            levelName = "Shared Level";

        string creatorName = DevvitBridge.Instance != null ? DevvitBridge.Instance.username : "EditorPlayer";

        CustomLevelData levelData = null;
        if (GridPainter.Instance != null)
        {
            levelData = GridPainter.Instance.ExportLevelData(levelName, creatorName);
        }
        else
        {
            levelData = new CustomLevelData
            {
                levelName = levelName,
                creator = creatorName
            };
        }

        string json = JsonUtility.ToJson(levelData);

        Debug.Log($"[LevelCreatorUI] Publishing level to Reddit: {levelName} by {levelData.creator}");

#if UNITY_WEBGL && !UNITY_EDITOR
        // Send WebGL post message to the Devvit parent context
        // In Devvit, we listen to this event to save to Redis and generate a subreddit post.
        Application.ExternalCall("publishCustomLevel", json);
#else
        Debug.Log($"[LevelCreatorUI] [Editor Mock] Published event sent. JSON size: {json.Length} chars");
#endif
    }

    private bool isUpdatingCamUI = false;

    void Start()
    {
        // Wire up camera customization slider listeners
        if (camOffsetXSlider != null) camOffsetXSlider.onValueChanged.AddListener(OnCamOffsetXChanged);
        if (camOffsetYSlider != null) camOffsetYSlider.onValueChanged.AddListener(OnCamOffsetYChanged);
        if (camOrthoSizeSlider != null) camOrthoSizeSlider.onValueChanged.AddListener(OnCamOrthoSizeChanged);
    }

    private void InitializeCameraSettingsSliders()
    {
        var settings = FindFirstObjectByType<LevelCameraSettings>();
        if (settings == null) return;

        isUpdatingCamUI = true;

        if (camOffsetXSlider != null)
        {
            camOffsetXSlider.minValue = -15f;
            camOffsetXSlider.maxValue = 15f;
            camOffsetXSlider.value = settings.offset.x;
        }

        if (camOffsetYSlider != null)
        {
            camOffsetYSlider.minValue = -15f;
            camOffsetYSlider.maxValue = 15f;
            camOffsetYSlider.value = settings.offset.y;
        }

        if (camOrthoSizeSlider != null)
        {
            camOrthoSizeSlider.minValue = 2f;
            camOrthoSizeSlider.maxValue = 15f;
            camOrthoSizeSlider.value = settings.orthoSize;
        }

        isUpdatingCamUI = false;
    }

    private void OnCamOffsetXChanged(float val)
    {
        if (isUpdatingCamUI) return;
        var settings = FindFirstObjectByType<LevelCameraSettings>();
        if (settings != null)
        {
            settings.offset = new Vector3(val, settings.offset.y, settings.offset.z);
            if (GridPainter.Instance != null)
            {
                GridPainter.Instance.SnapCameraToPlayerStart();
            }
        }
    }

    private void OnCamOffsetYChanged(float val)
    {
        if (isUpdatingCamUI) return;
        var settings = FindFirstObjectByType<LevelCameraSettings>();
        if (settings != null)
        {
            settings.offset = new Vector3(settings.offset.x, val, settings.offset.z);
            // Sync fixed height in case Follow Y is disabled
            if (!settings.followY)
            {
                settings.fixedYHeight = val;
            }
            if (GridPainter.Instance != null)
            {
                GridPainter.Instance.SnapCameraToPlayerStart();
            }
        }
    }

    private void OnCamOrthoSizeChanged(float val)
    {
        if (isUpdatingCamUI) return;
        var settings = FindFirstObjectByType<LevelCameraSettings>();
        if (settings != null)
        {
            settings.orthoSize = val;
            
            // Force the active Main Camera to match size immediately during editing
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographicSize = val;
            }
            if (GridPainter.Instance != null)
            {
                GridPainter.Instance.SnapCameraToPlayerStart();
            }
        }
    }

    // ── Utility Requests ─────────────────────────────────────────────────────

    public void RequestSnapToPlayer()
    {
        if (GridPainter.Instance != null)
        {
            GridPainter.Instance.SnapCameraToPlayerStart();
        }
    }

    public void RequestClearGrid()
    {
        OnClearGridRequest?.Invoke();
        HasValidatedPlaytest = false;
        SetPublishButtonState(false);
        Debug.Log("[LevelCreatorUI] Grid clear requested.");
    }

    public void ExitEditor()
    {
        // Go back to the main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }

    private GameObject exitPlaytestButton;

    private void CreateDynamicExitPlaytestButton()
    {
        // Don't duplicate if already present
        Transform existing = transform.Find("ExitPlaytestButton");
        if (existing != null)
        {
            exitPlaytestButton = existing.gameObject;
            exitPlaytestButton.SetActive(false);
            return;
        }

        // Create the button container under Canvas
        exitPlaytestButton = new GameObject("ExitPlaytestButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        exitPlaytestButton.transform.SetParent(transform, false);

        // Position at Top-Right with margin
        RectTransform rt = exitPlaytestButton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(160f, 70f); // Match user button dimensions

        // Styled as a dark crimson red Slate button
        Image img = exitPlaytestButton.GetComponent<Image>();
        img.color = new Color(0.6f, 0.15f, 0.15f, 0.95f);

        // Add a neat border effect (optional, outline is clean)
        var outline = exitPlaytestButton.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.2f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Add a bold white label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObj.transform.SetParent(exitPlaytestButton.transform, false);
        TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
        txt.text = "Exit Test";
        txt.fontSize = 18;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        RectTransform rtLabel = labelObj.GetComponent<RectTransform>();
        rtLabel.anchorMin = Vector2.zero;
        rtLabel.anchorMax = Vector2.one;
        rtLabel.sizeDelta = Vector2.zero;

        // Attach listener
        Button btn = exitPlaytestButton.GetComponent<Button>();
        btn.onClick.AddListener(TogglePlaytest);

        // Start hidden in Edit Mode
        exitPlaytestButton.SetActive(false);
    }
}
