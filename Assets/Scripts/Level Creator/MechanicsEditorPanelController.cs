using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime UI controller for the Mechanics Editor Panel.
/// Dynamically filters candidates, manages trigger-trap wiring checklist,
/// and updates CollisionsAndTriggers components.
/// </summary>
public class MechanicsEditorPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private RectTransform listContent;
    [SerializeField] private RectTransform propertiesContent;

    [Header("Style Colors")]
    private Color panelColor = new Color(0.12f, 0.15f, 0.2f, 0.95f);
    private Color buttonColor = new Color(0.2f, 0.25f, 0.32f, 1f);
    private Color activeAccentColor = new Color(0.2f, 0.7f, 1f, 1f);
    private Color dangerColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    private PlacedEditorObject activeEditingTrigger;
    private CollisionsAndTriggers activeTriggerScript;

    private List<PlacedEditorObject> allSelectableObjects = new List<PlacedEditorObject>();
    private List<PlacedEditorObject> savedTrps = new List<PlacedEditorObject>();

    public void Initialize()
    {
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.RemoveAllListeners();
            searchInputField.onValueChanged.AddListener(OnTrapNameChanged);
        }
        RefreshCandidateList();
        ShowSelectPrompt();
    }

    private void OnTrapNameChanged(string val)
    {
        if (activeEditingTrigger != null)
        {
            activeEditingTrigger.gameObject.name = val;
            activeEditingTrigger.customToolDisplayName = val;

            // Refresh the top Saved Traps list to reflect the rename
            RefreshCandidateList();

            // Refresh the properties title/labels if active
            RefreshWiringPanelIfActive(activeEditingTrigger);

            if (LevelCreatorUI.Instance != null)
            {
                LevelCreatorUI.Instance.UpdateToolText();
            }
        }
    }

    public void RefreshWiringPanelIfActive(PlacedEditorObject obj)
    {
        if (activeEditingTrigger == obj)
        {
            // Update the title field or refresh panel values, but don't clear the input field's cursor!
        }
    }

    /// <summary>
    /// Refreshes the list of existing/saved traps in the level.
    /// Also updates the global list of selectable scene objects.
    /// </summary>
    public void RefreshCandidateList()
    {
        // Clear old list items
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        allSelectableObjects.Clear();
        savedTrps.Clear();

        if (GridPainter.Instance == null) return;

        var allPlaced = GridPainter.Instance.GetPlacedObjects();

        foreach (var obj in allPlaced)
        {
            if (obj == null) continue;

            string nameLower = obj.name.ToLower();
            string typeLower = obj.assetTypeName != null ? obj.assetTypeName.ToLower() : "";

            // Filter out non-interactive environment elements from the selectable objects list
            if (nameLower.Contains("cloud") || nameLower.Contains("background") || nameLower.Contains("camerasettings") || nameLower.Contains("wire"))
                continue;
            if (typeLower.Contains("cloud") || typeLower.Contains("background") || typeLower.Contains("camerasettings"))
                continue;

            allSelectableObjects.Add(obj);

            // Populate Saved Traps list if this object has the trigger component
            if (obj.GetComponent<CollisionsAndTriggers>() != null)
            {
                savedTrps.Add(obj);
            }
        }

        // Create the "Create Trap" button at the first slot of the scroll list
        CreateCreateTrapButton();

        // Build Saved Traps list elements at the top
        foreach (var trap in savedTrps)
        {
            CreateCandidateListItem(trap);
        }
    }

    private void CreateCandidateListItem(PlacedEditorObject candidate)
    {
        GameObject itemObj = new GameObject($"Item_{candidate.name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        itemObj.transform.SetParent(listContent, false);
        itemObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 80f);

        Image img = itemObj.GetComponent<Image>();
        img.color = new Color(0.18f, 0.22f, 0.28f, 1f);

        // Add layout group
        HorizontalLayoutGroup layout = itemObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 5, 5);
        layout.spacing = 15f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObj.transform.SetParent(itemObj.transform, false);
        TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
        txt.text = $"{candidate.name} ({candidate.assetTypeName})";
        txt.fontSize = 16f;
        txt.color = Color.white;
        labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 50f);

        // Action Button
        GameObject btnObj = CreateSimpleButton("EditButton", itemObj.transform, "✎ Edit", activeAccentColor, new Vector2(100f, 50f));
        btnObj.GetComponent<Button>().onClick.AddListener(() => SelectTriggerForEditing(candidate));
    }

    private void SelectTriggerForEditing(PlacedEditorObject candidate)
    {
        activeEditingTrigger = candidate;
        activeTriggerScript = candidate.GetComponent<CollisionsAndTriggers>();

        if (activeTriggerScript == null)
        {
            activeTriggerScript = candidate.gameObject.AddComponent<CollisionsAndTriggers>();
            var col = candidate.GetComponent<Collider2D>();
            if (col == null)
            {
                var newCol = candidate.gameObject.AddComponent<BoxCollider2D>();
                newCol.isTrigger = true;
            }
        }

        // Prefill rename input field
        if (searchInputField != null)
        {
            searchInputField.SetTextWithoutNotify(activeEditingTrigger.gameObject.name);
            searchInputField.interactable = true;
        }

        RefreshPropertiesPanel();
    }

    private void ShowSelectPrompt()
    {
        ClearPropertiesPanel();

        if (searchInputField != null)
        {
            searchInputField.SetTextWithoutNotify("");
            searchInputField.interactable = false;
        }

        GameObject promptObj = new GameObject("Prompt", typeof(RectTransform), typeof(CanvasRenderer));
        promptObj.transform.SetParent(propertiesContent, false);
        promptObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 80f);

        TMP_Text txt = promptObj.AddComponent<TextMeshProUGUI>();
        txt.text = "Select an existing Trigger from the list or spawn a new object to configure.";
        txt.fontSize = 18f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    private void ClearPropertiesPanel()
    {
        foreach (Transform child in propertiesContent)
        {
            Destroy(child.gameObject);
        }
    }

    private void RefreshPropertiesPanel()
    {
        ClearPropertiesPanel();
        if (activeEditingTrigger == null || activeTriggerScript == null) return;

        // Container title
        CreateLabelField($"Editing Wiring: {activeEditingTrigger.name}", 20f, Color.white);

        // 1. Objects that activate trap
        CreateSectionHeader("1. Objects that Activate Trap");
        CreateNoteField("Choose whether this trap runs automatically at startup, or triggers when specified objects collide with each other in-game.");

        CreateToggleField("Activate Trap on Start (No collision required)", activeTriggerScript.activateOnStart, (val) =>
        {
            activeTriggerScript.activateOnStart = val;
            RefreshPropertiesPanel(); // Rebuild panel to show/hide collision checklist dynamically!
        });

        if (!activeTriggerScript.activateOnStart)
        {
            CreateNoteField("Select two or more objects in the scene below. The trap will trigger when any of these selected objects collide with each other in-game.");
            CreateActivationObjectsChecklist();
        }

        // 2. Select trap object
        CreateSectionHeader("2. Select Trap Object");
        CreateNoteField("Select the main GameObject whose physics settings (like gravity) will be modified by this trap.");
        CreatePhysicsTargetSelector();

        // 3. Select trap type
        CreateSectionHeader("3. Select Trap Type");
        CreateNoteField("Select the behavior type of the trap (e.g. Teleportation, Continuous Motion, or Physics tweaks).");

        List<string> triggerOptions = new List<string> { "None", "Teleport", "Continuous Motion", "Single Motion", "Physics Modifier", "Jump Modifier" };
        int currentTypeIndex = 0;
        switch (activeTriggerScript.triggerType)
        {
            case TriggerType.None: currentTypeIndex = 0; break;
            case TriggerType.Teleport: currentTypeIndex = 1; break;
            case TriggerType.ContinousMotion: currentTypeIndex = 2; break;
            case TriggerType.SingleMotion: currentTypeIndex = 3; break;
            case TriggerType.PhysicsModifier: currentTypeIndex = 4; break;
            case TriggerType.JumpModifier: currentTypeIndex = 5; break;
            default: currentTypeIndex = 0; break;
        }

        CreateRadioGroupField("Trap Action Type", triggerOptions, currentTypeIndex, (index) =>
        {
            TriggerType selectedType = TriggerType.None;
            switch (index)
            {
                case 0: selectedType = TriggerType.None; break;
                case 1: selectedType = TriggerType.Teleport; break;
                case 2: selectedType = TriggerType.ContinousMotion; break;
                case 3: selectedType = TriggerType.SingleMotion; break;
                case 4: selectedType = TriggerType.PhysicsModifier; break;
                case 5: selectedType = TriggerType.JumpModifier; break;
            }
            activeTriggerScript.triggerType = selectedType;
            RefreshPropertiesPanel(); // Rebuild parameters dynamically
        });

        // 4. Options concerning trap type
        if (activeTriggerScript.triggerType != TriggerType.None)
        {
            CreateSectionHeader("4. Options Concerning Trap Type");

            switch (activeTriggerScript.triggerType)
            {
                case TriggerType.Teleport:
                    CreateNoteField("Object to Move (Select which object will be teleported):");
                    CreateObjectsToTriggerChecklist();

                    CreateNoteField("Target Position Object (Select the destination marker object):");
                    CreateDestinationTargetSelector();

                    if (activeTriggerScript.destinationTargetObject != null)
                    {
                        CreateToggleField("Teleport X Coordinate", activeTriggerScript.useTargetX, (val) =>
                        {
                            activeTriggerScript.useTargetX = val;
                        });
                        CreateToggleField("Teleport Y Coordinate", activeTriggerScript.useTargetY, (val) =>
                        {
                            activeTriggerScript.useTargetY = val;
                        });
                    }
                    break;

                case TriggerType.SingleMotion:
                    CreateNoteField("Object to Move (Select which object will move to the target position):");
                    CreateObjectsToTriggerChecklist();

                    CreateNoteField("Target Position Object (Select the destination marker object):");
                    CreateDestinationTargetSelector();

                    if (activeTriggerScript.destinationTargetObject != null)
                    {
                        CreateToggleField("Match X Coordinate", activeTriggerScript.useTargetX, (val) =>
                        {
                            activeTriggerScript.useTargetX = val;
                        });
                        CreateToggleField("Match Y Coordinate", activeTriggerScript.useTargetY, (val) =>
                        {
                            activeTriggerScript.useTargetY = val;
                        });
                    }
                    CreateFloatField("Movement Speed", activeTriggerScript.targetMoveSpeed, (val) =>
                    {
                        activeTriggerScript.targetMoveSpeed = val;
                    });
                    break;

                case TriggerType.ContinousMotion:
                    List<string> dirs = new List<string> { "Up", "Down", "Left", "Right" };
                    CreateRadioGroupField("Move Direction", dirs, (int)activeTriggerScript.moveDirection, (index) =>
                    {
                        activeTriggerScript.moveDirection = (MoveDirection)index;
                    });
                    CreateFloatField("Move Speed", activeTriggerScript.moveSpeed, (val) =>
                    {
                        activeTriggerScript.moveSpeed = val;
                    });
                    CreateToggleField("Enable Continuous Movement Immediately", activeTriggerScript.enableMove, (val) =>
                    {
                        activeTriggerScript.enableMove = val;
                    });
                    break;

                case TriggerType.PhysicsModifier:
                    CreateFloatField("New Gravity Scale", activeTriggerScript.newGravityScale, (val) =>
                    {
                        activeTriggerScript.newGravityScale = val;
                    });
                    CreateFloatField("Fall Speed Multiplier", activeTriggerScript.fallSpeedMultiplier, (val) =>
                    {
                        activeTriggerScript.fallSpeedMultiplier = val;
                    });
                    break;

                case TriggerType.JumpModifier:
                    CreateIntField("Max Jumps Allowed", activeTriggerScript.newMaxJumpsValue, (val) =>
                    {
                        activeTriggerScript.newMaxJumpsValue = val;
                    });
                    break;
            }
        }

        // Extra trigger properties (One-shot toggle)
        CreateSectionHeader("Additional Trigger Options");
        CreateToggleField("Delete Trigger After One Use (One-Shot)", activeTriggerScript.deleteTriggerZone, (val) =>
        {
            activeTriggerScript.deleteTriggerZone = val;
        });

        // Add spacer before Remove button
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(propertiesContent, false);
        spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);

        // 5. Remove Trigger Component completely
        GameObject deleteBtn = CreateSimpleButton("RemoveTriggerButton", propertiesContent, "Remove Wiring Component", dangerColor, new Vector2(0f, 40f));
        deleteBtn.GetComponent<Button>().onClick.AddListener(RemoveWiringComponent);
    }

    private void RemoveWiringComponent()
    {
        if (activeEditingTrigger != null)
        {
            var triggerComp = activeEditingTrigger.GetComponent<CollisionsAndTriggers>();
            if (triggerComp != null)
            {
                Destroy(triggerComp);
            }
            activeEditingTrigger = null;
            activeTriggerScript = null;
            RefreshCandidateList();
            ShowSelectPrompt();
        }
    }

    // ── Helper UI builders ──────────────────────────────────────────────────

    private void CreateLabelField(string text, float size, Color col)
    {
        GameObject labelObj = new GameObject("TitleLabel", typeof(RectTransform), typeof(CanvasRenderer));
        labelObj.transform.SetParent(propertiesContent, false);
        labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 60f);

        TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = size;
        txt.alignment = TextAlignmentOptions.Left;
        txt.color = col;
    }

    private void CreateToggleField(string labelText, bool defaultValue, Action<bool> onValueChanged)
    {
        GameObject container = new GameObject("ToggleField", typeof(RectTransform));
        container.transform.SetParent(propertiesContent, false);
        container.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 60f);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        GameObject toggleObj = new GameObject("Toggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Toggle));
        toggleObj.transform.SetParent(container.transform, false);
        toggleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);

        // Add a visual background checkmark box
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(toggleObj.transform, false);
        bgObj.GetComponent<Image>().color = buttonColor;
        bgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 34f);

        GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkObj.transform.SetParent(bgObj.transform, false);
        checkObj.GetComponent<Image>().color = activeAccentColor;
        checkObj.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 20f);

        Toggle toggle = toggleObj.GetComponent<Toggle>();
        toggle.isOn = defaultValue;
        toggle.targetGraphic = bgObj.GetComponent<Image>();
        toggle.graphic = checkObj.GetComponent<Image>();

        toggle.onValueChanged.AddListener((val) => onValueChanged?.Invoke(val));

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObj.transform.SetParent(container.transform, false);
        TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
        txt.text = labelText;
        txt.fontSize = 16f;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 40f);
    }

    private void CreateRadioGroupField(string labelText, List<string> options, int currentSelectedIndex, Action<int> onValueChanged)
    {
        // Create a vertical container to stack options
        GameObject groupContainer = new GameObject("RadioGroup_" + labelText, typeof(RectTransform));
        groupContainer.transform.SetParent(propertiesContent, false);

        VerticalLayoutGroup vLayout = groupContainer.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 8f;
        vLayout.childControlHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childForceExpandWidth = true;

        CreateLabelField(labelText, 16f, activeAccentColor);

        List<Toggle> toggles = new List<Toggle>();

        for (int i = 0; i < options.Count; i++)
        {
            string optionName = options[i];
            int index = i;

            GameObject container = new GameObject($"RadioOption_{index}", typeof(RectTransform));
            container.transform.SetParent(groupContainer.transform, false);
            container.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 45f);

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            GameObject toggleObj = new GameObject("Toggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Toggle));
            toggleObj.transform.SetParent(container.transform, false);
            toggleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 30f);

            // Circular/box background
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgObj.transform.SetParent(toggleObj.transform, false);
            bgObj.GetComponent<Image>().color = buttonColor;
            bgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 24f);

            GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkObj.transform.SetParent(bgObj.transform, false);
            checkObj.GetComponent<Image>().color = activeAccentColor;
            checkObj.GetComponent<RectTransform>().sizeDelta = new Vector2(14f, 14f);

            Toggle toggle = toggleObj.GetComponent<Toggle>();
            toggle.isOn = (index == currentSelectedIndex);
            toggle.targetGraphic = bgObj.GetComponent<Image>();
            toggle.graphic = checkObj.GetComponent<Image>();
            toggles.Add(toggle);

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            labelObj.transform.SetParent(container.transform, false);
            TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
            txt.text = optionName;
            txt.fontSize = 15f;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 40f);

            toggle.onValueChanged.AddListener((val) =>
            {
                if (val)
                {
                    // Uncheck other toggles in the group
                    for (int j = 0; j < toggles.Count; j++)
                    {
                        if (j != index)
                        {
                            toggles[j].SetIsOnWithoutNotify(false);
                        }
                    }
                    onValueChanged?.Invoke(index);
                }
                else
                {
                    // Prevent completely unchecking all options (keep one checked)
                    bool noneChecked = true;
                    foreach (var t in toggles)
                    {
                        if (t.isOn) noneChecked = false;
                    }
                    if (noneChecked)
                    {
                        toggle.SetIsOnWithoutNotify(true);
                    }
                }
            });
        }

        ContentSizeFitter fitter = groupContainer.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void CreateFloatField(string labelText, float defaultValue, Action<float> onValueChanged)
    {
        GameObject container = new GameObject("FloatField", typeof(RectTransform));
        container.transform.SetParent(propertiesContent, false);
        container.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 70f);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObj.transform.SetParent(container.transform, false);
        TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
        txt.text = labelText;
        txt.fontSize = 16f;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 40f);

        GameObject inputObj = new GameObject("Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        inputObj.transform.SetParent(container.transform, false);
        inputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 45f);
        inputObj.GetComponent<Image>().color = buttonColor;

        // Viewport (TextArea) for the input text to render and receive typing clicks correctly
        GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform rtArea = textArea.GetComponent<RectTransform>();
        rtArea.anchorMin = Vector2.zero;
        rtArea.anchorMax = Vector2.one;
        rtArea.sizeDelta = new Vector2(-16f, -10f); // 8px left/right padding, 5px top/bottom padding

        // Text Component where typing value is displayed
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform rtText = textObj.GetComponent<RectTransform>();
        rtText.anchorMin = Vector2.zero;
        rtText.anchorMax = Vector2.one;
        rtText.sizeDelta = Vector2.zero;

        TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = 16f;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_InputField input = inputObj.GetComponent<TMP_InputField>();
        input.textViewport = rtArea;
        input.textComponent = tmpText;
        input.text = defaultValue.ToString();
        input.characterValidation = TMP_InputField.CharacterValidation.Decimal;

        // Setup caret styling to make the typing cursor show up
        input.caretWidth = 2;
        input.customCaretColor = true;
        input.caretColor = Color.white;
        input.fontAsset = tmpText.font;
        input.selectionColor = new Color(0.2f, 0.44f, 1f, 0.5f);

        input.onEndEdit.AddListener((val) =>
        {
            if (float.TryParse(val, out float res))
            {
                onValueChanged?.Invoke(res);
            }
        });
    }

    private void CreateIntField(string labelText, int defaultValue, Action<int> onValueChanged)
    {
        GameObject container = new GameObject("IntField", typeof(RectTransform));
        container.transform.SetParent(propertiesContent, false);
        container.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 70f);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObj.transform.SetParent(container.transform, false);
        TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
        txt.text = labelText;
        txt.fontSize = 16f;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 40f);

        GameObject inputObj = new GameObject("Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        inputObj.transform.SetParent(container.transform, false);
        inputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 45f);
        inputObj.GetComponent<Image>().color = buttonColor;

        // Viewport (TextArea) for the input text to render and receive typing clicks correctly
        GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform rtArea = textArea.GetComponent<RectTransform>();
        rtArea.anchorMin = Vector2.zero;
        rtArea.anchorMax = Vector2.one;
        rtArea.sizeDelta = new Vector2(-16f, -10f);

        // Text Component where typing value is displayed
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform rtText = textObj.GetComponent<RectTransform>();
        rtText.anchorMin = Vector2.zero;
        rtText.anchorMax = Vector2.one;
        rtText.sizeDelta = Vector2.zero;

        TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = 16f;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_InputField input = inputObj.GetComponent<TMP_InputField>();
        input.textViewport = rtArea;
        input.textComponent = tmpText;
        input.text = defaultValue.ToString();
        input.characterValidation = TMP_InputField.CharacterValidation.Integer;

        // Setup caret styling to make the typing cursor show up
        input.caretWidth = 2;
        input.customCaretColor = true;
        input.caretColor = Color.white;
        input.fontAsset = tmpText.font;
        input.selectionColor = new Color(0.2f, 0.44f, 1f, 0.5f);

        input.onEndEdit.AddListener((val) =>
        {
            if (int.TryParse(val, out int res))
            {
                onValueChanged?.Invoke(res);
            }
        });
    }

    private void CreateVector2Field(string labelText, Vector2 defaultValue, Action<Vector2> onValueChanged)
    {
        GameObject container = new GameObject("Vector2Field", typeof(RectTransform));
        container.transform.SetParent(propertiesContent, false);
        container.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 80f);

        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        layout.childControlHeight = false;

        CreateLabelField(labelText, 16f, Color.white);

        GameObject row = new GameObject("CoordsRow", typeof(RectTransform));
        row.transform.SetParent(container.transform, false);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 35f);

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 15f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;

        // X Input
        CreateFloatField("X Offset", defaultValue.x, (val) =>
        {
            Vector2 curr = (activeTriggerScript != null) ? activeTriggerScript.teleportPosition : Vector2.zero;
            if (activeTriggerScript != null && activeTriggerScript.triggerType == TriggerType.SingleMotion) curr = activeTriggerScript.targetPosition;
            onValueChanged?.Invoke(new Vector2(val, curr.y));
        });

        // Y Input
        CreateFloatField("Y Offset", defaultValue.y, (val) =>
        {
            Vector2 curr = (activeTriggerScript != null) ? activeTriggerScript.teleportPosition : Vector2.zero;
            if (activeTriggerScript != null && activeTriggerScript.triggerType == TriggerType.SingleMotion) curr = activeTriggerScript.targetPosition;
            onValueChanged?.Invoke(new Vector2(curr.x, val));
        });
    }

    private void CreateDestinationTargetSelector()
    {
        HashSet<GameObject> currentSelections = new HashSet<GameObject>();
        if (activeTriggerScript.destinationTargetObject != null)
        {
            currentSelections.Add(activeTriggerScript.destinationTargetObject);
        }

        CreateScrollableObjectSelector(
            null,
            "Select target destination object marker:",
            currentSelections,
            (itemGo, selected) =>
            {
                if (selected)
                {
                    activeTriggerScript.destinationTargetObject = itemGo;
                }
                else
                {
                    activeTriggerScript.destinationTargetObject = null;
                }
                RefreshPropertiesPanel(); // Rebuild panel to show/hide X and Y toggles dynamically!
            },
            false // Single-select!
        );
    }

    private void CreatePhysicsTargetSelector()
    {
        HashSet<GameObject> currentSelections = new HashSet<GameObject>();
        if (activeTriggerScript.objectToModify != null)
        {
            currentSelections.Add(activeTriggerScript.objectToModify);
        }

        CreateScrollableObjectSelector(
            null,
            "Choose GameObject to apply physics modifiers on:",
            currentSelections,
            (itemGo, selected) =>
            {
                if (selected)
                {
                    activeTriggerScript.objectToModify = itemGo;
                }
                else
                {
                    activeTriggerScript.objectToModify = null;
                }
            },
            false // Single-select!
        );
    }

    private void CreateSectionHeader(string titleText)
    {
        // Add a small spacer beforehand
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(propertiesContent, false);
        spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 15f);

        CreateLabelField(titleText, 18f, activeAccentColor);
    }

    private void CreateNoteField(string noteText)
    {
        GameObject noteObj = new GameObject("NoteField", typeof(RectTransform), typeof(CanvasRenderer));
        noteObj.transform.SetParent(propertiesContent, false);

        var rect = noteObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 35f);

        TMP_Text txt = noteObj.AddComponent<TextMeshProUGUI>();
        txt.text = noteText;
        txt.fontSize = 13f;
        txt.fontStyle = FontStyles.Italic;
        txt.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Muted gray
        txt.alignment = TextAlignmentOptions.TopLeft;
        txt.enableWordWrapping = true;

        ContentSizeFitter fitter = noteObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void CreateActivationObjectsChecklist()
    {
        HashSet<GameObject> currentSelections = new HashSet<GameObject>();
        if (activeTriggerScript.activationObjects != null && activeTriggerScript.activationObjects.Length > 0)
        {
            var firstObj = activeTriggerScript.activationObjects[0];
            if (firstObj != null) currentSelections.Add(firstObj);
        }

        CreateScrollableObjectSelector(
            null,
            "Choose a single object that triggers this trap when entering the zone:",
            currentSelections,
            (itemGo, selected) =>
            {
                if (selected)
                {
                    activeTriggerScript.activationObjects = new GameObject[] { itemGo };
                }
                else
                {
                    activeTriggerScript.activationObjects = new GameObject[0];
                }
                Debug.Log($"[Mechanics] Updated activation object: {(selected ? itemGo.name : "None")}");
            },
            false // Single-select!
        );
    }

    private void CreateObjectsToTriggerChecklist()
    {
        HashSet<GameObject> currentSelections = new HashSet<GameObject>();
        if (activeTriggerScript.objectsToTrigger != null)
        {
            foreach (var t in activeTriggerScript.objectsToTrigger)
            {
                if (t != null) currentSelections.Add(t);
            }
        }

        CreateScrollableObjectSelector(
            null,
            "Select object(s) to move/teleport:",
            currentSelections,
            (itemGo, selected) =>
            {
                if (selected)
                {
                    currentSelections.Add(itemGo);
                }
                else
                {
                    currentSelections.Remove(itemGo);
                }

                GameObject[] arr = new GameObject[currentSelections.Count];
                currentSelections.CopyTo(arr);
                activeTriggerScript.objectsToTrigger = arr;
                Debug.Log($"[Mechanics] Updated objects to move checklist: {arr.Length} items.");
            },
            true // Multi-select!
        );
    }

    private void CreateScrollableObjectSelector(string labelText, string noteText, HashSet<GameObject> currentSelections, Action<GameObject, bool> onToggleChanged, bool isMultiSelect)
    {
        if (!string.IsNullOrEmpty(labelText))
        {
            CreateLabelField(labelText, 16f, activeAccentColor);
        }
        if (!string.IsNullOrEmpty(noteText))
        {
            CreateNoteField(noteText);
        }

        // Create a fixed-height nested Scroll View
        GameObject scrollObj = new GameObject("ScrollableSelector", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(propertiesContent, false);
        scrollObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 150f); // Fixed height!
        scrollObj.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.24f, 1f);

        // Viewport
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform rtView = viewportObj.GetComponent<RectTransform>();
        rtView.anchorMin = Vector2.zero;
        rtView.anchorMax = Vector2.one;
        rtView.sizeDelta = Vector2.zero;

        // Content
        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform rtContent = contentObj.GetComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0f, 1f);
        rtContent.anchorMax = new Vector2(1f, 1f);
        rtContent.pivot = new Vector2(0.5f, 1f);
        rtContent.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.viewport = rtView;
        scrollRect.content = rtContent;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 25f;

        List<Toggle> toggles = new List<Toggle>();

        foreach (var candidate in allSelectableObjects)
        {
            string itemName = candidate.name;
            GameObject itemGo = candidate.gameObject;

            bool isChecked = currentSelections.Contains(itemGo);

            GameObject itemContainer = new GameObject("OptionRow", typeof(RectTransform));
            itemContainer.transform.SetParent(contentObj.transform, false);
            itemContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 40f);

            HorizontalLayoutGroup rowLayout = itemContainer.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 15f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;

            GameObject toggleObj = new GameObject("Toggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Toggle));
            toggleObj.transform.SetParent(itemContainer.transform, false);
            toggleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 30f);

            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgObj.transform.SetParent(toggleObj.transform, false);
            bgObj.GetComponent<Image>().color = buttonColor;
            bgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 24f);

            GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkObj.transform.SetParent(bgObj.transform, false);
            checkObj.GetComponent<Image>().color = activeAccentColor;
            checkObj.GetComponent<RectTransform>().sizeDelta = new Vector2(14f, 14f);

            Toggle toggle = toggleObj.GetComponent<Toggle>();
            toggle.isOn = isChecked;
            toggle.targetGraphic = bgObj.GetComponent<Image>();
            toggle.graphic = checkObj.GetComponent<Image>();
            toggles.Add(toggle);

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            labelObj.transform.SetParent(itemContainer.transform, false);
            TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
            txt.text = itemName;
            txt.fontSize = 15f;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 40f);

            int toggleIndex = toggles.Count - 1;
            toggle.onValueChanged.AddListener((val) =>
            {
                if (!isMultiSelect)
                {
                    if (val)
                    {
                        for (int j = 0; j < toggles.Count; j++)
                        {
                            if (j != toggleIndex)
                            {
                                toggles[j].SetIsOnWithoutNotify(false);
                            }
                        }
                        onToggleChanged?.Invoke(itemGo, true);
                    }
                    else
                    {
                        onToggleChanged?.Invoke(itemGo, false);
                    }
                }
                else
                {
                    onToggleChanged?.Invoke(itemGo, val);
                }
            });
        }
    }

    private GameObject CreateSimpleButton(string name, Transform parent, string label, Color color, Vector2 size)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        if (size == Vector2.zero)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 50f);
        }
        else
        {
            rt.sizeDelta = size;
        }

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
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        return btnObj;
    }

    private void CreateCreateTrapButton()
    {
        GameObject itemObj = new GameObject("CreateTrapItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        itemObj.transform.SetParent(listContent, false);
        itemObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 60f);

        Image img = itemObj.GetComponent<Image>();
        img.color = new Color(0.12f, 0.28f, 0.2f, 1f); // subtle slate green

        HorizontalLayoutGroup layout = itemObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 5, 5);
        layout.spacing = 15f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        GameObject btnObj = CreateSimpleButton("CreateButton", itemObj.transform, "+ Create New Trap", new Color(0.18f, 0.65f, 0.35f, 1f), Vector2.zero);
        btnObj.GetComponent<Button>().onClick.AddListener(CreateNewTrap);
    }

    private void CreateNewTrap()
    {
        if (GridPainter.Instance == null) return;

        // Bypass name prompt overlay for programmatic creation
        GridPainter.suppressNamePromptOnce = true;

        // Determine name: "Trap X"
        int trapNumber = 1;
        while (true)
        {
            string candidateName = "Trap " + trapNumber;
            bool nameExists = GridPainter.Instance.GetPlacedObjects().Exists(o => o != null && o.gameObject.name == candidateName);
            if (!nameExists)
            {
                break;
            }
            trapNumber++;
        }

        string trapName = "Trap " + trapNumber;

        // Spawn a TriggerZone at the center of screen
        GridPainter.Instance.SpawnAssetAtCenter("TriggerZone");

        // The newly spawned object is automatically the selected object
        PlacedEditorObject newObj = GridPainter.Instance.GetSelectedObject();
        if (newObj != null)
        {
            newObj.gameObject.name = trapName;
            newObj.customToolDisplayName = trapName;

            // Add the CollisionsAndTriggers component so it becomes a trap
            var triggerComp = newObj.GetComponent<CollisionsAndTriggers>();
            if (triggerComp == null)
            {
                triggerComp = newObj.gameObject.AddComponent<CollisionsAndTriggers>();
            }

            // Select it for editing immediately!
            SelectTriggerForEditing(newObj);
        }

        // Refresh list
        RefreshCandidateList();
    }
}
