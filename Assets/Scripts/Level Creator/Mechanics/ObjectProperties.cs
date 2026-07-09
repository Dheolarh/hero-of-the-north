using UnityEngine;
using UnityEngine.UI;

public class ObjectProperties : MonoBehaviour
{
    [Header("Solidity Settings")]
    [SerializeField] private Toggle changeSolidityToggle;
    [SerializeField] private Toggle solidToggle;
    [SerializeField] private Toggle passThroughToggle;

    [Header("Gravity Settings")]
    [SerializeField] private Toggle changeGravityToggle;
    [SerializeField] private Toggle fallsDownToggle;
    [SerializeField] private Toggle floatsToggle;

    [Header("Visibility Settings")]
    [SerializeField] private Toggle appearOnTriggerToggle;

    public void Setup(CollisionsAndTriggers trigger)
    {
        if (trigger == null) return;

        // --- Solidity Setup ---
        if (changeSolidityToggle != null)
        {
            changeSolidityToggle.onValueChanged.RemoveAllListeners();
            changeSolidityToggle.isOn = trigger.modifyColliderState;
            
            // Helper to update interactable states
            System.Action<bool> updateSolidityInteractable = (active) =>
            {
                if (solidToggle != null) solidToggle.interactable = active;
                if (passThroughToggle != null) passThroughToggle.interactable = active;
            };

            updateSolidityInteractable(trigger.modifyColliderState);

            changeSolidityToggle.onValueChanged.AddListener((val) =>
            {
                trigger.modifyColliderState = val;
                updateSolidityInteractable(val);
                UpdateTriggerType(trigger);
            });
        }

        if (solidToggle != null)
        {
            solidToggle.onValueChanged.RemoveAllListeners();
            solidToggle.isOn = trigger.makeSolid;
            solidToggle.onValueChanged.AddListener((val) =>
            {
                if (val)
                {
                    trigger.makeSolid = true;
                    if (passThroughToggle != null)
                    {
                        passThroughToggle.SetIsOnWithoutNotify(false);
                    }
                }
                else
                {
                    if (passThroughToggle != null && !passThroughToggle.isOn)
                    {
                        solidToggle.SetIsOnWithoutNotify(true); // Don't allow unchecking both
                    }
                }
            });
        }

        if (passThroughToggle != null)
        {
            passThroughToggle.onValueChanged.RemoveAllListeners();
            passThroughToggle.isOn = !trigger.makeSolid;
            passThroughToggle.onValueChanged.AddListener((val) =>
            {
                if (val)
                {
                    trigger.makeSolid = false;
                    if (solidToggle != null)
                    {
                        solidToggle.SetIsOnWithoutNotify(false);
                    }
                }
                else
                {
                    if (solidToggle != null && !solidToggle.isOn)
                    {
                        passThroughToggle.SetIsOnWithoutNotify(true); // Don't allow unchecking both
                    }
                }
            });
        }

        // --- Gravity Setup ---
        if (changeGravityToggle != null)
        {
            changeGravityToggle.onValueChanged.RemoveAllListeners();
            changeGravityToggle.isOn = trigger.modifyGravityState;

            System.Action<bool> updateGravityInteractable = (active) =>
            {
                if (fallsDownToggle != null) fallsDownToggle.interactable = active;
                if (floatsToggle != null) floatsToggle.interactable = active;
            };

            updateGravityInteractable(trigger.modifyGravityState);

            changeGravityToggle.onValueChanged.AddListener((val) =>
            {
                trigger.modifyGravityState = val;
                updateGravityInteractable(val);
                UpdateTriggerType(trigger);
            });
        }

        if (fallsDownToggle != null)
        {
            fallsDownToggle.onValueChanged.RemoveAllListeners();
            fallsDownToggle.isOn = trigger.makeSubjectToGravity;
            fallsDownToggle.onValueChanged.AddListener((val) =>
            {
                if (val)
                {
                    trigger.makeSubjectToGravity = true;
                    if (floatsToggle != null)
                    {
                        floatsToggle.SetIsOnWithoutNotify(false);
                    }
                }
                else
                {
                    if (floatsToggle != null && !floatsToggle.isOn)
                    {
                        fallsDownToggle.SetIsOnWithoutNotify(true);
                    }
                }
            });
        }

        if (floatsToggle != null)
        {
            floatsToggle.onValueChanged.RemoveAllListeners();
            floatsToggle.isOn = !trigger.makeSubjectToGravity;
            floatsToggle.onValueChanged.AddListener((val) =>
            {
                if (val)
                {
                    trigger.makeSubjectToGravity = false;
                    if (fallsDownToggle != null)
                    {
                        fallsDownToggle.SetIsOnWithoutNotify(false);
                    }
                }
                else
                {
                    if (fallsDownToggle != null && !fallsDownToggle.isOn)
                    {
                        floatsToggle.SetIsOnWithoutNotify(true);
                    }
                }
            });
        }

        // --- Visibility Setup ---
        if (appearOnTriggerToggle != null)
        {
            appearOnTriggerToggle.onValueChanged.RemoveAllListeners();
            appearOnTriggerToggle.isOn = trigger.appearOnTrigger;
            appearOnTriggerToggle.onValueChanged.AddListener((val) =>
            {
                trigger.appearOnTrigger = val;
                UpdateTriggerType(trigger);
            });
        }

        // Ensure trigger type is correct from the start
        UpdateTriggerType(trigger);
    }

    /// <summary>
    /// Sets triggerType to PhysicsModifier if any object-property flag is active, otherwise None.
    /// </summary>
    private void UpdateTriggerType(CollisionsAndTriggers trigger)
    {
        bool anyActive = trigger.modifyColliderState || trigger.modifyGravityState || trigger.appearOnTrigger;
        if (anyActive)
        {
            trigger.triggerType = TriggerType.PhysicsModifier;
        }
        else if (trigger.triggerType == TriggerType.PhysicsModifier)
        {
            trigger.triggerType = TriggerType.None;
        }
    }
}
