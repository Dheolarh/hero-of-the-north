using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class MechanicsUIExposer : EditorWindow
{
    [MenuItem("Tools/Expose Mechanics UI")]
    public static void ExposeUI()
    {
        LevelCreatorUI creatorUI = FindObjectOfType<LevelCreatorUI>();
        if (creatorUI == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find LevelCreatorUI in the scene. Please open the level creator scene first.", "OK");
            return;
        }

        var rootField = typeof(LevelCreatorUI).GetField("editorUIRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        GameObject editorUIRoot = null;
        if (rootField != null)
        {
            editorUIRoot = (GameObject)rootField.GetValue(creatorUI);
        }

        if (editorUIRoot == null)
        {
            GameObject canvasObj = GameObject.Find("LevelCreatorCanvas");
            if (canvasObj != null)
            {
                Transform rootTrans = canvasObj.transform.Find("EditorUIRoot") ?? canvasObj.transform.Find("SafeArea") ?? canvasObj.transform;
                editorUIRoot = rootTrans.gameObject;
            }
        }

        if (editorUIRoot == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find editorUIRoot inside LevelCreatorUI.", "OK");
            return;
        }

        Transform existingPanel = editorUIRoot.transform.Find("MechanicsPopupPanel");
        GameObject panelObj;
        if (existingPanel != null)
        {
            panelObj = existingPanel.gameObject;
            Undo.RegisterCompleteObjectUndo(panelObj, "Expose Mechanics UI");
        }
        else
        {
            creatorUI.ToggleMechanicsPanel();
            existingPanel = editorUIRoot.transform.Find("MechanicsPopupPanel");
            if (existingPanel == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to create MechanicsPopupPanel.", "OK");
                return;
            }
            panelObj = existingPanel.gameObject;
            Undo.RegisterCreatedObjectUndo(panelObj, "Expose Mechanics UI");
        }

        panelObj.SetActive(true);

        var controller = panelObj.GetComponent<MechanicsEditorPanelController>();
        if (controller != null)
        {
            // Auto-detect and populate inspector slots if they exist under propertiesContent
            var propContentField = typeof(MechanicsEditorPanelController).GetField("propertiesContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (propContentField != null)
            {
                RectTransform propertiesContent = (RectTransform)propContentField.GetValue(controller);
                if (propertiesContent != null)
                {
                    var slotFields = new[]
                    {
                        new { FieldName = "triggerSelectorGroup", ChildName = "Trap Trigger Selector" },
                        new { FieldName = "trapTypesGroup", ChildName = "Trap Types" },
                        new { FieldName = "teleportGroup", ChildName = "Teleport" },
                        new { FieldName = "singleMotionGroup", ChildName = "Single Motion" },
                        new { FieldName = "continuousMotionGroup", ChildName = "Continous Motion" },
                        new { FieldName = "objectPropertiesGroup", ChildName = "Object Properties" },
                        new { FieldName = "triggerDeleteGroup", ChildName = "Trigger Delete" }
                    };

                    foreach (var slot in slotFields)
                    {
                        Transform foundChild = propertiesContent.Find(slot.ChildName);
                        if (foundChild == null && slot.ChildName == "Continous Motion")
                        {
                            foundChild = propertiesContent.Find("Continuous Motion");
                        }
                        if (foundChild == null && slot.ChildName == "Trigger Delete")
                        {
                            foundChild = propertiesContent.Find("Delete Trap Trigger");
                        }

                        if (foundChild != null)
                        {
                            var field = typeof(MechanicsEditorPanelController).GetField(slot.FieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                field.SetValue(controller, foundChild);
                            }
                        }
                    }

                    // Auto-detect and populate Toggle slots
                    Transform trapTypesTrans = propertiesContent.Find("Trap Types");
                    if (trapTypesTrans != null)
                    {
                        Transform typesContainer = trapTypesTrans.Find("trap types") ?? trapTypesTrans.Find("Trap Types Options") ?? trapTypesTrans;
                        if (typesContainer != null)
                        {
                            var toggleFields = new[]
                            {
                                new { FieldName = "noneToggle", ChildName = "none" },
                                new { FieldName = "teleportToggle", ChildName = "teleport" },
                                new { FieldName = "continuousMotionToggle", ChildName = "continous motion" },
                                new { FieldName = "singleMotionToggle", ChildName = "single motion" },
                                new { FieldName = "objPropToggle", ChildName = "object properties" }
                            };

                            foreach (var slot in toggleFields)
                            {
                                Transform toggleChild = typesContainer.Find(slot.ChildName);
                                if (toggleChild == null && slot.ChildName == "continous motion")
                                {
                                    toggleChild = typesContainer.Find("continuous motion");
                                }
                                if (toggleChild != null)
                                {
                                    Toggle toggleComp = toggleChild.GetComponent<Toggle>();
                                    if (toggleComp != null)
                                    {
                                        var field = typeof(MechanicsEditorPanelController).GetField(slot.FieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        if (field != null)
                                        {
                                            field.SetValue(controller, toggleComp);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Auto-detect Scroll Slots
                    var scrollFields = new[]
                    {
                        new { ParentName = "Trap Trigger Selector", ChildName = "Objects scroll", FieldName = "activationObjectsScroll" },
                        new { ParentName = "Teleport", ChildName = "Objects scroll", FieldName = "teleportObjectsScroll" },
                        new { ParentName = "Teleport", ChildName = "Destination Target scroll", FieldName = "teleportDestinationTargetScroll" },
                        new { ParentName = "Single Motion", ChildName = "Objects scroll", FieldName = "singleMotionObjectsScroll" },
                        new { ParentName = "Single Motion", ChildName = "Destination Target scroll", FieldName = "singleMotionDestinationTargetScroll" },
                        new { ParentName = "Continuous Motion", ChildName = "Objects scroll", FieldName = "continuousMotionObjectsScroll" },
                        new { ParentName = "Object Properties", ChildName = "Objects scroll", FieldName = "objectPropertiesObjectsScroll" }
                    };

                    foreach (var scroll in scrollFields)
                    {
                        Transform parentTrans = propertiesContent.Find(scroll.ParentName);
                        if (parentTrans == null && scroll.ParentName == "Continuous Motion")
                        {
                            parentTrans = propertiesContent.Find("Continous Motion");
                        }

                        if (parentTrans != null)
                        {
                            Transform scrollTrans = parentTrans.Find(scroll.ChildName);
                            if (scrollTrans == null && scroll.ChildName == "Objects scroll")
                            {
                                scrollTrans = parentTrans.Find("Objects ScrollView") ?? parentTrans.Find("Objects scroll");
                            }
                            if (scrollTrans == null && scroll.ChildName == "Destination Target scroll")
                            {
                                scrollTrans = parentTrans.Find("Destination Target ScrollView") ?? parentTrans.Find("Destination Target scroll");
                            }

                            if (scrollTrans != null)
                            {
                                var field = typeof(MechanicsEditorPanelController).GetField(scroll.FieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (field != null)
                                {
                                    field.SetValue(controller, scrollTrans);
                                }
                            }
                        }
                    }
                }
            }

            var refreshMethod = typeof(MechanicsEditorPanelController).GetMethod("RefreshPropertiesPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (refreshMethod != null)
            {
                refreshMethod.Invoke(controller, null);
            }
        }

        EditorSceneManager.MarkSceneDirty(creatorUI.gameObject.scene);

        EditorUtility.DisplayDialog("Success", "Mechanics UI has been successfully injected and exposed! You can now select 'MechanicsPopupPanel' in the hierarchy and edit its design.", "OK");
    }
}
