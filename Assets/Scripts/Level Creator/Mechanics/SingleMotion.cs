using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SingleMotion : MonoBehaviour
{
    [SerializeField] private Slider horizontalDirectionSlider;
    [SerializeField] private Slider verticalDirectionSlider;
    [SerializeField] private Slider motionSpeedSlider;
    [SerializeField] private Slider timeIntervalSlider;
    [SerializeField] private Slider rotationSpeedSlider;

    public void Setup(CollisionsAndTriggers trigger)
    {
        if (trigger == null) return;

        // 1. Get the primary moving object
        GameObject targetObj = (trigger.objectsToTrigger != null && trigger.objectsToTrigger.Length > 0)
            ? trigger.objectsToTrigger[0]
            : null;

        // 2. Setup the Speed Slider
        if (motionSpeedSlider != null)
        {
            motionSpeedSlider.minValue = 0f;
            motionSpeedSlider.maxValue = 20f;
            motionSpeedSlider.onValueChanged.RemoveAllListeners();
            motionSpeedSlider.value = trigger.targetMoveSpeed;
            motionSpeedSlider.onValueChanged.AddListener((val) =>
            {
                trigger.targetMoveSpeed = val;
            });
        }

        // 3. Setup the Interval Slider
        if (timeIntervalSlider != null)
        {
            timeIntervalSlider.minValue = 0f;
            timeIntervalSlider.maxValue = 5f;
            timeIntervalSlider.onValueChanged.RemoveAllListeners();
            timeIntervalSlider.value = trigger.moveStaggerInterval;
            timeIntervalSlider.onValueChanged.AddListener((val) =>
            {
                trigger.moveStaggerInterval = val;
            });
        }

        // 3b. Setup Rotation Speed Slider
        if (rotationSpeedSlider != null)
        {
            rotationSpeedSlider.minValue = 0f;
            rotationSpeedSlider.maxValue = 360f;
            rotationSpeedSlider.onValueChanged.RemoveAllListeners();
            rotationSpeedSlider.value = trigger.rotationSpeed;
            rotationSpeedSlider.onValueChanged.AddListener((val) =>
            {
                trigger.rotationSpeed = val;
            });
        }

        // Always force distance preservation for Single Motion traps configured in the editor
        trigger.preserveRelativeDistance = true;

        // 4. Setup the Horizontal/Vertical direction sliders using CameraBorrowerSlider
        var borrower = GetComponent<CameraBorrowerSlider>();
        if (targetObj != null)
        {
            if (borrower == null) borrower = gameObject.AddComponent<CameraBorrowerSlider>();
            CanvasGroup parentGroup = transform.parent != null ? transform.parent.GetComponentInParent<CanvasGroup>() : null;
            if (parentGroup == null && transform.parent != null)
            {
                Transform parent = transform.parent;
                while (parent != null)
                {
                    if (parent.name == "MechanicsPopupPanel")
                    {
                        parentGroup = parent.gameObject.GetComponent<CanvasGroup>() ?? parent.gameObject.AddComponent<CanvasGroup>();
                        break;
                    }
                    parent = parent.parent;
                }
            }

            borrower.Initialize(horizontalDirectionSlider, verticalDirectionSlider, trigger.objectsToTrigger, trigger.targetPosition, parentGroup);
            borrower.OnPositionSaved = (pos) =>
            {
                trigger.targetPosition = pos;
            };
        }
        else
        {
            if (borrower != null)
            {
                borrower.CleanUp();
                Destroy(borrower);
            }
        }
    }
}
