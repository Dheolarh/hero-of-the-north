using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility to automate the creation and configuration of the Level Creator scene.
/// Generates a clean, modern, and spacious UI layout utilizing Unity UI Layout Groups.
/// </summary>
public class LevelCreatorSetupHelper : EditorWindow
{
    [MenuItem("Tools/Level Creator Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<LevelCreatorSetupHelper>("Level Creator Setup Helper");
    }

    private void OnGUI()
    {
        GUILayout.Label("Level Creator Scene & Canvas Auto-Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label("This tool will:\n" +
                        "1. Create or open the 'LevelCreator' scene.\n" +
                        "2. Configure Main Camera with CameraFollow & CameraShake.\n" +
                        "3. Create GridPainter GameObject with script attached.\n" +
                        "4. Build a clean, spacious UI Canvas utilizing Layout Groups.\n" +
                        "5. Automatically wire all Button click events to scripts.\n" +
                        "6. Hook up default palette asset selections.", EditorStyles.wordWrappedLabel);

        GUILayout.Space(20);

        if (GUILayout.Button("Build Setup Now", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Confirm Build", 
                "This will build the Level Creator UI and GridPainter hierarchy in the current/new scene. Proceed?", "Yes", "No"))
            {
                RunSetup();
            }
        }
    }

    private static void RunSetup()
    {
        // 1. Ensure scene exists/is loaded
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "LevelCreator")
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                ActiveEditorTracker.sharedTracker.isLocked = false;
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, "Assets/Scenes/LevelCreator.unity");
            }
            else
            {
                return;
            }
        }

        // 2. Set Up Main Camera
        GameObject cameraObj = GameObject.FindWithTag("MainCamera");
        if (cameraObj == null)
        {
            cameraObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObj.tag = "MainCamera";
        }

        CameraFollow follow = cameraObj.GetComponent<CameraFollow>();
        if (follow == null) follow = cameraObj.AddComponent<CameraFollow>();

        CameraShake shake = cameraObj.GetComponent<CameraShake>();
        if (shake == null) shake = cameraObj.AddComponent<CameraShake>();

        // 3. Set Up GridPainter
        GameObject painterObj = GameObject.Find("GridPainter");
        if (painterObj == null)
        {
            painterObj = new GameObject("GridPainter");
        }
        GridPainter painter = painterObj.GetComponent<GridPainter>();
        if (painter == null) painter = painterObj.AddComponent<GridPainter>();

        // Set camera target reference on GridPainter via reflection
        var camField = typeof(GridPainter).GetField("editorCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (camField != null)
        {
            camField.SetValue(painter, cameraObj.GetComponent<Camera>());
        }

        // 4. Set Up UI Canvas
        GameObject canvasObj = GameObject.Find("LevelCreatorCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("LevelCreatorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Configure CanvasScaler to scale with 1080x1920 screen size so it scales perfectly on all aspect ratios
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // Scale by width for portrait screens
        }

        // Add EventSystem if missing
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // Attach UI Manager
        LevelCreatorUI ui = canvasObj.GetComponent<LevelCreatorUI>();
        if (ui == null) ui = canvasObj.AddComponent<LevelCreatorUI>();

        // Modern Slate Palette colors
        Color panelBgColor = new Color(0.12f, 0.15f, 0.2f, 0.9f);
        Color buttonColor = new Color(0.2f, 0.25f, 0.32f, 1f);

        // Create Panel structure (stretches to fill whole screen)
        GameObject uiRoot = CreatePanel("EditorUIRoot", canvasObj.transform, panelBgColor);
        RectTransform rtRoot = uiRoot.GetComponent<RectTransform>();
        rtRoot.anchorMin = Vector2.zero;
        rtRoot.anchorMax = Vector2.one;
        rtRoot.pivot = new Vector2(0.5f, 0.5f);
        rtRoot.anchoredPosition = Vector2.zero;
        rtRoot.sizeDelta = Vector2.zero;
        
        // ── TOP TOOLBAR SETUP (Horizontal Layout Group, firmly anchored at top) ──────────────────────
        GameObject topBar = CreatePanel("TopToolbarPanel", uiRoot.transform, panelBgColor);
        RectTransform rtTop = topBar.GetComponent<RectTransform>();
        rtTop.anchorMin = new Vector2(0f, 1f);
        rtTop.anchorMax = new Vector2(1f, 1f);
        rtTop.pivot = new Vector2(0.5f, 1f);
        rtTop.anchoredPosition = Vector2.zero;
        rtTop.sizeDelta = new Vector2(0f, 180f); // High height of 180 units to fit 100 height buttons

        HorizontalLayoutGroup topLayout = topBar.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(20, 20, 15, 15);
        topLayout.spacing = 15f;
        topLayout.childAlignment = TextAnchor.MiddleCenter;
        topLayout.childControlWidth = false;
        topLayout.childControlHeight = false;
        topLayout.childForceExpandWidth = false;
        topLayout.childForceExpandHeight = false;

        // Tool text (Label)
        GameObject toolTextObj = new GameObject("SelectedToolText", typeof(RectTransform), typeof(CanvasRenderer));
        toolTextObj.transform.SetParent(topBar.transform, false);
        TMP_Text toolText = toolTextObj.AddComponent<TextMeshProUGUI>();
        toolText.fontSize = 22;
        toolText.alignment = TextAlignmentOptions.Center;
        toolText.color = Color.white;
        toolTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 100f);
        SetPrivateField(ui, "selectedToolText", toolText);

        // Name input field
        GameObject inputObj = new GameObject("LevelNameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        inputObj.transform.SetParent(topBar.transform, false);
        inputObj.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
        inputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 100f);
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        
        GameObject textarea = new GameObject("TextArea", typeof(RectTransform));
        textarea.transform.SetParent(inputObj.transform, false);
        RectTransform rtTextarea = textarea.GetComponent<RectTransform>();
        rtTextarea.anchorMin = Vector2.zero;
        rtTextarea.anchorMax = Vector2.one;
        rtTextarea.sizeDelta = Vector2.zero;

        GameObject textDisplayObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textDisplayObj.transform.SetParent(textarea.transform, false);
        TMP_Text textDisplay = textDisplayObj.AddComponent<TextMeshProUGUI>();
        textDisplay.fontSize = 18;
        textDisplay.color = Color.white;
        textDisplay.alignment = TextAlignmentOptions.Center;
        RectTransform rtDisplay = textDisplayObj.GetComponent<RectTransform>();
        rtDisplay.anchorMin = Vector2.zero;
        rtDisplay.anchorMax = Vector2.one;
        rtDisplay.sizeDelta = Vector2.zero;

        inputField.textComponent = textDisplay;
        inputField.text = "My Custom Level";
        SetPrivateField(ui, "levelNameInputField", inputField);

        // Toolbar Buttons (150 width, 100 height)
        GameObject playtestBtnObj = CreateButton("PlaytestButton", topBar.transform, "Playtest", buttonColor, new Vector2(150f, 100f));
        GameObject saveBtnObj = CreateButton("SaveButton", topBar.transform, "Save", buttonColor, new Vector2(150f, 100f));
        GameObject loadBtnObj = CreateButton("LoadButton", topBar.transform, "Load", buttonColor, new Vector2(150f, 100f));
        GameObject clearBtnObj = CreateButton("ClearButton", topBar.transform, "Clear", buttonColor, new Vector2(150f, 100f));
        GameObject publishBtnObj = CreateButton("PublishButton", topBar.transform, "Publish", buttonColor, new Vector2(150f, 100f));
        GameObject eraserBtnObj = CreateButton("EraserButton", topBar.transform, "Eraser", buttonColor, new Vector2(150f, 100f));

        SetPrivateField(ui, "playtestButton", playtestBtnObj.GetComponent<Button>());
        SetPrivateField(ui, "publishButton", publishBtnObj.GetComponent<Button>());

        // Wire events
        UnityEventTools.AddPersistentListener(playtestBtnObj.GetComponent<Button>().onClick, ui.TogglePlaytest);
        UnityEventTools.AddPersistentListener(saveBtnObj.GetComponent<Button>().onClick, ui.SaveLevelDraft);
        UnityEventTools.AddPersistentListener(loadBtnObj.GetComponent<Button>().onClick, ui.LoadLevelDraft);
        UnityEventTools.AddPersistentListener(clearBtnObj.GetComponent<Button>().onClick, ui.RequestClearGrid);
        UnityEventTools.AddPersistentListener(publishBtnObj.GetComponent<Button>().onClick, ui.PublishLevel);
        UnityEventTools.AddPersistentListener(eraserBtnObj.GetComponent<Button>().onClick, ui.ToggleEraser);

        // ── BOTTOM PALETTE PANEL (Vertical flow, firmly anchored at bottom) ─────────────
        GameObject bottomPanelObj = CreatePanel("BottomPalettePanel", uiRoot.transform, panelBgColor);
        RectTransform rtBot = bottomPanelObj.GetComponent<RectTransform>();
        rtBot.anchorMin = new Vector2(0f, 0f);
        rtBot.anchorMax = new Vector2(1f, 0f);
        rtBot.pivot = new Vector2(0.5f, 0f);
        rtBot.anchoredPosition = Vector2.zero;
        rtBot.sizeDelta = new Vector2(0f, 400f); // High height of 400 units to fit large buttons & sliders

        VerticalLayoutGroup botLayout = bottomPanelObj.AddComponent<VerticalLayoutGroup>();
        botLayout.padding = new RectOffset(20, 20, 15, 15);
        botLayout.spacing = 15f;
        botLayout.childAlignment = TextAnchor.MiddleCenter;
        botLayout.childControlWidth = false;
        botLayout.childControlHeight = false;
        botLayout.childForceExpandWidth = false;
        botLayout.childForceExpandHeight = false;

        // Row 1: Category Tab selectors (Horizontal Row)
        GameObject tabRowObj = new GameObject("TabSelectorRow", typeof(RectTransform));
        tabRowObj.transform.SetParent(bottomPanelObj.transform, false);
        tabRowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 110f); // Fits 100 height buttons
        HorizontalLayoutGroup tabLayout = tabRowObj.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 15f;
        tabLayout.childAlignment = TextAnchor.MiddleCenter;

        GameObject tabTerrain = CreateButton("TabTerrainButton", tabRowObj.transform, "Terrain", buttonColor, new Vector2(150f, 100f));
        GameObject tabHazards = CreateButton("TabHazardsButton", tabRowObj.transform, "Hazards", buttonColor, new Vector2(150f, 100f));
        GameObject tabEssentials = CreateButton("TabEssentialsButton", tabRowObj.transform, "Essentials", buttonColor, new Vector2(150f, 100f));
        GameObject tabCamera = CreateButton("TabCameraButton", tabRowObj.transform, "Camera", buttonColor, new Vector2(150f, 100f));

        UnityEventTools.AddPersistentListener(tabTerrain.GetComponent<Button>().onClick, ui.ShowTerrainPalette);
        UnityEventTools.AddPersistentListener(tabHazards.GetComponent<Button>().onClick, ui.ShowHazardsPalette);
        UnityEventTools.AddPersistentListener(tabEssentials.GetComponent<Button>().onClick, ui.ShowEssentialsPalette);
        UnityEventTools.AddPersistentListener(tabCamera.GetComponent<Button>().onClick, ui.ShowCameraSettingsPalette);

        // Row 2: Selected active sub-palette (holds the actual brush buttons)
        GameObject subPaletteRowObj = new GameObject("SubPaletteRow", typeof(RectTransform));
        subPaletteRowObj.transform.SetParent(bottomPanelObj.transform, false);
        subPaletteRowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 110f); // Fits 100 height buttons

        // Terrain Brushes (Scrollable)
        GameObject terrainPalette = CreateScrollablePalette("TerrainPalette", subPaletteRowObj.transform, buttonColor);
        Transform terrainContent = terrainPalette.GetComponent<ScrollRect>().content;

        GameObject floorBtn = CreateButton("FloorBrush", terrainContent, "Floor", buttonColor, new Vector2(150f, 100f));
        GameObject iceBtn = CreateButton("IceBrush", terrainContent, "Ice", buttonColor, new Vector2(150f, 100f));
        GameObject platBtn = CreateButton("MovingPlatBrush", terrainContent, "PingPong", buttonColor, new Vector2(150f, 100f));
        GameObject platformBtn = CreateButton("PlatformBrush", terrainContent, "Platform", buttonColor, new Vector2(150f, 100f));
        GameObject platform1Btn = CreateButton("Platform1Brush", terrainContent, "Platform 1", buttonColor, new Vector2(150f, 100f));
        GameObject skyBtn = CreateButton("SkyBrush", terrainContent, "Sky", buttonColor, new Vector2(150f, 100f));
        GameObject skyDeathBtn = CreateButton("SkyDeathBrush", terrainContent, "Sky Death", buttonColor, new Vector2(150f, 100f));
        GameObject barrierBtn = CreateButton("BarrierBrush", terrainContent, "Barrier", buttonColor, new Vector2(150f, 100f));
        GameObject bgBtn = CreateButton("BackgroundBrush", terrainContent, "Background", buttonColor, new Vector2(150f, 100f));

        AddStringSelectEvent(floorBtn.GetComponent<Button>(), ui, "Floor");
        AddStringSelectEvent(iceBtn.GetComponent<Button>(), ui, "PlatformIce");
        AddStringSelectEvent(platBtn.GetComponent<Button>(), ui, "MovingPlatform");
        AddStringSelectEvent(platformBtn.GetComponent<Button>(), ui, "Platform");
        AddStringSelectEvent(platform1Btn.GetComponent<Button>(), ui, "Platform 1");
        AddStringSelectEvent(skyBtn.GetComponent<Button>(), ui, "Sky");
        AddStringSelectEvent(skyDeathBtn.GetComponent<Button>(), ui, "Sky Death");
        AddStringSelectEvent(barrierBtn.GetComponent<Button>(), ui, "Barrier");
        AddStringSelectEvent(bgBtn.GetComponent<Button>(), ui, "Background");

        // Hazards Brushes (Scrollable)
        GameObject hazardsPalette = CreateScrollablePalette("HazardsPalette", subPaletteRowObj.transform, buttonColor);
        Transform hazardsContent = hazardsPalette.GetComponent<ScrollRect>().content;

        GameObject spikeBtn = CreateButton("SpikeBrush", hazardsContent, "Spikes", buttonColor, new Vector2(150f, 100f));
        GameObject spike1Btn = CreateButton("Spike1Brush", hazardsContent, "Spike 1", buttonColor, new Vector2(150f, 100f));
        GameObject rollerBtn = CreateButton("RollerBrush", hazardsContent, "Roller", buttonColor, new Vector2(150f, 100f));

        AddStringSelectEvent(spikeBtn.GetComponent<Button>(), ui, "SpikesMetal");
        AddStringSelectEvent(spike1Btn.GetComponent<Button>(), ui, "Spike 1");
        AddStringSelectEvent(rollerBtn.GetComponent<Button>(), ui, "Roller");
        hazardsPalette.SetActive(false);

        // Essentials Brushes (Scrollable)
        GameObject essentialsPalette = CreateScrollablePalette("EssentialsPalette", subPaletteRowObj.transform, buttonColor);
        Transform essentialsContent = essentialsPalette.GetComponent<ScrollRect>().content;

        GameObject startBtn = CreateButton("PlayerStartBrush", essentialsContent, "Hero", buttonColor, new Vector2(150f, 100f));
        GameObject portalBtn = CreateButton("GoalPortalBrush", essentialsContent, "Portal", buttonColor, new Vector2(150f, 100f));

        AddStringSelectEvent(startBtn.GetComponent<Button>(), ui, "PlayerStart");
        AddStringSelectEvent(portalBtn.GetComponent<Button>(), ui, "Goal");
        essentialsPalette.SetActive(false);

        // Camera Settings Customizer Panel (Static sliders layout, no scroll needed)
        GameObject cameraPalette = CreatePanel("CameraSettingsPalette", subPaletteRowObj.transform, Color.clear, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
        HorizontalLayoutGroup camLayout = cameraPalette.AddComponent<HorizontalLayoutGroup>();
        camLayout.spacing = 25f;
        camLayout.childAlignment = TextAnchor.MiddleCenter;

        Slider sliderX, sliderY, sliderZoom;
        CreateSliderWithLabel("OffsetXSlider", cameraPalette.transform, "Cam Offset X", out sliderX);
        CreateSliderWithLabel("OffsetYSlider", cameraPalette.transform, "Cam Offset Y", out sliderY);
        CreateSliderWithLabel("ZoomSlider", cameraPalette.transform, "Cam Zoom / Size", out sliderZoom);
        cameraPalette.SetActive(false);

        // Bind panel fields on LevelCreatorUI using reflection
        SetPrivateField(ui, "editorUIRoot", uiRoot);
        SetPrivateField(ui, "terrainPalettePanel", terrainPalette);
        SetPrivateField(ui, "hazardsPalettePanel", hazardsPalette);
        SetPrivateField(ui, "essentialsPalettePanel", essentialsPalette);
        SetPrivateField(ui, "cameraSettingsPalettePanel", cameraPalette);
        SetPrivateField(ui, "camOffsetXSlider", sliderX);
        SetPrivateField(ui, "camOffsetYSlider", sliderY);
        SetPrivateField(ui, "camOrthoSizeSlider", sliderZoom);

        // Mark Scene Dirty to allow saving
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Setup Complete", 
            "Spacious Layout Creator set up successfully!\n" +
            "- Everything is automatically arranged using Layout Groups.\n" +
            "- Attach your prefabs to the GridPainter's Palette registry.", "Ok");
    }

    // ── Generator Helpers ────────────────────────────────────────────────────

    private static GameObject CreatePanel(string name, Transform parent, Color bgColor, Vector2? min = null, Vector2? max = null, Vector2? pivot = null, Vector2? size = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        
        Image img = go.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = false; // Disable raycast target so background panels don't block clicks on the scene

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min ?? Vector2.zero;
        rt.anchorMax = max ?? Vector2.one;
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        
        if (size != null)
        {
            if (min == max)
                rt.sizeDelta = size.Value;
            else
                rt.offsetMax = new Vector2(rt.offsetMax.x, size.Value.y); // top bar height offset
        }

        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string label, Color bgColor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.color = bgColor;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObj.transform.SetParent(go.transform, false);
        TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 16;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        RectTransform rtLabel = labelObj.GetComponent<RectTransform>();
        rtLabel.anchorMin = Vector2.zero;
        rtLabel.anchorMax = Vector2.one;
        rtLabel.sizeDelta = Vector2.zero;

        return go;
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    private static void AddStringSelectEvent(Button button, LevelCreatorUI ui, string assetName)
    {
        UnityEventTools.AddStringPersistentListener(button.onClick, ui.SelectAsset, assetName);
    }

    private static GameObject CreateSliderWithLabel(string name, Transform parent, string labelText, out Slider outSlider)
    {
        // Container
        GameObject container = new GameObject(name, typeof(RectTransform));
        container.transform.SetParent(parent, false);
        container.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 70f);

        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObj.transform.SetParent(container.transform, false);
        TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
        txt.text = labelText;
        txt.fontSize = 14;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        // Slider Object
        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(CanvasRenderer));
        sliderObj.transform.SetParent(container.transform, false);
        sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 20f);

        // Background
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        bgObj.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f, 1f);
        RectTransform rtBg = bgObj.GetComponent<RectTransform>();
        rtBg.anchorMin = new Vector2(0f, 0.25f);
        rtBg.anchorMax = new Vector2(1f, 0.75f);
        rtBg.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform rtFillArea = fillArea.GetComponent<RectTransform>();
        rtFillArea.anchorMin = new Vector2(0f, 0.25f);
        rtFillArea.anchorMax = new Vector2(1f, 0.75f);
        rtFillArea.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = Color.cyan;
        RectTransform rtFill = fill.GetComponent<RectTransform>();
        rtFill.sizeDelta = Vector2.zero;

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform rtHandleArea = handleArea.GetComponent<RectTransform>();
        rtHandleArea.anchorMin = Vector2.zero;
        rtHandleArea.anchorMax = Vector2.one;
        rtHandleArea.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        handle.GetComponent<Image>().color = Color.white;
        RectTransform rtHandle = handle.GetComponent<RectTransform>();
        rtHandle.sizeDelta = new Vector2(15f, 0f); // Width 15, matches height of area

        // Slider Component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = rtFill;
        slider.handleRect = rtHandle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;

        outSlider = slider;
        return container;
    }

    private static GameObject CreateScrollablePalette(string name, Transform parent, Color buttonColor)
    {
        // 1. Create the Scroll View GameObject
        GameObject scrollView = CreatePanel(name, parent, Color.clear, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);

        // 2. Add ScrollRect component (horizontal only, hide scrollbars)
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.horizontalScrollbar = null;
        scrollRect.verticalScrollbar = null;

        // 3. Create Viewport (Masks buttons outside the bounds)
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollView.transform, false);

        RectTransform rtView = viewport.GetComponent<RectTransform>();
        rtView.anchorMin = Vector2.zero;
        rtView.anchorMax = Vector2.one;
        rtView.pivot = new Vector2(0f, 1f);
        rtView.sizeDelta = Vector2.zero;

        // Visual graphic for dragging interaction
        Image img = viewport.GetComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = true; // MUST be true for click-dragging

        // 4. Create Content container (grows horizontally)
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);

        RectTransform rtContent = content.GetComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0f, 0f);
        rtContent.anchorMax = new Vector2(0f, 1f); // Stretch height, dynamic width
        rtContent.pivot = new Vector2(0f, 0.5f);
        rtContent.sizeDelta = Vector2.zero;

        // Auto layout using Horizontal Layout Group
        HorizontalLayoutGroup layout = content.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15f;
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Auto resize based on total button count
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Connect components
        scrollRect.viewport = rtView;
        scrollRect.content = rtContent;

        return scrollView;
    }
}
