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

    [Header("Player Settings Data")]
    public float playerMoveSpeed = 5f;
    public float playerJumpForce = 7f;
    public int playerMaxJumps = 1;
    public bool playerEnableFallDamage = false;

    [Header("Object Transform Panel")]
    [SerializeField] private GameObject objectTransformPanel;

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

    [SerializeField] private GameObject mechanicsPopupPanel;

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

        if (GridPainter.Instance != null)
        {
            GridPainter.Instance.SpawnAssetAtCenter(assetType);
        }
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

        // Programmatically wire the third button under Tools to ToggleObjectTransformPanel
        Transform toolsTrans = transform.Find("EditorUIRoot/Tools");
        if (toolsTrans != null)
        {
            Button[] buttons = toolsTrans.GetComponentsInChildren<Button>(true);
            if (buttons.Length >= 3)
            {
                buttons[2].onClick.RemoveAllListeners();
                buttons[2].onClick.AddListener(ToggleObjectTransformPanel);
                Debug.Log("[LevelCreatorUI] Programmatically wired the third tool button to ToggleObjectTransformPanel.");
            }
        }
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

    /// <summary>
    /// Toggles the dynamic popup Mechanics Editor Panel.
    /// </summary>
    public void ToggleMechanicsPanel()
    {
        if (mechanicsPopupPanel == null)
        {
            // Try to find the panel dynamically under children or in the active/inactive scene
            var trans = transform.Find("MechanicsPopupPanel") ?? transform.Find("MechanicsEditorPanel");
            if (trans != null)
            {
                mechanicsPopupPanel = trans.gameObject;
            }
            else
            {
                var ctrl = FindFirstObjectByType<MechanicsEditorPanelController>(FindObjectsInactive.Include);
                if (ctrl != null)
                {
                    mechanicsPopupPanel = ctrl.gameObject;
                }
            }
        }

        if (mechanicsPopupPanel != null)
        {
            mechanicsPopupPanel.SetActive(!mechanicsPopupPanel.activeSelf);
            if (mechanicsPopupPanel.activeSelf)
            {
                var panelCtrl = mechanicsPopupPanel.GetComponent<MechanicsEditorPanelController>();
                if (panelCtrl != null)
                {
                    panelCtrl.RefreshCandidateList();
                }
            }
        }
        else
        {
            Debug.LogError("[LevelCreatorUI] mechanicsPopupPanel is not assigned and could not be found in the scene.");
        }
    }

    /// <summary>
    /// Toggles the dynamic popup Object Transform Panel.
    /// </summary>
    public void ToggleObjectTransformPanel()
    {
        if (objectTransformPanel == null)
        {
            var trans = transform.Find("ObjectTransformPanel");
            if (trans != null)
            {
                objectTransformPanel = trans.gameObject;
            }
            else
            {
                var ctrl = FindFirstObjectByType<ObjectTransformPanelController>(FindObjectsInactive.Include);
                if (ctrl != null)
                {
                    objectTransformPanel = ctrl.gameObject;
                }
            }
        }

        if (objectTransformPanel != null)
        {
            objectTransformPanel.SetActive(!objectTransformPanel.activeSelf);
            if (objectTransformPanel.activeSelf)
            {
                var panelCtrl = objectTransformPanel.GetComponent<ObjectTransformPanelController>();
                if (panelCtrl != null)
                {
                    panelCtrl.UpdatePlayerSettingsUI();
                }
            }
        }
        else
        {
            Debug.LogError("[LevelCreatorUI] objectTransformPanel is not assigned and could not be found.");
        }
    }

    private GameObject CreateScrollView(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject scrollObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(parent, false);

        RectTransform rt = scrollObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        scrollObj.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.11f, 1f); // darker inner backing

        // Viewport
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        
        RectTransform rtView = viewportObj.GetComponent<RectTransform>();
        rtView.anchorMin = Vector2.zero;
        rtView.anchorMax = Vector2.one;
        rtView.sizeDelta = Vector2.zero;
        viewportObj.GetComponent<Image>().color = Color.clear;

        // Content
        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform rtContent = contentObj.GetComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0f, 1f);
        rtContent.anchorMax = new Vector2(1f, 1f);
        rtContent.pivot = new Vector2(0.5f, 1f);
        rtContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.viewport = rtView;
        scrollRect.content = rtContent;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 25f;

        return scrollObj;
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogWarning($"[LevelCreatorUI] Field {fieldName} not found on {target.GetType().Name}");
        }
    }

    private GameObject CreateFooterButton(string name, Transform parent, string label, Color color)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        btnObj.GetComponent<Image>().color = color;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.sizeDelta = Vector2.zero;

        TMP_Text txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 15f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        return btnObj;
    }

    public void PromptForObjectName(PlacedEditorObject placedObj, Action<string> onConfirm, Action onCancel)
    {
        if (editorUIRoot == null) return;

        // Create modal backdrop overlay to block background clicks
        GameObject modalBackdrop = new GameObject("NamePromptModalBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(GraphicRaycaster));
        modalBackdrop.transform.SetParent(editorUIRoot.transform, false);

        RectTransform rtBackdrop = modalBackdrop.GetComponent<RectTransform>();
        rtBackdrop.anchorMin = Vector2.zero;
        rtBackdrop.anchorMax = Vector2.one;
        rtBackdrop.pivot = new Vector2(0.5f, 0.5f);
        rtBackdrop.anchoredPosition = Vector2.zero;
        rtBackdrop.sizeDelta = Vector2.zero;

        // Semi-transparent dark background
        Image imgBackdrop = modalBackdrop.GetComponent<Image>();
        imgBackdrop.color = new Color(0f, 0f, 0f, 0.6f);

        // Center Panel container
        GameObject dialogPanel = new GameObject("DialogPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogPanel.transform.SetParent(modalBackdrop.transform, false);

        RectTransform rtPanel = dialogPanel.GetComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0.35f, 0.4f);
        rtPanel.anchorMax = new Vector2(0.65f, 0.6f);
        rtPanel.pivot = new Vector2(0.5f, 0.5f);
        rtPanel.anchoredPosition = Vector2.zero;
        rtPanel.sizeDelta = Vector2.zero;

        Image imgPanel = dialogPanel.GetComponent<Image>();
        imgPanel.color = new Color(0.12f, 0.15f, 0.2f, 1f); // deep slate style matching the theme

        Outline outline = dialogPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.7f, 1f, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Title text
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(dialogPanel.transform, false);
        RectTransform rtTitle = titleObj.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0.05f, 0.75f);
        rtTitle.anchorMax = new Vector2(0.95f, 0.95f);
        rtTitle.pivot = new Vector2(0.5f, 0.5f);
        rtTitle.anchoredPosition = Vector2.zero;
        rtTitle.sizeDelta = Vector2.zero;

        TMP_Text txtTitle = titleObj.AddComponent<TextMeshProUGUI>();
        txtTitle.text = "Name Spawned Object";
        txtTitle.fontSize = 18;
        txtTitle.fontStyle = FontStyles.Bold;
        txtTitle.color = Color.white;
        txtTitle.alignment = TextAlignmentOptions.Center;

        // Input Field
        GameObject inputObj = new GameObject("NameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        inputObj.transform.SetParent(dialogPanel.transform, false);
        RectTransform rtInput = inputObj.GetComponent<RectTransform>();
        rtInput.anchorMin = new Vector2(0.1f, 0.4f);
        rtInput.anchorMax = new Vector2(0.9f, 0.65f);
        rtInput.pivot = new Vector2(0.5f, 0.5f);
        rtInput.anchoredPosition = Vector2.zero;
        rtInput.sizeDelta = Vector2.zero;

        inputObj.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.32f, 1f);

        // Viewport (TextArea)
        GameObject inputArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        inputArea.transform.SetParent(inputObj.transform, false);
        RectTransform rtInputArea = inputArea.GetComponent<RectTransform>();
        rtInputArea.anchorMin = Vector2.zero;
        rtInputArea.anchorMax = Vector2.one;
        rtInputArea.sizeDelta = new Vector2(-16f, -10f);

        // Text Component
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textObj.transform.SetParent(inputArea.transform, false);
        RectTransform rtText = textObj.GetComponent<RectTransform>();
        rtText.anchorMin = Vector2.zero;
        rtText.anchorMax = Vector2.one;
        rtText.sizeDelta = Vector2.zero;

        TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = 16f;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_InputField input = inputObj.GetComponent<TMP_InputField>();
        input.textViewport = rtInputArea;
        input.textComponent = tmpText;
        input.text = placedObj.gameObject.name;
        input.caretWidth = 2;
        input.customCaretColor = true;
        input.caretColor = Color.white;
        input.fontAsset = tmpText.font;
        input.selectionColor = new Color(0.2f, 0.44f, 1f, 0.5f);

        // Focus input field immediately
        input.ActivateInputField();

        // Footer buttons container
        GameObject footerObj = new GameObject("Footer", typeof(RectTransform));
        footerObj.transform.SetParent(dialogPanel.transform, false);
        RectTransform rtFooter = footerObj.GetComponent<RectTransform>();
        rtFooter.anchorMin = new Vector2(0.1f, 0.1f);
        rtFooter.anchorMax = new Vector2(0.9f, 0.3f);
        rtFooter.pivot = new Vector2(0.5f, 0.5f);
        rtFooter.anchoredPosition = Vector2.zero;
        rtFooter.sizeDelta = Vector2.zero;

        HorizontalLayoutGroup layout = footerObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        GameObject okayBtn = CreateFooterButton("OkayButton", footerObj.transform, "OK", new Color(0.18f, 0.65f, 0.35f, 1f));
        GameObject cancelBtn = CreateFooterButton("CancelButton", footerObj.transform, "CANCEL", new Color(0.8f, 0.2f, 0.2f, 1f));

        okayBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            string chosenName = input.text.Trim();
            if (string.IsNullOrEmpty(chosenName)) chosenName = placedObj.assetTypeName;
            placedObj.gameObject.name = chosenName;
            placedObj.customToolDisplayName = chosenName;
            Destroy(modalBackdrop);
            onConfirm?.Invoke(chosenName);
        });

        cancelBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            Destroy(modalBackdrop);
            onCancel?.Invoke();
        });
    }
}
