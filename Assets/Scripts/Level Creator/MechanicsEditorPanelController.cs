using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MechanicsEditorPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform listContent;
    [SerializeField] private Trap trapPrefab;
    [SerializeField] private TMP_Text activeTrapNameText;
    [SerializeField] private Button createTrapButton;
    [SerializeField] private Objects objectRowPrefab;

    [Header("Exclusion Settings")]
    [SerializeField] private GameObject[] objectsToExclude;

    [Header("Style Colors")]
    private Color panelColor = new Color(0.12f, 0.15f, 0.2f, 0.95f);
    private Color buttonColor = new Color(0.2f, 0.25f, 0.32f, 1f);
    private Color activeAccentColor = new Color(0.2f, 0.7f, 1f, 1f);
    private Color dangerColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    private PlacedEditorObject activeEditingTrigger;
    private CollisionsAndTriggers activeTriggerScript;

    private List<PlacedEditorObject> allSelectableObjects = new List<PlacedEditorObject>();
    private List<PlacedEditorObject> savedTrps = new List<PlacedEditorObject>();



    [Header("Static Group Slots")]
    [SerializeField] private Transform triggerSelectorGroup;
    [SerializeField] private Transform trapTypesGroup;
    [SerializeField] private Transform teleportGroup;
    [SerializeField] private Transform singleMotionGroup;
    [SerializeField] private Transform continuousMotionGroup;
    [SerializeField] private Transform objectPropertiesGroup;
    [SerializeField] private Transform triggerDeleteGroup;

    [Header("Static Toggle Slots")]
    [SerializeField] private Toggle noneToggle;
    [SerializeField] private Toggle teleportToggle;
    [SerializeField] private Toggle continuousMotionToggle;
    [SerializeField] private Toggle singleMotionToggle;
    [SerializeField] private Toggle objPropToggle;

    [Header("UI Element Slots")]
    [SerializeField] private Transform activationObjectsScroll;

    [SerializeField] private Transform teleportObjectsScroll;
    [SerializeField] private Teleport teleportComponent;

    [SerializeField] private Transform singleMotionObjectsScroll;

    [SerializeField] private Transform continuousMotionObjectsScroll;

    [SerializeField] private Transform objectPropertiesObjectsScroll;

    private TMP_InputField singleMotionSpeedInput;
    private TMP_InputField singleMotionIntervalInput;
    private TMP_InputField continuousMotionSpeedInput;

    public void ToggleMechanicPanel()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    private void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (createTrapButton != null)
        {
            createTrapButton.onClick.RemoveAllListeners();
            createTrapButton.onClick.AddListener(CreateNewTrap);
        }

        RefreshCandidateList();
        ShowSelectPrompt();
    }

    public void RefreshWiringPanelIfActive(PlacedEditorObject obj)
    {
        if (activeEditingTrigger == obj)
        {
        }
    }
    public void RefreshCandidateList()
    {
        foreach (Transform child in listContent)
        {
            if (createTrapButton != null && (child.gameObject == createTrapButton.gameObject || createTrapButton.transform.IsChildOf(child)))
            {
                continue;
            }
            Destroy(child.gameObject);
        }

        allSelectableObjects.Clear();
        savedTrps.Clear();

        if (GridPainter.Instance == null) return;

        var allPlaced = GridPainter.Instance.GetPlacedObjects();

        foreach (var obj in allPlaced)
        {
            if (obj == null) continue;

            // Only show parent PlacedEditorObjects in the list
            Transform parentTrans = obj.transform.parent;
            bool hasPlacedParent = false;
            while (parentTrans != null)
            {
                if (parentTrans.GetComponent<PlacedEditorObject>() != null)
                {
                    hasPlacedParent = true;
                    break;
                }
                parentTrans = parentTrans.parent;
            }
            if (hasPlacedParent) continue;

            // Check if this object or any of its parents is in the objectsToExclude array
            bool shouldExclude = false;
            if (objectsToExclude != null)
            {
                foreach (var excludeObj in objectsToExclude)
                {
                    if (excludeObj != null && (obj.gameObject == excludeObj || obj.transform.IsChildOf(excludeObj.transform)))
                    {
                        shouldExclude = true;
                        break;
                    }
                }
            }
            if (shouldExclude) continue;

            // Populate Saved Traps list if this object has the trigger component
            if (obj.GetComponent<CollisionsAndTriggers>() != null)
            {
                savedTrps.Add(obj);
                continue; // Do not add traps as candidate selectable scene objects!
            }

            allSelectableObjects.Add(obj);
        }



        // Build Saved Traps list elements at the top
        foreach (var trap in savedTrps)
        {
            CreateCandidateListItem(trap);
        }
    }

    private string GetTrapLetterName(PlacedEditorObject obj)
    {
        int index = savedTrps.IndexOf(obj);
        if (index < 0) return obj.name;

        string letterName = "";
        int temp = index;
        while (temp >= 0)
        {
            letterName = (char)('A' + (temp % 26)) + letterName;
            temp = (temp / 26) - 1;
        }
        return "Trap " + letterName;
    }

    private void CreateCandidateListItem(PlacedEditorObject candidate)
    {
        if (trapPrefab == null) return;

        Trap item = Instantiate(trapPrefab, listContent, false);
        item.gameObject.SetActive(true);
        string trapDisplayName = GetTrapLetterName(candidate);

        item.Setup(
            trapDisplayName,
            candidate.gameObject.activeSelf,
            (val) => {
                candidate.gameObject.SetActive(val);
            },
            () => {
                SelectTriggerForEditing(candidate);
            },
            () => {
                DeleteTrap(candidate);
            }
        );
    }

    private void DeleteTrap(PlacedEditorObject candidate)
    {
        if (GridPainter.Instance != null)
        {
            GridPainter.Instance.DeleteObject(candidate);
        }
        else
        {
            Destroy(candidate.gameObject);
        }
        RefreshCandidateList();
        ShowSelectPrompt();
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

        string trapDisplayName = GetTrapLetterName(candidate);

        if (activeTrapNameText != null)
        {
            activeTrapNameText.text = trapDisplayName;
        }

        RefreshPropertiesPanel();
    }

    private void ShowSelectPrompt()
    {
        activeEditingTrigger = null;
        activeTriggerScript = null;
        if (activeTrapNameText != null) activeTrapNameText.text = "";

        if (teleportGroup != null) teleportGroup.gameObject.SetActive(false);
        if (singleMotionGroup != null) singleMotionGroup.gameObject.SetActive(false);
        if (continuousMotionGroup != null) continuousMotionGroup.gameObject.SetActive(false);
        if (objectPropertiesGroup != null) objectPropertiesGroup.gameObject.SetActive(false);
        if (triggerDeleteGroup != null) triggerDeleteGroup.gameObject.SetActive(false);
        if (triggerSelectorGroup != null) triggerSelectorGroup.gameObject.SetActive(false);

        BindStaticUI();
    }

    private void RefreshPropertiesPanel()
    {
        BindStaticUI();
    }



    private void CreateNewTrap()
    {
        if (GridPainter.Instance == null) return;

        // Bypass name prompt overlay for programmatic creation
        GridPainter.suppressNamePromptOnce = true;

        // Spawn a TriggerZone at the center of screen
        GridPainter.Instance.SpawnAssetAtCenter("TriggerZone");

        // The newly spawned object is automatically the selected object
        PlacedEditorObject newObj = GridPainter.Instance.GetSelectedObject();
        if (newObj != null)
        {
            // Add the CollisionsAndTriggers component so it becomes a trap
            var triggerComp = newObj.GetComponent<CollisionsAndTriggers>();
            if (triggerComp == null)
            {
                triggerComp = newObj.gameObject.AddComponent<CollisionsAndTriggers>();
            }

            // Temporarily add to savedTrps to determine alphabetical name
            if (!savedTrps.Contains(newObj)) savedTrps.Add(newObj);
            string trapName = GetTrapLetterName(newObj);
            newObj.gameObject.name = trapName;
            newObj.customToolDisplayName = trapName;

            // Select it for editing immediately!
            SelectTriggerForEditing(newObj);
        }

        // Refresh list
        RefreshCandidateList();
    }



    private void PopulateStaticScrollChecklist(Transform scrollTrans, HashSet<GameObject> currentSelections, Action<GameObject, bool> onToggleChanged, bool isMultiSelect)
    {
        if (scrollTrans == null) return;

        Transform contentTrans = null;

        // Support direct assignment of "Content" RectTransform or standard ScrollRect root
        if (scrollTrans.name == "Content" || scrollTrans.GetComponent<LayoutGroup>() != null || scrollTrans.GetComponent<ContentSizeFitter>() != null)
        {
            contentTrans = scrollTrans;
        }
        else
        {
            contentTrans = scrollTrans.Find("Viewport/Content");
            if (contentTrans == null)
            {
                ScrollRect sr = scrollTrans.GetComponentInChildren<ScrollRect>(true);
                if (sr != null) contentTrans = sr.content;
            }
            if (contentTrans == null)
            {
                contentTrans = scrollTrans;
            }
        }

        if (contentTrans == null) return;

        foreach (Transform child in contentTrans)
        {
            Destroy(child.gameObject);
        }


        foreach (var candidate in allSelectableObjects)
        {
            if (candidate == null) continue;
            string itemName = candidate.name;
            GameObject itemGo = candidate.gameObject;
            bool isChecked = currentSelections.Contains(itemGo);

            if (objectRowPrefab != null)
            {
                Objects item = Instantiate(objectRowPrefab, contentTrans, false);
                item.gameObject.SetActive(true);
                item.Setup(itemName, isChecked, (val) =>
                {
                    if (!isMultiSelect)
                    {
                        if (val)
                        {
                            foreach (Transform child in contentTrans)
                            {
                                Objects childItem = child.GetComponent<Objects>();
                                if (childItem != null && childItem != item && childItem.Toggle != null)
                                {
                                    childItem.Toggle.SetIsOnWithoutNotify(false);
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
            else
            {
                GameObject itemContainer = new GameObject("OptionRow", typeof(RectTransform));
                itemContainer.transform.SetParent(contentTrans, false);
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
                GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
                labelObj.transform.SetParent(itemContainer.transform, false);
                TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
                txt.text = itemName;
                txt.fontSize = 15f;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.MidlineLeft;
                labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 40f);

                toggle.onValueChanged.AddListener((val) =>
                {
                    if (!isMultiSelect)
                    {
                        if (val)
                        {
                            foreach (Transform child in contentTrans)
                            {
                                Toggle childToggle = child.GetComponentInChildren<Toggle>(true);
                                if (childToggle != null && childToggle != toggle)
                                {
                                    childToggle.SetIsOnWithoutNotify(false);
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
    }

    private void UpdateObjectsToTrigger(GameObject itemGo, bool selected)
    {
        Debug.Log($"[UpdateObjectsToTrigger] itemGo: {(itemGo != null ? itemGo.name : "null")}, selected: {selected}");
        if (activeTriggerScript == null) return;
        HashSet<GameObject> currentSelections = new HashSet<GameObject>();
        if (activeTriggerScript.objectsToTrigger != null)
        {
            foreach (var t in activeTriggerScript.objectsToTrigger)
            {
                if (t != null) currentSelections.Add(t);
            }
        }

        if (selected) currentSelections.Add(itemGo);
        else currentSelections.Remove(itemGo);

        GameObject[] arr = new GameObject[currentSelections.Count];
        currentSelections.CopyTo(arr);
        activeTriggerScript.objectsToTrigger = arr;
    }    private void BindStaticUI()
    {
        if (activeTriggerScript == null)
        {
            if (trapTypesGroup != null) trapTypesGroup.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (trapTypesGroup != null) trapTypesGroup.gameObject.SetActive(true);
        }

        // 1. Activation Trigger Selector
        if (triggerSelectorGroup != null && triggerSelectorGroup.gameObject.activeSelf)
        {
            if (activationObjectsScroll != null)
            {
                bool showChecklist = !activeTriggerScript.activateOnStart;
                activationObjectsScroll.gameObject.SetActive(showChecklist);

                if (showChecklist)
                {
                    HashSet<GameObject> currentSelections = new HashSet<GameObject>();
                    if (activeTriggerScript.activationObjects != null && activeTriggerScript.activationObjects.Length > 0)
                    {
                        var firstObj = activeTriggerScript.activationObjects[0];
                        if (firstObj != null) currentSelections.Add(firstObj);
                    }
                    PopulateStaticScrollChecklist(activationObjectsScroll, currentSelections, (itemGo, selected) =>
                    {
                        if (selected) activeTriggerScript.activationObjects = new GameObject[] { itemGo };
                        else activeTriggerScript.activationObjects = new GameObject[0];
                    }, false);
                }
            }
        }

        // 2. Trap Types (Radio selection)
        Action<TriggerType, Toggle> onToggleSelected = (selectedType, toggledOn) =>
        {
            activeTriggerScript.triggerType = selectedType;

            // Turn off all other toggles without triggering listeners
            if (noneToggle != null && noneToggle != toggledOn) noneToggle.SetIsOnWithoutNotify(false);
            if (teleportToggle != null && teleportToggle != toggledOn) teleportToggle.SetIsOnWithoutNotify(false);
            if (singleMotionToggle != null && singleMotionToggle != toggledOn) singleMotionToggle.SetIsOnWithoutNotify(false);
            if (continuousMotionToggle != null && continuousMotionToggle != toggledOn) continuousMotionToggle.SetIsOnWithoutNotify(false);
            if (objPropToggle != null && objPropToggle != toggledOn) objPropToggle.SetIsOnWithoutNotify(false);

            BindStaticUI();
        };

        if (noneToggle != null)
        {
            noneToggle.onValueChanged.RemoveAllListeners();
            noneToggle.isOn = (activeTriggerScript.triggerType == TriggerType.None);
            noneToggle.onValueChanged.AddListener((val) => { if (val) onToggleSelected(TriggerType.None, noneToggle); });
        }
        if (teleportToggle != null)
        {
            teleportToggle.onValueChanged.RemoveAllListeners();
            teleportToggle.isOn = (activeTriggerScript.triggerType == TriggerType.Teleport);
            teleportToggle.onValueChanged.AddListener((val) => { if (val) onToggleSelected(TriggerType.Teleport, teleportToggle); });
        }
        if (singleMotionToggle != null)
        {
            singleMotionToggle.onValueChanged.RemoveAllListeners();
            singleMotionToggle.isOn = (activeTriggerScript.triggerType == TriggerType.SingleMotion);
            singleMotionToggle.onValueChanged.AddListener((val) => { if (val) onToggleSelected(TriggerType.SingleMotion, singleMotionToggle); });
        }
        if (continuousMotionToggle != null)
        {
            continuousMotionToggle.onValueChanged.RemoveAllListeners();
            continuousMotionToggle.isOn = (activeTriggerScript.triggerType == TriggerType.ContinousMotion);
            continuousMotionToggle.onValueChanged.AddListener((val) => { if (val) onToggleSelected(TriggerType.ContinousMotion, continuousMotionToggle); });
        }
        if (objPropToggle != null)
        {
            objPropToggle.onValueChanged.RemoveAllListeners();
            objPropToggle.isOn = (activeTriggerScript.triggerType == TriggerType.PhysicsModifier);
            objPropToggle.onValueChanged.AddListener((val) => { if (val) onToggleSelected(TriggerType.PhysicsModifier, objPropToggle); });
        }

        // 3. Show/hide trap configuration blocks
        if (teleportGroup != null) teleportGroup.gameObject.SetActive(activeTriggerScript.triggerType == TriggerType.Teleport);
        if (singleMotionGroup != null) singleMotionGroup.gameObject.SetActive(activeTriggerScript.triggerType == TriggerType.SingleMotion);
        if (continuousMotionGroup != null) continuousMotionGroup.gameObject.SetActive(activeTriggerScript.triggerType == TriggerType.ContinousMotion);
        if (objectPropertiesGroup != null) objectPropertiesGroup.gameObject.SetActive(activeTriggerScript.triggerType == TriggerType.PhysicsModifier);
        if (triggerDeleteGroup != null) triggerDeleteGroup.gameObject.SetActive(activeTriggerScript.triggerType != TriggerType.None);

        // 4. Teleport configuration details
        if (teleportGroup != null && teleportGroup.gameObject.activeSelf)
        {
            if (teleportObjectsScroll != null)
            {
                HashSet<GameObject> currentSelections = new HashSet<GameObject>();
                if (activeTriggerScript.objectsToTrigger != null)
                {
                    foreach (var t in activeTriggerScript.objectsToTrigger) if (t != null) currentSelections.Add(t);
                }
                PopulateStaticScrollChecklist(teleportObjectsScroll, currentSelections, (itemGo, selected) =>
                {
                    UpdateObjectsToTrigger(itemGo, selected);
                    if (teleportComponent != null) teleportComponent.Setup(activeTriggerScript);
                }, true);
            }



            if (teleportComponent != null)
            {
                teleportComponent.Setup(activeTriggerScript);
            }
        }

        // 5. Single Motion configuration details
        if (singleMotionGroup != null && singleMotionGroup.gameObject.activeSelf)
        {
            if (singleMotionObjectsScroll != null)
            {
                HashSet<GameObject> currentSelections = new HashSet<GameObject>();
                if (activeTriggerScript.objectsToTrigger != null)
                {
                    foreach (var t in activeTriggerScript.objectsToTrigger) if (t != null) currentSelections.Add(t);
                }
                PopulateStaticScrollChecklist(singleMotionObjectsScroll, currentSelections, (itemGo, selected) =>
                {
                    UpdateObjectsToTrigger(itemGo, selected);
                }, true);
            }



            if (singleMotionSpeedInput != null)
            {
                singleMotionSpeedInput.onEndEdit.RemoveAllListeners();
                singleMotionSpeedInput.text = activeTriggerScript.targetMoveSpeed.ToString();
                singleMotionSpeedInput.onEndEdit.AddListener((val) => { if (float.TryParse(val, out float res)) activeTriggerScript.targetMoveSpeed = res; });
            }

            if (singleMotionIntervalInput != null)
            {
                singleMotionIntervalInput.onEndEdit.RemoveAllListeners();
                singleMotionIntervalInput.text = activeTriggerScript.moveStaggerInterval.ToString();
                singleMotionIntervalInput.onEndEdit.AddListener((val) => { if (float.TryParse(val, out float res)) activeTriggerScript.moveStaggerInterval = res; });
            }
        }

        // 6. Continuous Motion configuration details
        if (continuousMotionGroup != null && continuousMotionGroup.gameObject.activeSelf)
        {
            if (continuousMotionObjectsScroll != null)
            {
                HashSet<GameObject> currentSelections = new HashSet<GameObject>();
                if (activeTriggerScript.objectsToTrigger != null)
                {
                    foreach (var t in activeTriggerScript.objectsToTrigger) if (t != null) currentSelections.Add(t);
                }
                PopulateStaticScrollChecklist(continuousMotionObjectsScroll, currentSelections, (itemGo, selected) =>
                {
                    UpdateObjectsToTrigger(itemGo, selected);
                }, true);
            }

            if (continuousMotionSpeedInput != null)
            {
                continuousMotionSpeedInput.onEndEdit.RemoveAllListeners();
                continuousMotionSpeedInput.text = activeTriggerScript.moveSpeed.ToString();
                continuousMotionSpeedInput.onEndEdit.AddListener((val) => { if (float.TryParse(val, out float res)) activeTriggerScript.moveSpeed = res; });
            }
        }

        // 7. Object Properties configuration details
        if (objectPropertiesGroup != null && objectPropertiesGroup.gameObject.activeSelf)
        {
            if (objectPropertiesObjectsScroll != null)
            {
                HashSet<GameObject> currentSelections = new HashSet<GameObject>();
                if (activeTriggerScript.objectsToTrigger != null)
                {
                    foreach (var t in activeTriggerScript.objectsToTrigger) if (t != null) currentSelections.Add(t);
                }
                PopulateStaticScrollChecklist(objectPropertiesObjectsScroll, currentSelections, (itemGo, selected) =>
                {
                    UpdateObjectsToTrigger(itemGo, selected);
                }, true);
            }
        }
    }
}
